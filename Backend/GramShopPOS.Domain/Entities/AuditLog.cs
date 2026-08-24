namespace GramShopPOS.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public int? StoreId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedDate { get; set; }

    public ApplicationUser? User { get; set; }
    public Store? Store { get; set; }
}
