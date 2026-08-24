namespace GramShopPOS.Domain.Entities;

public class BillSequence : BaseEntity
{
    public int StoreId { get; set; }
    public string FinancialYearCode { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Store Store { get; set; } = null!;
}
