namespace GramShopPOS.Domain.Entities;

public class CustomerPayment : BaseEntity
{
    public int CustomerId { get; set; }
    public int StoreId { get; set; }
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}
