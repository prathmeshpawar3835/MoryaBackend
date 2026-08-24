using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class WalletTransaction : BaseEntity
{
    public int CustomerId { get; set; }
    public int StoreId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public LedgerTransactionType TransactionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public int UserId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
