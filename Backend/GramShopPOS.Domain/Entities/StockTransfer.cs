using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class StockTransfer : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public int FromStoreId { get; set; }
    public int ToStoreId { get; set; }
    public DateTime TransferDate { get; set; }
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Completed;
    public string? Reason { get; set; }
    public int UserId { get; set; }

    public Store FromStore { get; set; } = null!;
    public Store ToStore { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}
