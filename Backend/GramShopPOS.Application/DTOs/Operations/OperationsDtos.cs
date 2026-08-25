using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Operations;

public class StoreDiscountDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OfferCategory OfferCategory { get; set; } = OfferCategory.Store;
    public DiscountKind DiscountKind { get; set; }
    public decimal Value { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public class StoreDiscountRequest
{
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OfferCategory OfferCategory { get; set; } = OfferCategory.Store;
    public DiscountKind DiscountKind { get; set; } = DiscountKind.Percentage;
    public decimal Value { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SupplierDto
{
    public int Id { get; set; }
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalPurchased { get; set; }
}

public class SupplierRequest
{
    public int? StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RepairJobDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int? BillId { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDetails { get; set; }
    public RepairJobType JobType { get; set; }
    public RepairJobStatus Status { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<RepairJobHistoryDto> History { get; set; } = [];
}

public class RepairJobHistoryDto
{
    public RepairJobStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class CreateRepairJobRequest
{
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int? BillId { get; set; }
    public int? BillItemId { get; set; }
    public int? ProductId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDetails { get; set; }
    public RepairJobType JobType { get; set; } = RepairJobType.Repair;
    public DateTime? ExpectedDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateRepairJobRequest
{
    public RepairJobStatus Status { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string? Notes { get; set; }
}

public class SalesPersonOptionDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ReferralValidationDto
{
    public bool Valid { get; set; }
    public string? Message { get; set; }
    public int? ReferrerCustomerId { get; set; }
    public string? ReferrerName { get; set; }
    public string? ReferrerMobile { get; set; }
    public string? ReferrerCode { get; set; }
    public decimal ReferrerWalletBalance { get; set; }
    public decimal NewCustomerDiscountRate { get; set; }
    public decimal ReferrerBenefitRate { get; set; }
    public RewardType RewardType { get; set; }
}

public class ReferralPreviewDto
{
    public bool Applies { get; set; }
    public decimal EligibleAmount { get; set; }
    public decimal NewCustomerDiscount { get; set; }
    public decimal ReferrerBenefit { get; set; }
    public ReferralValidationDto? Referrer { get; set; }
}

public class BirthdayOfferSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountKind DiscountKind { get; set; }
    public decimal Value { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
}

public class BirthdayEligibilityDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public bool IsBirthdayToday { get; set; }
    public bool AlreadyRedeemed { get; set; }
    public string? RedeemedInvoiceNumber { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<BirthdayOfferSummaryDto> Offers { get; set; } = [];
}

public class DailyBirthdayRunResult
{
    public int CustomersFound { get; set; }
    public int MessagesSent { get; set; }
    public int MessagesFailed { get; set; }
    public int MessagesSkipped { get; set; }
}

public sealed record BirthdayDiscountApplication(
    decimal Amount,
    decimal Percent,
    string? Name,
    int? OfferId,
    string? Description)
{
    public static BirthdayDiscountApplication None { get; } = new(0, 0, null, null, null);
    public bool Applies => Amount > 0 && OfferId.HasValue;
}
