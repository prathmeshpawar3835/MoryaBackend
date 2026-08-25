using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class BirthdayMessageLog : BaseEntity
{
    public int CustomerId { get; set; }
    public int StoreId { get; set; }
    public int? BirthdayOfferId { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public DateOnly BirthdayDate { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? OfferName { get; set; }
    public WhatsAppMessageStatus Status { get; set; } = WhatsAppMessageStatus.Pending;
    public string? Error { get; set; }
    public DateTime? SentDate { get; set; }

    public Customer Customer { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public StoreDiscount? BirthdayOffer { get; set; }
}
