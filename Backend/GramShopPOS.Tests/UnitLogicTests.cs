using FluentAssertions;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Tests;

public class BillCalculatorTests
{
    [Fact]
    public void Calculates_line_tax_and_total_with_decimal_rounding()
    {
        var line = BillCalculator.CalculateLine(2, 100.555m, 10, 3);
        line.LineSubtotal.Should().Be(201.11m);
        line.Taxable.Should().Be(191.11m);
        line.TaxAmount.Should().Be(5.73m);
        line.Total.Should().Be(196.84m);
    }

    [Fact]
    public void Rejects_discount_greater_than_line()
    {
        var act = () => BillCalculator.CalculateLine(1, 100, 120, 3);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Payment_validation_requires_exact_cover_including_credit()
    {
        var act = () => BillCalculator.ValidatePayments(5000, 0, [2000, 2000], 500);
        act.Should().Throw<InvalidOperationException>();
        var ok = () => BillCalculator.ValidatePayments(5000, 0, [2000, 2000], 1000);
        ok.Should().NotThrow();
    }
}

public class ReferralCalculatorTests
{
    [Fact]
    public void Percentage_benefit_matches_business_example()
    {
        ReferralCalculator.ComputeBenefit(10000, 10, RewardType.Percentage).Should().Be(1000);
        ReferralCalculator.ComputeBenefit(10000, 5, RewardType.Percentage).Should().Be(500);
    }

    [Fact]
    public void Return_reduces_referrer_benefit_proportionally()
    {
        ReferralCalculator.RemainingBenefit(500, 10000, 6000).Should().Be(300);
        ReferralCalculator.RemainingBenefit(500, 10000, 0).Should().Be(0);
    }
}

public class StoreIsolationTests
{
    [Fact]
    public void Salesperson_cannot_access_another_store()
    {
        var access = new StoreAccessService([1], Roles.SalesPerson);
        var act = () => access.EnsureStoreAccess(2);
        act.Should().Throw<ForbiddenAppException>();
        access.CanAccessStore(1).Should().BeTrue();
    }

    [Fact]
    public void Salesperson_storeId_query_is_rejected_not_ignored()
    {
        var access = new StoreAccessService([1], Roles.SalesPerson);
        var act = () => access.ResolveStoreId(2);
        act.Should().Throw<ForbiddenAppException>().WithMessage("*requested store*");
    }

    [Fact]
    public void Admin_can_access_any_store()
    {
        var access = new StoreAccessService([], Roles.Admin);
        access.EnsureStoreAccess(99);
        access.ResolveStoreId(99).Should().Be(99);
    }
}

public class PasswordServiceTests
{
    [Fact]
    public void Hashes_and_verifies_and_rejects_weak_passwords()
    {
        var svc = new PasswordService();
        var hash = svc.Hash("ChangeMe@123");
        hash.Should().NotBe("ChangeMe@123");
        svc.Verify("ChangeMe@123", hash).Should().BeTrue();
        var act = () => svc.ValidateStrength("weak");
        act.Should().Throw<ValidationAppException>();
    }
}
