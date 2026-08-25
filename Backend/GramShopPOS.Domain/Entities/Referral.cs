using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class Referral : BaseEntity
{
    public int StoreId { get; set; }
    public int ReferrerCustomerId { get; set; }
    public int ReferredCustomerId { get; set; }
    public int? BillId { get; set; }
    public int? SalesPersonId { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public decimal SaleAmount { get; set; }
    public decimal DiscountGiven { get; set; }
    public decimal NewCustomerPercent { get; set; }
    public decimal ReferrerPercent { get; set; }
    public decimal RewardAmount { get; set; }
    public ReferralRewardStatus Status { get; set; } = ReferralRewardStatus.Pending;
    public DateTime ReferralDate { get; set; }

    public Store Store { get; set; } = null!;
    public Customer ReferrerCustomer { get; set; } = null!;
    public Customer ReferredCustomer { get; set; } = null!;
    public Bill? Bill { get; set; }
    public ApplicationUser? SalesPerson { get; set; }
    public ICollection<ReferralReward> Rewards { get; set; } = new List<ReferralReward>();
}
