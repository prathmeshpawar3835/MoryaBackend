using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class StoreDiscount : BaseEntity
{
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DiscountKind DiscountKind { get; set; } = DiscountKind.Percentage;
    public decimal Value { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public Store Store { get; set; } = null!;
}
