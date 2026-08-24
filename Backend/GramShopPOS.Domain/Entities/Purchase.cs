namespace GramShopPOS.Domain.Entities;

public class Purchase : BaseEntity
{
    public int StoreId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }

    public Store Store { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}
