namespace GramShopPOS.Domain.Entities;

public class BillItem : BaseEntity
{
    public int BillId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public Bill Bill { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
