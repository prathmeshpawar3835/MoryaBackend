using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class BusinessSetting : BaseEntity
{
    public string ShopName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? Address { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? GSTNumber { get; set; }
    public string? InvoiceFooter { get; set; }
    public string? ReturnPolicy { get; set; }
    public string InvoicePrefix { get; set; } = "INV";
    public string InvoiceNumberFormat { get; set; } = "{PREFIX}-FY{FY}-{SEQ:000000}";
    public int FinancialYearStartMonth { get; set; } = 4;
    public bool AllowNegativeStock { get; set; }
    public decimal DefaultTaxPercent { get; set; }
    public decimal LowStockDefaultLevel { get; set; } = 5;
    public decimal NewCustomerReward { get; set; } = 10;
    public decimal ReferrerReward { get; set; } = 5;
    public RewardType RewardType { get; set; } = RewardType.Percentage;
    public RewardTrigger RewardTrigger { get; set; } = RewardTrigger.FirstPurchase;
    public bool ReferralStoreWise { get; set; }
    public bool ReferralEnabled { get; set; } = true;
    public decimal BirthdayDiscountPercent { get; set; }
    public decimal ReturnDeductionPercent { get; set; }
    public decimal ExchangeDeductionPercent { get; set; }
    public decimal BuybackDeductionPercent { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public string? WhatsAppPhoneNumberId { get; set; }
    public string? WhatsAppAccessToken { get; set; }
    public string? WhatsAppApiBaseUrl { get; set; }
}
