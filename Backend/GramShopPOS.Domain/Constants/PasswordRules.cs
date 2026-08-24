namespace GramShopPOS.Domain.Constants;

public static class PasswordRules
{
    public const int MinLength = 8;
    public const int LockoutThreshold = 5;
    public const int LockoutMinutes = 15;
}
