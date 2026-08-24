using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IExcelWorkbookService _excel;
    private readonly IPdfService _pdf;

    public ReportService(IAppDbContext db, ICurrentUser currentUser, IExcelWorkbookService excel, IPdfService pdf)
    {
        _db = db;
        _currentUser = currentUser;
        _excel = excel;
        _pdf = pdf;
    }

    public async Task<SalesReportDto> GetSalesAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var query = BillQuery(request);
        var totals = await query.GroupBy(_ => 1).Select(g => new
        {
            Sales = g.Sum(x => x.GrandTotal),
            Count = g.Count(),
            Tax = g.Sum(x => x.TaxAmount),
            Discount = g.Sum(x => x.ItemDiscountTotal + x.BillDiscount)
        }).FirstOrDefaultAsync(cancellationToken);

        var page = await query.OrderByDescending(b => b.BillDate).Select(b => new BillDto
        {
            Id = b.Id,
            StoreId = b.StoreId,
            StoreCode = b.Store.StoreCode,
            BillNumber = b.BillNumber,
            BillDate = b.BillDate,
            GrandTotal = b.GrandTotal,
            TaxAmount = b.TaxAmount,
            PaidAmount = b.PaidAmount,
            DueAmount = b.DueAmount,
            Status = b.Status,
            CustomerName = b.Customer != null ? b.Customer.Name : null
        }).ToPagedAsync(request, cancellationToken);

        var ids = page.Items.Select(x => x.Id).ToList();
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.BillId != null && ids.Contains(p.BillId.Value))
            .GroupBy(p => p.PaymentMode)
            .Select(g => new PaymentModeSummaryDto { PaymentMode = g.Key.ToString(), Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return new SalesReportDto
        {
            TotalSales = totals?.Sales ?? 0,
            BillCount = totals?.Count ?? 0,
            Tax = totals?.Tax ?? 0,
            Discounts = totals?.Discount ?? 0,
            NetSales = (totals?.Sales ?? 0) - (totals?.Discount ?? 0),
            PaymentBreakdown = payments,
            Bills = page
        };
    }

    public async Task<PagedResponse<ProductSalesRowDto>> GetProductSalesAsync(ReportRequest request, bool slowMoving, CancellationToken cancellationToken = default)
    {
        var (from, to) = Range(request);
        var query = _db.BillItems.AsNoTracking().Where(i => i.Bill.Status != BillStatus.Cancelled && i.Bill.BillDate >= from && i.Bill.BillDate < to);
        query = FilterStore(query, i => i.Bill.StoreId, request.StoreId);
        var grouped = query.GroupBy(i => new { i.ProductId, i.ProductCode, i.ProductName })
            .Select(g => new ProductSalesRowDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Total)
            });
        grouped = slowMoving ? grouped.OrderBy(x => x.QuantitySold) : grouped.OrderByDescending(x => x.QuantitySold);
        return await grouped.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<InventoryReportRowDto>> GetInventoryAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var query = _db.Inventories.AsNoTracking().Where(i => !i.IsDeleted);
        query = FilterStore(query, i => i.StoreId, request.StoreId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(i => i.Product.ProductCode.Contains(s) || i.Product.ProductName.Contains(s));
        }

        var projected = query.Select(i => new InventoryReportRowDto
        {
            StoreId = i.StoreId,
            StoreCode = i.Store.StoreCode,
            ProductId = i.ProductId,
            ProductCode = i.Product.ProductCode,
            ProductName = i.Product.ProductName,
            Quantity = i.Quantity,
            PurchaseValue = _currentUser.IsAdmin ? i.Quantity * i.Product.PurchasePrice : 0,
            SellingValue = i.Quantity * i.Product.SellingPrice,
            IsLowStock = i.Quantity <= i.Product.MinimumStockLevel
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<PurchaseDto>> GetPurchasesAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var (from, to) = Range(request);
        var query = _db.Purchases.AsNoTracking().Where(p => p.PurchaseDate >= from && p.PurchaseDate < to);
        query = FilterStore(query, p => p.StoreId, request.StoreId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p => p.InvoiceNumber.Contains(s) || p.SupplierName.Contains(s));
        }

        var projected = query.OrderByDescending(p => p.PurchaseDate).Select(p => new PurchaseDto
        {
            Id = p.Id,
            StoreId = p.StoreId,
            StoreCode = p.Store.StoreCode,
            SupplierName = p.SupplierName,
            InvoiceNumber = p.InvoiceNumber,
            PurchaseDate = p.PurchaseDate,
            Total = p.Total
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<ReturnDto>> GetReturnsAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var (from, to) = Range(request);
        var query = _db.Returns.AsNoTracking().Where(r => r.ReturnDate >= from && r.ReturnDate < to);
        query = FilterStore(query, r => r.StoreId, request.StoreId);
        var projected = query.OrderByDescending(r => r.ReturnDate).Select(r => new ReturnDto
        {
            Id = r.Id,
            StoreId = r.StoreId,
            OriginalBillId = r.OriginalBillId,
            OriginalBillNumber = r.OriginalBillNumber,
            ReturnNumber = r.ReturnNumber,
            ReturnDate = r.ReturnDate,
            ReturnAmount = r.ReturnAmount,
            ReturnKind = r.ReturnKind,
            ExchangeBillId = r.ExchangeBillId
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<CustomerDueRowDto>> GetCustomerDuesAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted && c.OutstandingBalance > 0);
        query = FilterStore(query, c => c.StoreId, request.StoreId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(c => c.Name.Contains(s) || c.MobileNumber.Contains(s));
        }

        var projected = query.Select(c => new CustomerDueRowDto
        {
            CustomerId = c.Id,
            Name = c.Name,
            Mobile = c.MobileNumber,
            StoreId = c.StoreId,
            OutstandingAmount = c.OutstandingBalance,
            TotalPurchases = c.Bills.Where(b => b.Status != BillStatus.Cancelled).Sum(b => b.GrandTotal),
            AgingDays = 0
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<ReferralReportRowDto>> GetReferralsAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var (from, to) = Range(request);
        var query = _db.Referrals.AsNoTracking().Where(r => r.ReferralDate >= from && r.ReferralDate < to);
        query = FilterStore(query, r => r.StoreId, request.StoreId);
        var grouped = query.GroupBy(r => new { r.ReferrerCustomerId, r.ReferrerCustomer.Name })
            .Select(g => new ReferralReportRowDto
            {
                ReferrerCustomerId = g.Key.ReferrerCustomerId,
                ReferrerName = g.Key.Name,
                ReferralCount = g.Count(),
                PendingRewards = g.Where(x => x.Status == ReferralRewardStatus.Pending).Sum(x => x.RewardAmount),
                CreditedRewards = g.Where(x => x.Status == ReferralRewardStatus.Credited).Sum(x => x.RewardAmount),
                RedeemedRewards = g.SelectMany(x => x.Rewards).Where(x => x.Status == ReferralRewardStatus.Redeemed).Sum(x => x.Amount)
            });
        return await grouped.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PagedResponse<ProfitReportRowDto>> GetProfitAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var (from, to) = Range(request);
        var query = _db.BillItems.AsNoTracking().Where(i => i.Bill.Status != BillStatus.Cancelled && i.Bill.BillDate >= from && i.Bill.BillDate < to);
        if (request.StoreId.HasValue)
        {
            query = query.Where(i => i.Bill.StoreId == request.StoreId.Value);
        }

        var projected = query.Select(i => new ProfitReportRowDto
        {
            BillNumber = i.Bill.BillNumber,
            BillDate = i.Bill.BillDate,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            SellingAmount = i.Total,
            HistoricalPurchaseAmount = i.PurchasePrice * i.Quantity,
            Discount = i.DiscountAmount,
            Profit = i.Total - (i.PurchasePrice * i.Quantity) - i.DiscountAmount
        }).OrderByDescending(x => x.BillDate);
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<FileDownload> ExportSalesExcelAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        request.PageSize = 200;
        var report = await GetSalesAsync(request, cancellationToken);
        return _excel.CreateWorkbook("Sales", "sales-report.xlsx",
            ["Bill Number", "Date", "Store", "Customer", "Total", "Tax", "Paid", "Due", "Status"],
            report.Bills.Items.Select(b => (IReadOnlyList<object?>)[b.BillNumber, b.BillDate, b.StoreCode, b.CustomerName, b.GrandTotal, b.TaxAmount, b.PaidAmount, b.DueAmount, b.Status.ToString()]));
    }

    public async Task<FileDownload> ExportInventoryExcelAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        request.PageSize = 200;
        var rows = await GetInventoryAsync(request, cancellationToken);
        return _excel.CreateWorkbook("Inventory", "inventory-report.xlsx",
            ["Store", "Code", "Product", "Qty", "Purchase Value", "Selling Value", "Low Stock"],
            rows.Items.Select(r => (IReadOnlyList<object?>)[r.StoreCode, r.ProductCode, r.ProductName, r.Quantity, r.PurchaseValue, r.SellingValue, r.IsLowStock]));
    }

    public async Task<FileDownload> ExportCustomersExcelAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        request.PageSize = 200;
        var rows = await GetCustomerDuesAsync(request, cancellationToken);
        return _excel.CreateWorkbook("Customers", "customer-dues.xlsx",
            ["Name", "Mobile", "Store", "Outstanding", "Purchases", "Aging Days"],
            rows.Items.Select(r => (IReadOnlyList<object?>)[r.Name, r.Mobile, r.StoreId, r.OutstandingAmount, r.TotalPurchases, r.AgingDays]));
    }

    public async Task<FileDownload> ExportProductSalesExcelAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        request.PageSize = 200;
        var rows = await GetProductSalesAsync(request, false, cancellationToken);
        return _excel.CreateWorkbook("ProductSales", "product-sales.xlsx",
            ["Code", "Product", "Qty", "Revenue"],
            rows.Items.Select(r => (IReadOnlyList<object?>)[r.ProductCode, r.ProductName, r.QuantitySold, r.Revenue]));
    }

    public async Task<FileDownload> ExportSalesPdfAsync(ReportRequest request, CancellationToken cancellationToken = default) =>
        _pdf.SalesReportPdf(await GetSalesAsync(request, cancellationToken));

    public async Task<FileDownload> ExportInventoryPdfAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        request.PageSize = 200;
        var rows = await GetInventoryAsync(request, cancellationToken);
        return _pdf.InventoryReportPdf(rows.Items);
    }

    private IQueryable<Domain.Entities.Bill> BillQuery(ReportRequest request)
    {
        _currentUser.EnsureAuthenticated();
        ApplyPeriod(request);
        var (from, to) = Range(request);
        var query = _db.Bills.AsNoTracking().Where(b => b.Status != BillStatus.Cancelled && b.BillDate >= from && b.BillDate < to);
        return FilterStore(query, b => b.StoreId, request.StoreId);
    }

    private IQueryable<T> FilterStore<T>(IQueryable<T> query, System.Linq.Expressions.Expression<Func<T, int>> storeSelector, int? storeId)
    {
        if (storeId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(storeId.Value);
            var p = storeSelector.Parameters[0];
            var body = System.Linq.Expressions.Expression.Equal(storeSelector.Body, System.Linq.Expressions.Expression.Constant(storeId.Value));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, p);
            query = query.Where(lambda);
        }
        else if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            var p = storeSelector.Parameters[0];
            var contains = typeof(Enumerable).GetMethods().First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(int));
            var body = System.Linq.Expressions.Expression.Call(contains, System.Linq.Expressions.Expression.Constant(ids), storeSelector.Body);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, p);
            query = query.Where(lambda);
        }

        return query;
    }

    private static void ApplyPeriod(ReportRequest request)
    {
        var today = DateTime.UtcNow.Date;
        switch (request.Period.ToLowerInvariant())
        {
            case "daily":
                request.FromDate = today;
                request.ToDate = today;
                break;
            case "weekly":
                request.FromDate = today.AddDays(-6);
                request.ToDate = today;
                break;
            case "monthly":
                request.FromDate = new DateTime(today.Year, today.Month, 1);
                request.ToDate = today;
                break;
        }
    }

    private static (DateTime From, DateTime To) Range(PagedRequest request)
    {
        var from = request.FromDate?.Date ?? DateTime.UtcNow.Date.AddMonths(-1);
        var to = (request.ToDate?.Date ?? DateTime.UtcNow.Date).AddDays(1);
        return (from, to);
    }
}
