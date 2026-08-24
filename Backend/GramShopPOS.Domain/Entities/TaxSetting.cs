namespace GramShopPOS.Domain.Entities;

public class TaxSetting : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public bool IsDefault { get; set; }
}
