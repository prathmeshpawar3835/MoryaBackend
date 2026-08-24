namespace GramShopPOS.Domain.Entities;

public class Inventory : BaseEntity
{
    public int StoreId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Store Store { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
