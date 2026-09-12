namespace GramShopPOS.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string SalesPerson = "SalesPerson";
    public const string Customer = "Customer";

    public static readonly string[] All = [Admin, SalesPerson];
}
