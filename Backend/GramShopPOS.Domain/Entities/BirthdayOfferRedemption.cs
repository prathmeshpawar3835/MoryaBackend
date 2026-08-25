using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class BirthdayOfferRedemption : BaseEntity
{
    public int CustomerId { get; set; }
    public int StoreId { get; set; }
    public int BirthdayOfferId { get; set; }
    public int BillId { get; set; }
    public int SalesPersonId { get; set; }
    public DateOnly BirthdayDate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public BirthdayRedemptionStatus Status { get; set; } = BirthdayRedemptionStatus.Redeemed;

    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public StoreDiscount BirthdayOffer { get; set; } = null!;
    public Bill Bill { get; set; } = null!;
    public ApplicationUser SalesPerson { get; set; } = null!;
}
