using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Settings;

public class SettingsDto
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
    public decimal LowStockDefaultLevel { get; set; }
    public bool ReferralEnabled { get; set; }
    public decimal NewCustomerReward { get; set; }
    public decimal ReferrerReward { get; set; }
    public RewardType RewardType { get; set; }
    public RewardTrigger RewardTrigger { get; set; }
    public bool ReferralStoreWise { get; set; }
    public IReadOnlyList<TaxSettingDto> TaxSettings { get; set; } = [];
}

public class UpdateSettingsRequest : SettingsDto
{
}

public class TaxSettingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public bool IsDefault { get; set; }
}

public class AuditLogDto
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public int? StoreId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedDate { get; set; }
}
