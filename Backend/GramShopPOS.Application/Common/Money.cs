namespace GramShopPOS.Application.Common;

public static class Money
{
    public static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
