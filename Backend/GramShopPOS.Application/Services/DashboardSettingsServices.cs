using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.DTOs.Settings;
using GramShopPOS.Application.Exceptions;
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
        var monthStart = new DateTime(start.Year, start.Month, 1);
        var todayBills = bills.Where(b => b.BillDate >= start && b.BillType == BillType.Sale);
        var monthBills = bills.Where(b => b.BillDate >= monthStart && b.BillType == BillType.Sale);
        var sales = await todayBills.SumAsync(b => (decimal?)b.GrandTotal, cancellationToken) ?? 0;
        var count = await todayBills.CountAsync(cancellationToken);
        var customers = await todayBills.Select(b => b.CustomerId).Distinct().CountAsync(cancellationToken);
        var dues = await bills.SumAsync(b => (decimal?)b.DueAmount, cancellationToken) ?? 0;
        var monthlySales = await monthBills.SumAsync(b => (decimal?)b.GrandTotal, cancellationToken) ?? 0;
        var monthlyBills = await monthBills.CountAsync(cancellationToken);
        var avgBill = monthlyBills == 0 ? 0 : Money.Round(monthlySales / monthlyBills);

        var returnsQuery = _db.Returns.AsNoTracking().AsQueryable();
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            returnsQuery = returnsQuery.Where(r => ids.Contains(r.StoreId));
        }

        if (storeId.HasValue)
        {
            returnsQuery = returnsQuery.Where(r => r.StoreId == storeId.Value);
        }

        var todayReturns = returnsQuery.Where(r => r.ReturnDate >= start && r.ReturnKind == ReturnKind.Return);
        var monthReturns = returnsQuery.Where(r => r.ReturnDate >= monthStart && r.ReturnKind == ReturnKind.Return);
        var todayExchanges = returnsQuery.Where(r => r.ReturnDate >= start && r.ReturnKind == ReturnKind.Exchange);
        var monthExchanges = returnsQuery.Where(r => r.ReturnDate >= monthStart && r.ReturnKind == ReturnKind.Exchange);
        var todayBuybacks = returnsQuery.Where(r => r.ReturnDate >= start && r.ReturnKind == ReturnKind.Buyback);
        var monthBuybacks = returnsQuery.Where(r => r.ReturnDate >= monthStart && r.ReturnKind == ReturnKind.Buyback);

        var inventoryBase = _db.Inventories.AsNoTracking().Where(i => !i.IsDeleted);
        if (storeId.HasValue)
        {
            inventoryBase = inventoryBase.Where(i => i.StoreId == storeId.Value);
        }
        else if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            inventoryBase = inventoryBase.Where(i => ids.Contains(i.StoreId));
        }

        var inventory = inventoryBase.Where(i => i.Quantity <= i.Product.MinimumStockLevel);

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
            .Where(i => i.Bill.Status != BillStatus.Cancelled && i.Bill.BillType == BillType.Sale && i.Bill.BillDate >= from && (storeId == null || i.Bill.StoreId == storeId))
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

        var custQuery = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);
        if (storeId.HasValue)
        {
            custQuery = custQuery.Where(c => c.StoreId == storeId.Value);
        }
        else if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            custQuery = custQuery.Where(c => ids.Contains(c.StoreId));
        }

        var totalCustomers = await custQuery.CountAsync(cancellationToken);
        var purchasingCustomers = await bills.Where(b => b.CustomerId != null).Select(b => b.CustomerId).Distinct().CountAsync(cancellationToken);
        var purchaseRatio = totalCustomers == 0 ? 0 : Money.Round((decimal)purchasingCustomers / totalCustomers);

        var refQuery = _db.Referrals.AsNoTracking().AsQueryable();
        if (storeId.HasValue)
        {
            refQuery = refQuery.Where(r => r.StoreId == storeId.Value);
        }
        else if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            refQuery = refQuery.Where(r => ids.Contains(r.StoreId));
        }

        var todayRefs = refQuery.Where(r => r.ReferralDate >= start);
        var monthRefs = refQuery.Where(r => r.ReferralDate >= monthStart);
        var rewardQuery = _db.ReferralRewards.AsNoTracking().Where(r => r.IsReferrerReward);
        if (storeId.HasValue)
        {
            rewardQuery = rewardQuery.Where(r => r.Referral.StoreId == storeId.Value);
        }

        var slow = await _db.BillItems.AsNoTracking()
            .Where(i => i.Bill.Status != BillStatus.Cancelled && i.Bill.BillType == BillType.Sale && i.Bill.BillDate >= from && (storeId == null || i.Bill.StoreId == storeId))
            .GroupBy(i => new { i.ProductId, i.ProductCode, i.ProductName })
            .Select(g => new ProductSalesRowDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Total)
            })
            .OrderBy(x => x.QuantitySold)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topReferrers = await monthRefs
            .GroupBy(r => new { r.ReferrerCustomerId, r.ReferrerCustomer.Name, r.ReferrerCustomer.CustomerCode })
            .Select(g => new TopReferrerDto
            {
                CustomerId = g.Key.ReferrerCustomerId,
                CustomerName = g.Key.Name,
                CustomerCode = g.Key.CustomerCode,
                ReferralCount = g.Count(),
                ReferralSales = g.Sum(x => x.SaleAmount),
                BenefitEarned = g.Sum(x => x.RewardAmount)
            })
            .OrderByDescending(x => x.BenefitEarned)
            .Take(5)
            .ToListAsync(cancellationToken);

        var referralChart = await monthRefs
            .GroupBy(r => r.ReferralDate.Date)
            .Select(g => new SalesChartPointDto { Date = g.Key, Sales = g.Sum(x => x.SaleAmount), BillCount = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var erChart = await returnsQuery.Where(r => r.ReturnDate >= from)
            .GroupBy(r => r.ReturnDate.Date)
            .Select(g => new ExchangeReturnChartPointDto
            {
                Date = g.Key,
                ReturnAmount = g.Where(x => x.ReturnKind == ReturnKind.Return).Sum(x => x.ReturnAmount),
                ExchangeAmount = g.Where(x => x.ReturnKind == ReturnKind.Exchange).Sum(x => x.ReturnAmount),
                BuybackAmount = g.Where(x => x.ReturnKind == ReturnKind.Buyback).Sum(x => x.ReturnAmount),
                ReturnCount = g.Count(x => x.ReturnKind == ReturnKind.Return),
                ExchangeCount = g.Count(x => x.ReturnKind == ReturnKind.Exchange),
                BuybackCount = g.Count(x => x.ReturnKind == ReturnKind.Buyback)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var todayDate = BusinessCalendar.Today();
        var birthdayMonth = todayDate.Month;
        var birthdayDay = todayDate.Day;
        var customerQuery = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);
        var messageQuery = _db.BirthdayMessageLogs.AsNoTracking().AsQueryable();
        var redemptionQuery = _db.BirthdayOfferRedemptions.AsNoTracking().Where(r => r.Status == BirthdayRedemptionStatus.Redeemed);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            customerQuery = customerQuery.Where(c => ids.Contains(c.StoreId));
            messageQuery = messageQuery.Where(l => ids.Contains(l.StoreId));
            redemptionQuery = redemptionQuery.Where(r => ids.Contains(r.StoreId));
        }

        if (storeId.HasValue)
        {
            customerQuery = customerQuery.Where(c => c.StoreId == storeId.Value);
            messageQuery = messageQuery.Where(l => l.StoreId == storeId.Value);
            redemptionQuery = redemptionQuery.Where(r => r.StoreId == storeId.Value);
        }

        var todayBirthdayCustomers = await customerQuery.CountAsync(
            c => c.DateOfBirth != null && c.DateOfBirth.Value.Month == birthdayMonth && c.DateOfBirth.Value.Day == birthdayDay,
            cancellationToken);
        var todayMsgs = messageQuery.Where(l => l.BirthdayDate == todayDate);
        var todayBirthdaySent = await todayMsgs.CountAsync(l => l.Status == WhatsAppMessageStatus.Sent, cancellationToken);
        var todayBirthdayFailed = await todayMsgs.CountAsync(l => l.Status == WhatsAppMessageStatus.Failed, cancellationToken);
        var todayReds = redemptionQuery.Where(r => r.BirthdayDate == todayDate);
        var todayBirthdayRedeemed = await todayReds.CountAsync(cancellationToken);
        var todayBirthdayDiscount = await todayReds.SumAsync(r => (decimal?)r.DiscountAmount, cancellationToken) ?? 0;
        var monthStartDate = new DateOnly(todayDate.Year, todayDate.Month, 1);
        var monthReds = redemptionQuery.Where(r => r.BirthdayDate >= monthStartDate);
        var monthlyBirthdayRedeemed = await monthReds.CountAsync(cancellationToken);
        var monthlyBirthdayDiscount = await monthReds.SumAsync(r => (decimal?)r.DiscountAmount, cancellationToken) ?? 0;

        return new DashboardDto
        {
            TodaySales = sales,
            TodayBills = count,
            TodayCustomers = customers,
            PendingDues = dues,
            MonthlySales = monthlySales,
            MonthlyBills = monthlyBills,
            TodayReturns = await todayReturns.SumAsync(r => (decimal?)r.ReturnAmount, cancellationToken) ?? 0,
            TodayReturnCount = await todayReturns.CountAsync(cancellationToken),
            MonthlyReturns = await monthReturns.SumAsync(r => (decimal?)r.ReturnAmount, cancellationToken) ?? 0,
            MonthlyReturnCount = await monthReturns.CountAsync(cancellationToken),
            TodayExchanges = await todayExchanges.SumAsync(r => (decimal?)r.ReturnAmount, cancellationToken) ?? 0,
            TodayExchangeCount = await todayExchanges.CountAsync(cancellationToken),
            MonthlyExchanges = await monthExchanges.SumAsync(r => (decimal?)r.ReturnAmount, cancellationToken) ?? 0,
            MonthlyExchangeCount = await monthExchanges.CountAsync(cancellationToken),
            TodayBuybacks = await todayBuybacks.SumAsync(r => (decimal?)r.ReturnAmount, cancellationToken) ?? 0,
            TodayBuybackCount = await todayBuybacks.CountAsync(cancellationToken),
            MonthlyBuybacks = await monthBuybacks.SumAsync(r => (decimal?)r.ReturnAmount, cancellationToken) ?? 0,
            MonthlyBuybackCount = await monthBuybacks.CountAsync(cancellationToken),
            TodayCreditUsed = await todayBills.SumAsync(b => (decimal?)b.WalletRedeemed, cancellationToken) ?? 0,
            TodayCreditGenerated = await todayBills.SumAsync(b => (decimal?)b.CreditGenerated, cancellationToken) ?? 0,
            TotalCustomers = totalCustomers,
            PurchasingCustomers = purchasingCustomers,
            CustomerPurchaseRatio = purchaseRatio,
            AverageBillValue = avgBill,
            TodayReferralCount = await todayRefs.CountAsync(cancellationToken),
            TodayReferralSales = await todayRefs.SumAsync(r => (decimal?)r.SaleAmount, cancellationToken) ?? 0,
            TodayReferralDiscount = await todayRefs.SumAsync(r => (decimal?)r.DiscountGiven, cancellationToken) ?? 0,
            TodayReferralCost = await rewardQuery.Where(r => r.CreatedDate >= start && r.IsReferrerReward && !r.IsReversal).SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0,
            MonthlyReferralCount = await monthRefs.CountAsync(cancellationToken),
            MonthlyReferralSales = await monthRefs.SumAsync(r => (decimal?)r.SaleAmount, cancellationToken) ?? 0,
            MonthlyReferralDiscount = await monthRefs.SumAsync(r => (decimal?)r.DiscountGiven, cancellationToken) ?? 0,
            MonthlyReferralCost = await rewardQuery.Where(r => r.CreatedDate >= monthStart && r.IsReferrerReward && !r.IsReversal).SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0,
            TotalReferralCost = await rewardQuery.Where(r => r.IsReferrerReward && !r.IsReversal).SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0,
            TotalInventoryProducts = await inventoryBase.Select(i => i.ProductId).Distinct().CountAsync(cancellationToken),
            TotalInventoryQuantity = await inventoryBase.SumAsync(i => (decimal?)i.Quantity, cancellationToken) ?? 0,
            LowStockCount = await inventory.CountAsync(cancellationToken),
            OutOfStockCount = await inventoryBase.CountAsync(i => i.Quantity <= 0, cancellationToken),
            TopReferrers = topReferrers,
            LowStockProducts = lowStock,
            TopSellingProducts = top,
            SlowMovingProducts = slow,
            RecentBills = recent,
            PaymentModeSummary = payments,
            SalesChartData = chart,
            ReferralChartData = referralChart,
            ExchangeReturnChart = erChart,
            TodayBirthdayCustomers = todayBirthdayCustomers,
            TodayBirthdayMessagesSent = todayBirthdaySent,
            TodayBirthdayMessagesFailed = todayBirthdayFailed,
            TodayBirthdayOffersRedeemed = todayBirthdayRedeemed,
            TodayBirthdayDiscount = todayBirthdayDiscount,
            MonthlyBirthdayOffersRedeemed = monthlyBirthdayRedeemed,
            MonthlyBirthdayDiscount = monthlyBirthdayDiscount
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

    public async Task<PosBillingRulesDto> GetPosRulesAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var s = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        return new PosBillingRulesDto
        {
            ReturnDeductionPercent = s.ReturnDeductionPercent,
            ExchangeDeductionPercent = s.ExchangeDeductionPercent,
            BuybackDeductionPercent = s.BuybackDeductionPercent
        };
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
        s.BirthdayDiscountPercent = request.BirthdayDiscountPercent < 0 ? 0 : request.BirthdayDiscountPercent;
        s.ReturnDeductionPercent = AdjustmentDeduction.ClampPercent(request.ReturnDeductionPercent);
        s.ExchangeDeductionPercent = AdjustmentDeduction.ClampPercent(request.ExchangeDeductionPercent);
        s.BuybackDeductionPercent = AdjustmentDeduction.ClampPercent(request.BuybackDeductionPercent);
        if (request.ReturnDeductionPercent is < 0 or > 100
            || request.ExchangeDeductionPercent is < 0 or > 100
            || request.BuybackDeductionPercent is < 0 or > 100)
        {
            throw new ValidationAppException("Return, exchange, and buyback deductions must be between 0 and 100 percent.");
        }

        s.WhatsAppEnabled = request.WhatsAppEnabled;
        s.WhatsAppPhoneNumberId = request.WhatsAppPhoneNumberId;
        if (!string.IsNullOrWhiteSpace(request.WhatsAppAccessToken) && request.WhatsAppAccessToken != "********")
        {
            s.WhatsAppAccessToken = request.WhatsAppAccessToken;
        }
        s.WhatsAppApiBaseUrl = request.WhatsAppApiBaseUrl;
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
        BirthdayDiscountPercent = s.BirthdayDiscountPercent,
        ReturnDeductionPercent = s.ReturnDeductionPercent,
        ExchangeDeductionPercent = s.ExchangeDeductionPercent,
        BuybackDeductionPercent = s.BuybackDeductionPercent,
        WhatsAppEnabled = s.WhatsAppEnabled,
        WhatsAppPhoneNumberId = s.WhatsAppPhoneNumberId,
        WhatsAppAccessToken = string.IsNullOrWhiteSpace(s.WhatsAppAccessToken) ? null : "********",
        WhatsAppApiBaseUrl = s.WhatsAppApiBaseUrl,
        TaxSettings = taxes
    };
}
