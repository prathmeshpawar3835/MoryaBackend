namespace GramShopPOS.Domain.Entities;

public class Supplier : BaseEntity
{
    public int? StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTNumber { get; set; }
    public string? Notes { get; set; }

    public Store? Store { get; set; }
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
