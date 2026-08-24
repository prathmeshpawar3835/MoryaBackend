namespace GramShopPOS.Domain.Entities;

public class PurchaseItem : BaseEntity
{
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Total { get; set; }

    public Purchase Purchase { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
