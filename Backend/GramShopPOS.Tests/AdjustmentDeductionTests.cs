using FluentAssertions;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Tests;

public sealed class AdjustmentDeductionTests
{
    private static ReferralService Referrals(SqliteFixture fx) =>
        new(fx.Db, fx.User, new AuditService(fx.Db, fx.User));

    private static ReturnDocumentService Docs(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), Referrals(fx));

    private static BillingService Billing(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User), Referrals(fx), Docs(fx), new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));

    [Fact]
    public async Task Admin_deduction_reduces_return_and_buyback()
    {
        await using var fx = new SqliteFixture();
        var settings = fx.Db.BusinessSettings.First();
        settings.ReturnDeductionPercent = 10;
        settings.BuybackDeductionPercent = 20;
        await fx.Db.SaveChangesAsync();

        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var original = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });

        var returns = new ReturnService(fx.Db, fx.User, billing, new AuditService(fx.Db, fx.User), Docs(fx));
        var ret = await returns.CreateReturnAsync(new CreateReturnRequest
        {
            OriginalBillId = original.Id,
            Items = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }]
        });
        ret.GrossAmount.Should().Be(5150);
        ret.DeductionPercent.Should().Be(10);
        ret.DeductionAmount.Should().Be(515);
        ret.ReturnAmount.Should().Be(4635);

        var buybackOriginal = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        var buyback = await returns.CreateBuybackAsync(new CreateBuybackRequest
        {
            OriginalBillId = buybackOriginal.Id,
            Items = [new CreateReturnItemRequest { OriginalBillItemId = buybackOriginal.Items[0].Id, Quantity = 1 }]
        });
        buyback.GrossAmount.Should().Be(5150);
        buyback.DeductionPercent.Should().Be(20);
        buyback.ReturnAmount.Should().Be(4120);
    }

    [Fact]
    public async Task Combined_sale_applies_admin_return_deduction_not_counter_amount()
    {
        await using var fx = new SqliteFixture();
        var settings = fx.Db.BusinessSettings.First();
        settings.ReturnDeductionPercent = 10;
        await fx.Db.SaveChangesAsync();

        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var original = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        var combined = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 515 }],
            Adjustments =
            [
                new SaleAdjustmentRequest
                {
                    Kind = ReturnKind.Return,
                    OriginalBillId = original.Id,
                    Items = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }]
                }
            ]
        });
        combined.ReturnAdjustment.Should().Be(4635);
        combined.PayableAmount.Should().Be(515);
        combined.Adjustments.Should().ContainSingle(a => a.DeductionPercent == 10 && a.ReturnAmount == 4635);
    }

    [Fact]
    public async Task Admin_can_override_final_return_amount()
    {
        await using var fx = new SqliteFixture();
        var settings = fx.Db.BusinessSettings.First();
        settings.ReturnDeductionPercent = 10;
        await fx.Db.SaveChangesAsync();

        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var original = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        var returns = new ReturnService(fx.Db, fx.User, billing, new AuditService(fx.Db, fx.User), Docs(fx));
        var ret = await returns.CreateReturnAsync(new CreateReturnRequest
        {
            OriginalBillId = original.Id,
            Amount = 4500,
            Items = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }]
        });
        ret.GrossAmount.Should().Be(5150);
        ret.ReturnAmount.Should().Be(4500);
        ret.DeductionAmount.Should().Be(650);
    }

    [Fact]
    public async Task Salesperson_cannot_override_calculated_amount()
    {
        await using var fx = new SqliteFixture();
        fx.User.Role = Roles.SalesPerson;
        fx.User.UserName = "salesperson";
        fx.User.UserId = fx.Db.Users.First(u => u.UserName == "salesperson").Id;
        var settings = fx.Db.BusinessSettings.First();
        settings.ReturnDeductionPercent = 10;
        await fx.Db.SaveChangesAsync();

        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var original = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        var returns = new ReturnService(fx.Db, fx.User, billing, new AuditService(fx.Db, fx.User), Docs(fx));
        var ret = await returns.CreateReturnAsync(new CreateReturnRequest
        {
            OriginalBillId = original.Id,
            Amount = 100,
            Items = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }]
        });
        ret.ReturnAmount.Should().Be(4635);
    }
}
