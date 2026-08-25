using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;

namespace GramShopPOS.Application.Services;

public static class InvoiceDiscountComposer
{
    public static IReadOnlyList<InvoiceDiscountLineDto> Build(
        decimal itemDiscount,
        decimal combinedBillDiscount,
        decimal referralDiscount,
        decimal referralPercent,
        decimal storeDiscount,
        decimal storePercent,
        string? storeDiscountName,
        decimal birthdayDiscount,
        decimal birthdayPercent,
        string? birthdayOfferName)
    {
        var lines = new List<InvoiceDiscountLineDto>();
        Add(lines, "Item", "Item Discount", itemDiscount, null, "Line-item discount on this invoice");
        Add(
            lines,
            "Referral",
            "Referral Discount",
            referralDiscount,
            referralPercent,
            "New customer referral discount on this invoice only");
        Add(
            lines,
            "Birthday",
            string.IsNullOrWhiteSpace(birthdayOfferName) ? "Birthday Offer" : birthdayOfferName,
            birthdayDiscount,
            birthdayPercent,
            "Birthday offer on this invoice only — valid on the customer's birthday");
        Add(
            lines,
            "Store",
            string.IsNullOrWhiteSpace(storeDiscountName) ? "Store Discount" : storeDiscountName,
            storeDiscount,
            storePercent,
            "Store discount applied on this invoice");
        var other = Money.Round(combinedBillDiscount - referralDiscount - storeDiscount - birthdayDiscount);
        Add(lines, "Other", "Other Discount", other, null, "Admin / bill discount on this invoice");
        return lines;
    }

    public static decimal Total(IReadOnlyList<InvoiceDiscountLineDto> lines) =>
        Money.Round(lines.Sum(l => l.Amount));

    public static decimal OtherAmount(decimal combinedBillDiscount, decimal referralDiscount, decimal storeDiscount, decimal birthdayDiscount) =>
        Money.Round(combinedBillDiscount - referralDiscount - storeDiscount - birthdayDiscount);

    private static void Add(
        List<InvoiceDiscountLineDto> lines,
        string type,
        string name,
        decimal amount,
        decimal? percent,
        string reason)
    {
        if (amount <= 0)
        {
            return;
        }

        lines.Add(new InvoiceDiscountLineDto
        {
            Type = type,
            Name = name,
            Amount = amount,
            Percent = percent is > 0 ? percent : null,
            Reason = reason
        });
    }
}
