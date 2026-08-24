namespace GramShopPOS.Domain.Constants;

public static class SortWhitelist
{
    public static readonly HashSet<string> Products = new(StringComparer.OrdinalIgnoreCase)
    {
        "productcode", "productname", "barcode", "sellingprice", "purchaseprice", "mrp", "createddate", "id"
    };

    public static readonly HashSet<string> Customers = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "mobilenumber", "createddate", "outstandingbalance", "id"
    };

    public static readonly HashSet<string> Bills = new(StringComparer.OrdinalIgnoreCase)
    {
        "billnumber", "billdate", "grandtotal", "paidamount", "dueamount", "id", "createddate"
    };

    public static readonly HashSet<string> Inventory = new(StringComparer.OrdinalIgnoreCase)
    {
        "quantity", "productname", "productcode", "id"
    };

    public static readonly HashSet<string> Purchases = new(StringComparer.OrdinalIgnoreCase)
    {
        "purchasedate", "invoicenumber", "suppliername", "total", "id"
    };

    public static readonly HashSet<string> Returns = new(StringComparer.OrdinalIgnoreCase)
    {
        "returndate", "returnnumber", "returnamount", "id"
    };
}
