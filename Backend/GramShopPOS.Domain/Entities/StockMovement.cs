using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class StockMovement : BaseEntity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public int? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
    public int UserId { get; set; }

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
