using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Customers;

public class CustomerDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsBirthday { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public int? ReferredByCustomerId { get; set; }
    public string? ReferredByName { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal WalletBalance { get; set; }
    public bool IsActive { get; set; }
    public bool HasCompletedSale { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateCustomerRequest
{
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ReferralCode { get; set; }
    public string? ReferringMobileNumber { get; set; }
}

public class UpdateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CustomerHistoryDto
{
    public CustomerDto Customer { get; set; } = null!;
    public IReadOnlyList<DTOs.Billing.BillDto> Bills { get; set; } = [];
    public IReadOnlyList<DTOs.Billing.ReturnDto> Returns { get; set; } = [];
}

public class LedgerEntryDto
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public LedgerTransactionType TransactionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class CustomerPaymentRequest
{
    public int StoreId { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }
}

public class WalletDto
{
    public int CustomerId { get; set; }
    public decimal Balance { get; set; }
    public IReadOnlyList<WalletTransactionDto> Transactions { get; set; } = [];
}

public class WalletTransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public LedgerTransactionType TransactionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class WalletRedeemRequest
{
    public int StoreId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class ReferralDto
{
    public int Id { get; set; }
    public int ReferrerCustomerId { get; set; }
    public string ReferrerName { get; set; } = string.Empty;
    public int ReferredCustomerId { get; set; }
    public string ReferredName { get; set; } = string.Empty;
    public decimal RewardAmount { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal DiscountGiven { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public string? BillNumber { get; set; }
    public ReferralRewardStatus Status { get; set; }
    public DateTime ReferralDate { get; set; }
}
