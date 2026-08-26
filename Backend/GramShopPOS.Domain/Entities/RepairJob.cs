using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class RepairJob : BaseEntity
{
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public int? BillId { get; set; }
    public int? BillItemId { get; set; }
    public int? ProductId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDetails { get; set; }
    public RepairJobType JobType { get; set; } = RepairJobType.Repair;
    public RepairJobStatus Status { get; set; } = RepairJobStatus.Received;
    public DateTime ReceivedDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? Notes { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PaymentMode? PaymentMode { get; set; }
    public string? PaymentReference { get; set; }
    public int UserId { get; set; }

    public Store Store { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Bill? Bill { get; set; }
    public BillItem? BillItem { get; set; }
    public Product? Product { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ICollection<RepairJobHistory> History { get; set; } = new List<RepairJobHistory>();
}
