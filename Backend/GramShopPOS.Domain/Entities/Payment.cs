using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class Payment : BaseEntity
{
    public int StoreId { get; set; }
    public int? BillId { get; set; }
    public int? CustomerId { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }

    public Store Store { get; set; } = null!;
    public Bill? Bill { get; set; }
    public Customer? Customer { get; set; }
    public ApplicationUser User { get; set; } = null!;
}
