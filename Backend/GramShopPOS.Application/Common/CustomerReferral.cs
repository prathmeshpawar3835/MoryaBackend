using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Common;

public static class CustomerReferral
{
    public static IQueryable<Customer> MatchingCode(IQueryable<Customer> customers, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return customers.Where(_ => false);
        }

        var trimmed = code.Trim();
        var upper = trimmed.ToUpperInvariant();
        return customers.Where(c =>
            c.ReferralCode == trimmed || c.ReferralCode == upper ||
            c.CustomerCode == trimmed || c.CustomerCode == upper);
    }

    public static async Task<string> NextCodeAsync(IAppDbContext db, CancellationToken cancellationToken)
    {
        string code;
        do
        {
            code = $"RF{Random.Shared.Next(10000000, 100000000):00000000}";
        } while (await db.Customers.AnyAsync(c => c.ReferralCode == code, cancellationToken));

        return code;
    }

    public static async Task<string> EnsureAsync(IAppDbContext db, int customerId, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstAsync(c => c.Id == customerId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(customer.ReferralCode))
        {
            return customer.ReferralCode;
        }

        customer.ReferralCode = await NextCodeAsync(db, cancellationToken);
        customer.UpdatedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return customer.ReferralCode;
    }
}
