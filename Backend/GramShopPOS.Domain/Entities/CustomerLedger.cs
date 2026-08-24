using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class CustomerLedger : BaseEntity
{
    public int CustomerId { get; set; }
    public int StoreId { get; set; }
    public string? ReferenceNumber { get; set; }
    public int? ReferenceId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public LedgerTransactionType TransactionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public int UserId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
