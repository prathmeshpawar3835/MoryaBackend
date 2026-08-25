using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class ReferralReward : BaseEntity
{
    public int ReferralId { get; set; }
    public int CustomerId { get; set; }
    public int? BillId { get; set; }
    public int? ReturnId { get; set; }
    public int? LedgerEntryId { get; set; }
    public decimal Amount { get; set; }
    public ReferralRewardStatus Status { get; set; } = ReferralRewardStatus.Pending;
    public bool IsReferrerReward { get; set; }
    public bool IsReversal { get; set; }
    public string Description { get; set; } = string.Empty;

    public Referral Referral { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Bill? Bill { get; set; }
    public ProductReturn? Return { get; set; }
    public CustomerLedger? LedgerEntry { get; set; }
}
