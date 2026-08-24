namespace GramShopPOS.Domain.Entities;

public class StockTransferItem : BaseEntity
{
    public int StockTransferId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    public StockTransfer StockTransfer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
