namespace GramShopPOS.Domain.Entities;

public class Customer : BaseEntity
{
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public int? ReferredByCustomerId { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal WalletBalance { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Store Store { get; set; } = null!;
    public Customer? ReferredByCustomer { get; set; }
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<CustomerLedger> LedgerEntries { get; set; } = new List<CustomerLedger>();
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
