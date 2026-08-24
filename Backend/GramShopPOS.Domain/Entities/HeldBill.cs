namespace GramShopPOS.Domain.Entities;

public class HeldBill : BaseEntity
{
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public int SalesPersonId { get; set; }
    public string HoldReference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal BillDiscount { get; set; }
    public string ItemsJson { get; set; } = "[]";

    public Store Store { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ApplicationUser SalesPerson { get; set; } = null!;
}
