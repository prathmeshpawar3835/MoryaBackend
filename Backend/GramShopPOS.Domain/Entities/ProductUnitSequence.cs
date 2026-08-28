namespace GramShopPOS.Domain.Entities;

public class ProductUnitSequence : BaseEntity
{
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; }
}
