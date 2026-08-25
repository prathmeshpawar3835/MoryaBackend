using GramShopPOS.Application.Common;
using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.Services;

public static class ReferralCalculator
{
    public static decimal ComputeBenefit(decimal eligibleAmount, decimal rate, RewardType type)
    {
        if (eligibleAmount <= 0 || rate <= 0)
        {
            return 0;
        }

        var amount = type == RewardType.Percentage
            ? Money.Round(eligibleAmount * rate / 100m)
            : Money.Round(rate);
        return amount > eligibleAmount ? eligibleAmount : amount;
    }

    public static decimal RemainingBenefit(decimal originalBenefit, decimal originalEligible, decimal remainingEligible)
    {
        if (originalBenefit <= 0 || originalEligible <= 0)
        {
            return 0;
        }

        if (remainingEligible <= 0)
        {
            return 0;
        }

        var remaining = Money.Round(originalBenefit * (remainingEligible / originalEligible));
        return remaining > originalBenefit ? originalBenefit : remaining;
    }
}
