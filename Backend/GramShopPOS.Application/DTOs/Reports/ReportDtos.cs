namespace GramShopPOS.Application.DTOs.Reports;

public class DashboardDto
{
    public decimal TodaySales { get; set; }
    public int TodayBills { get; set; }
    public int TodayCustomers { get; set; }
    public decimal PendingDues { get; set; }
    public IReadOnlyList<Inventory.InventoryDto> LowStockProducts { get; set; } = [];
    public IReadOnlyList<ProductSalesRowDto> TopSellingProducts { get; set; } = [];
    public IReadOnlyList<Billing.BillDto> RecentBills { get; set; } = [];
    public IReadOnlyList<PaymentModeSummaryDto> PaymentModeSummary { get; set; } = [];
    public IReadOnlyList<SalesChartPointDto> SalesChartData { get; set; } = [];
}

public class PaymentModeSummaryDto
{
    public string PaymentMode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class SalesChartPointDto
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public int BillCount { get; set; }
}

public class SalesReportDto
{
    public decimal TotalSales { get; set; }
    public int BillCount { get; set; }
    public decimal Tax { get; set; }
    public decimal Discounts { get; set; }
    public decimal NetSales { get; set; }
    public IReadOnlyList<PaymentModeSummaryDto> PaymentBreakdown { get; set; } = [];
    public Common.PagedResponse<Billing.BillDto> Bills { get; set; } = Common.PagedResponse<Billing.BillDto>.Create([], 1, 20, 0);
}

public class ProductSalesRowDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class InventoryReportRowDto
{
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PurchaseValue { get; set; }
    public decimal SellingValue { get; set; }
    public bool IsLowStock { get; set; }
}

public class CustomerDueRowDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal TotalPurchases { get; set; }
    public int AgingDays { get; set; }
}

public class ReferralReportRowDto
{
    public int ReferrerCustomerId { get; set; }
    public string ReferrerName { get; set; } = string.Empty;
    public int ReferralCount { get; set; }
    public decimal PendingRewards { get; set; }
    public decimal CreditedRewards { get; set; }
    public decimal RedeemedRewards { get; set; }
}

public class ProfitReportRowDto
{
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal SellingAmount { get; set; }
    public decimal HistoricalPurchaseAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal Profit { get; set; }
}

public class ReportRequest : Common.PagedRequest
{
    public string Period { get; set; } = "custom";
}

public class FileDownload
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "download.bin";
}
