using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DashboardService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> GetAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var bills = _db.Bills.AsNoTracking().Where(b => b.Status != BillStatus.Cancelled);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            bills = bills.Where(b => ids.Contains(b.StoreId));
        }

        if (storeId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(storeId.Value);
            bills = bills.Where(b => b.StoreId == storeId.Value);
        }

        var start = DateTime.UtcNow.Date;
        var todayBills = bills.Where(b => b.BillDate >= start);
        var sales = await todayBills.SumAsync(b => (decimal?)b.GrandTotal, cancellationToken) ?? 0;
        var count = await todayBills.CountAsync(cancellationToken);
        var customers = await todayBills.Select(b => b.CustomerId).Distinct().CountAsync(cancellationToken);
        var dues = await bills.SumAsync(b => (decimal?)b.DueAmount, cancellationToken) ?? 0;

        var inventory = _db.Inventories.AsNoTracking().Where(i => !i.IsDeleted && i.Quantity <= i.Product.MinimumStockLevel);
        if (storeId.HasValue)
        {
            inventory = inventory.Where(i => i.StoreId == storeId.Value);
        }
        else if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            inventory = inventory.Where(i => ids.Contains(i.StoreId));
        }

        var lowStock = await inventory.Take(10).Select(i => new InventoryDto
        {
            Id = i.Id,
            StoreId = i.StoreId,
            StoreCode = i.Store.StoreCode,
            ProductId = i.ProductId,
            ProductCode = i.Product.ProductCode,
            ProductName = i.Product.ProductName,
            Quantity = i.Quantity,
            MinimumStockLevel = i.Product.MinimumStockLevel,
            IsLowStock = true,
            SellingPrice = i.Product.SellingPrice
        }).ToListAsync(cancellationToken);

        var from = start.AddDays(-6);
        var top = await _db.BillItems.AsNoTracking()
            .Where(i => i.Bill.Status != BillStatus.Cancelled && i.Bill.BillDate >= from && (storeId == null || i.Bill.StoreId == storeId))
            .GroupBy(i => new { i.ProductId, i.ProductCode, i.ProductName })
            .Select(g => new ProductSalesRowDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recent = await bills.OrderByDescending(b => b.BillDate).Take(8).Select(b => new BillDto
        {
            Id = b.Id,
            BillNumber = b.BillNumber,
            BillDate = b.BillDate,
            GrandTotal = b.GrandTotal,
            Status = b.Status,
            CustomerName = b.Customer != null ? b.Customer.Name : null,
            StoreId = b.StoreId
        }).ToListAsync(cancellationToken);

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.PaymentDate >= start && p.BillId != null && (storeId == null || p.StoreId == storeId))
            .GroupBy(p => p.PaymentMode)
            .Select(g => new PaymentModeSummaryDto { PaymentMode = g.Key.ToString(), Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var chart = await bills.Where(b => b.BillDate >= from)
            .GroupBy(b => b.BillDate.Date)
            .Select(g => new SalesChartPointDto { Date = g.Key, Sales = g.Sum(x => x.GrandTotal), BillCount = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return new DashboardDto
        {
            TodaySales = sales,
            TodayBills = count,
            TodayCustomers = customers,
            PendingDues = dues,
            LowStockProducts = lowStock,
            TopSellingProducts = top,
            RecentBills = recent,
            PaymentModeSummary = payments,
            SalesChartData = chart
        };
    }
}

public sealed class SettingsService : ISettingsService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public SettingsService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<DTOs.Settings.SettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var s = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var taxes = await _db.TaxSettings.AsNoTracking().Where(t => !t.IsDeleted).Select(t => new DTOs.Settings.TaxSettingDto
        {
            Id = t.Id,
            Name = t.Name,
            Percent = t.Percent,
            IsDefault = t.IsDefault
        }).ToListAsync(cancellationToken);
        return Map(s, taxes);
    }

    public async Task<DTOs.Settings.SettingsDto> UpdateAsync(DTOs.Settings.UpdateSettingsRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var s = await _db.BusinessSettings.FirstAsync(cancellationToken);
        s.ShopName = request.ShopName;
        s.LogoPath = request.LogoPath;
        s.Address = request.Address;
        s.Mobile = request.Mobile;
        s.Email = request.Email;
        s.GSTNumber = request.GSTNumber;
        s.InvoiceFooter = request.InvoiceFooter;
        s.ReturnPolicy = request.ReturnPolicy;
        s.InvoicePrefix = request.InvoicePrefix;
        s.InvoiceNumberFormat = request.InvoiceNumberFormat;
        s.FinancialYearStartMonth = request.FinancialYearStartMonth is >= 1 and <= 12 ? request.FinancialYearStartMonth : 4;
        s.AllowNegativeStock = request.AllowNegativeStock;
        s.DefaultTaxPercent = request.DefaultTaxPercent;
        s.LowStockDefaultLevel = request.LowStockDefaultLevel;
        s.ReferralEnabled = request.ReferralEnabled;
        s.NewCustomerReward = request.NewCustomerReward;
        s.ReferrerReward = request.ReferrerReward;
        s.RewardType = request.RewardType;
        s.RewardTrigger = request.RewardTrigger;
        s.ReferralStoreWise = request.ReferralStoreWise;
        s.UpdatedDate = DateTime.UtcNow;
        s.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(Domain.Constants.AuditActions.SettingsChanged, nameof(Domain.Entities.BusinessSetting), s.Id.ToString(), null, request, null, cancellationToken);
        return await GetAsync(cancellationToken);
    }

    private static DTOs.Settings.SettingsDto Map(Domain.Entities.BusinessSetting s, IReadOnlyList<DTOs.Settings.TaxSettingDto> taxes) => new()
    {
        ShopName = s.ShopName,
        LogoPath = s.LogoPath,
        Address = s.Address,
        Mobile = s.Mobile,
        Email = s.Email,
        GSTNumber = s.GSTNumber,
        InvoiceFooter = s.InvoiceFooter,
        ReturnPolicy = s.ReturnPolicy,
        InvoicePrefix = s.InvoicePrefix,
        InvoiceNumberFormat = s.InvoiceNumberFormat,
        FinancialYearStartMonth = s.FinancialYearStartMonth,
        AllowNegativeStock = s.AllowNegativeStock,
        DefaultTaxPercent = s.DefaultTaxPercent,
        LowStockDefaultLevel = s.LowStockDefaultLevel,
        ReferralEnabled = s.ReferralEnabled,
        NewCustomerReward = s.NewCustomerReward,
        ReferrerReward = s.ReferrerReward,
        RewardType = s.RewardType,
        RewardTrigger = s.RewardTrigger,
        ReferralStoreWise = s.ReferralStoreWise,
        TaxSettings = taxes
    };
}
