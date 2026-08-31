using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class ProductUnit : BaseEntity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public string UniqueNumber { get; set; } = string.Empty;
    public ProductUnitStatus Status { get; set; } = ProductUnitStatus.Available;
    public int? BillItemId { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? MRP { get; set; }

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public BillItem? BillItem { get; set; }
}
