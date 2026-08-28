namespace GramShopPOS.Domain.Entities;

public class Product : BaseEntity
{
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Unit { get; set; } = "PCS";
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public string? ImagePath { get; set; }
    public decimal? WeightGrams { get; set; }
    public string? Metal { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<ProductUnit> Units { get; set; } = new List<ProductUnit>();
}
