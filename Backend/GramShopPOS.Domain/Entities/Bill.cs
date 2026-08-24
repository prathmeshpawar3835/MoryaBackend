using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class Bill : BaseEntity
{
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public int SalesPersonId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public BillType BillType { get; set; } = BillType.Sale;
    public BillStatus Status { get; set; } = BillStatus.Completed;
    public decimal Subtotal { get; set; }
    public decimal ItemDiscountTotal { get; set; }
    public decimal BillDiscount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public decimal WalletRedeemed { get; set; }
    public string? Notes { get; set; }
    public int? ExchangeOfBillId { get; set; }

    public Store Store { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ApplicationUser SalesPerson { get; set; } = null!;
    public Bill? ExchangeOfBill { get; set; }
    public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
