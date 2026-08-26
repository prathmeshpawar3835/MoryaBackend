using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.Common;

public static class AdjustmentDeduction
{
    public static decimal ClampPercent(decimal percent) =>
        percent < 0 ? 0 : percent > 100 ? 100 : Money.Round(percent);

    public static decimal PercentFor(ReturnKind kind, BusinessSetting settings) =>
        ClampPercent(kind switch
        {
            ReturnKind.Exchange => settings.ExchangeDeductionPercent,
            ReturnKind.Buyback => settings.BuybackDeductionPercent,
            _ => settings.ReturnDeductionPercent
        });

    public static decimal DeductionOf(decimal gross, decimal percent) =>
        Money.Round(gross * ClampPercent(percent) / 100m);

    public static decimal NetOf(decimal gross, decimal percent) =>
        Money.Round(gross - DeductionOf(gross, percent));
}
