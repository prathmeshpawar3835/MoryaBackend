namespace GramShopPOS.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string SalesPerson = "SalesPerson";

    public static readonly string[] All = [Admin, SalesPerson];
}
