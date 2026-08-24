namespace GramShopPOS.Domain.Entities;

public class ReturnItem : BaseEntity
{
    public int ProductReturnId { get; set; }
    public int OriginalBillItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public ProductReturn ProductReturn { get; set; } = null!;
    public BillItem OriginalBillItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
