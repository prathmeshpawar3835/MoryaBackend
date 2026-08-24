namespace GramShopPOS.Domain.Entities;

public class ProductImportBatch : BaseEntity
{
    public Guid BatchId { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public string PayloadJson { get; set; } = "[]";
    public int ValidRowCount { get; set; }
    public int ErrorRowCount { get; set; }
}
