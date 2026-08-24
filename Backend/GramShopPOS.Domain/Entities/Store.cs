namespace GramShopPOS.Domain.Entities;

public class Store : BaseEntity
{
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? GSTNumber { get; set; }
    public string? InvoicePrefix { get; set; }

    public ICollection<StoreUser> StoreUsers { get; set; } = new List<StoreUser>();
}
