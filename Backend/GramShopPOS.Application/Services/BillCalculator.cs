using GramShopPOS.Application.Common;

namespace GramShopPOS.Application.Services;

public sealed class BillLineCalculation
{
    public decimal Quantity { get; init; }
    public decimal Rate { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxPercent { get; init; }
    public decimal LineSubtotal { get; init; }
    public decimal Taxable { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal Total { get; init; }
}

public sealed class BillTotalsCalculation
{
    public decimal Subtotal { get; init; }
    public decimal ItemDiscountTotal { get; init; }
    public decimal BillDiscount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public IReadOnlyList<BillLineCalculation> Lines { get; init; } = [];
}

public static class BillCalculator
{
    public static BillLineCalculation CalculateLine(decimal quantity, decimal rate, decimal discountAmount, decimal taxPercent)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (rate < 0 || discountAmount < 0 || taxPercent < 0)
        {
            throw new ArgumentOutOfRangeException("Monetary values cannot be negative.");
        }

        var lineSubtotal = Money.Round(quantity * rate);
        if (discountAmount > lineSubtotal)
        {
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount cannot exceed line subtotal.");
        }

        var taxable = Money.Round(lineSubtotal - discountAmount);
        var tax = Money.Round(taxable * taxPercent / 100m);
        var total = Money.Round(taxable + tax);

        return new BillLineCalculation
        {
            Quantity = quantity,
            Rate = rate,
            DiscountAmount = Money.Round(discountAmount),
            TaxPercent = taxPercent,
            LineSubtotal = lineSubtotal,
            Taxable = taxable,
            TaxAmount = tax,
            Total = total
        };
    }

    public static BillTotalsCalculation CalculateTotals(
        IReadOnlyList<(decimal Quantity, decimal Rate, decimal DiscountAmount, decimal TaxPercent)> lines,
        decimal billDiscount)
    {
        if (billDiscount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(billDiscount));
        }

        var calculated = lines.Select(l => CalculateLine(l.Quantity, l.Rate, l.DiscountAmount, l.TaxPercent)).ToList();
        var itemDiscount = Money.Round(calculated.Sum(x => x.DiscountAmount));
        var subtotal = Money.Round(calculated.Sum(x => x.LineSubtotal));
        var netAfterItemDiscount = Money.Round(calculated.Sum(x => x.Taxable));

        if (billDiscount > netAfterItemDiscount)
        {
            throw new ArgumentOutOfRangeException(nameof(billDiscount), "Bill discount cannot exceed net amount.");
        }

        if (billDiscount == 0)
        {
            return new BillTotalsCalculation
            {
                Subtotal = subtotal,
                ItemDiscountTotal = itemDiscount,
                BillDiscount = 0,
                TaxAmount = Money.Round(calculated.Sum(x => x.TaxAmount)),
                GrandTotal = Money.Round(calculated.Sum(x => x.Total)),
                Lines = calculated
            };
        }

        var remainingDiscount = billDiscount;
        var adjusted = new List<BillLineCalculation>(calculated.Count);
        for (var i = 0; i < calculated.Count; i++)
        {
            var line = calculated[i];
            decimal share;
            if (i == calculated.Count - 1)
            {
                share = remainingDiscount;
            }
            else
            {
                share = netAfterItemDiscount == 0
                    ? 0
                    : Money.Round(billDiscount * (line.Taxable / netAfterItemDiscount));
                remainingDiscount -= share;
            }

            adjusted.Add(CalculateLine(line.Quantity, line.Rate, line.DiscountAmount + share, line.TaxPercent));
        }

        return new BillTotalsCalculation
        {
            Subtotal = subtotal,
            ItemDiscountTotal = itemDiscount,
            BillDiscount = Money.Round(billDiscount),
            TaxAmount = Money.Round(adjusted.Sum(x => x.TaxAmount)),
            GrandTotal = Money.Round(adjusted.Sum(x => x.Total)),
            Lines = adjusted
        };
    }

    public static void ValidatePayments(decimal grandTotal, decimal walletRedeemed, IReadOnlyList<decimal> payments, decimal creditAmount)
    {
        if (walletRedeemed < 0 || creditAmount < 0)
        {
            throw new ArgumentOutOfRangeException("Amounts cannot be negative.");
        }

        var paid = Money.Round(payments.Sum() + walletRedeemed);
        var required = Money.Round(grandTotal - creditAmount);
        if (paid != required)
        {
            throw new InvalidOperationException("Total payments must equal the required payment amount.");
        }
    }
}
