using GramShopPOS.Application.Exceptions;

namespace GramShopPOS.Application.Common;

public static class CreditUsage
{
    public static string OveruseMessage(decimal available, decimal requested) =>
        $"Available customer credit is ₹{available:0.00}. You cannot use ₹{requested:0.00}. Please enter an amount up to ₹{available:0.00}.";

    public static void EnsureWithinBalance(decimal available, decimal requested)
    {
        if (requested <= 0)
        {
            return;
        }

        if (requested > available)
        {
            throw new ValidationAppException(OveruseMessage(available, requested));
        }
    }
}
