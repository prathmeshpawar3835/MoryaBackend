using FluentAssertions;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Tests;

public class BirthdayOfferTests
{
    private static IBirthdayService Birthdays(SqliteFixture fx, IWhatsAppService? wa = null) =>
        new BirthdayService(fx.Db, fx.User, wa ?? new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User));

    private static BillingService Billing(SqliteFixture fx, IBirthdayService? birthdays = null) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User),
            new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User)),
            new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db),
                new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))),
            birthdays ?? Birthdays(fx));

    private static StoreDiscount AddOffer(SqliteFixture fx, int storeId = 1, decimal value = 10, bool active = true, DiscountKind kind = DiscountKind.Percentage)
    {
        var offer = new StoreDiscount
        {
            StoreId = storeId,
            Name = "Birthday Special Offer",
            Description = "Valid only on your birthday",
            OfferCategory = OfferCategory.Birthday,
            DiscountKind = kind,
            Value = value,
            IsActive = active,
            CreatedDate = DateTime.UtcNow
        };
        fx.Db.StoreDiscounts.Add(offer);
        fx.Db.SaveChanges();
        return offer;
    }

    private static async Task<Customer> CreateBirthdayCustomerAsync(SqliteFixture fx, DateOnly? dob, string mobile = "9876543210", int storeId = 1, string name = "Prathmesh")
    {
        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var dto = await customers.CreateAsync(new CreateCustomerRequest
        {
            StoreId = storeId,
            Name = name,
            MobileNumber = mobile,
            DateOfBirth = dob
        });
        return fx.Db.Customers.First(c => c.Id == dto.Id);
    }

    [Fact]
    public async Task Eligibility_detects_birthday_today_ignoring_year()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var offer = AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var eligibility = await Birthdays(fx).GetEligibilityAsync(customer.Id, 1);
        eligibility.IsBirthdayToday.Should().BeTrue();
        eligibility.AlreadyRedeemed.Should().BeFalse();
        eligibility.Offers.Should().ContainSingle(o => o.Id == offer.Id && o.Value == 10);
    }

    [Fact]
    public async Task Eligibility_is_false_for_tomorrow_yesterday_and_missing_dob()
    {
        await using var fx = new SqliteFixture();
        AddOffer(fx);
        var today = BusinessCalendar.Today();
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var tmr = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, tomorrow.Month, tomorrow.Day), "9000000001");
        var yest = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, yesterday.Month, yesterday.Day), "9000000002", name: "Yesterday");
        var none = await CreateBirthdayCustomerAsync(fx, null, "9000000003", name: "NoDob");
        (await Birthdays(fx).GetEligibilityAsync(tmr.Id, 1)).IsBirthdayToday.Should().BeFalse();
        (await Birthdays(fx).GetEligibilityAsync(yest.Id, 1)).IsBirthdayToday.Should().BeFalse();
        var missing = await Birthdays(fx).GetEligibilityAsync(none.Id, 1);
        missing.IsBirthdayToday.Should().BeFalse();
        missing.Offers.Should().BeEmpty();
    }

    [Fact]
    public async Task Inactive_or_missing_offer_is_not_available()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var none = await Birthdays(fx).GetEligibilityAsync(customer.Id, 1);
        none.Offers.Should().BeEmpty();
        none.Message.Should().Contain("No birthday offer");
        AddOffer(fx, active: false);
        var inactive = await Birthdays(fx).GetEligibilityAsync(customer.Id, 1);
        inactive.Offers.Should().BeEmpty();
    }

    [Fact]
    public async Task Store_specific_offer_is_not_shown_or_applied_on_another_store()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var storeB = AddOffer(fx, storeId: 2, value: 20);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        (await Birthdays(fx).GetEligibilityAsync(customer.Id, 1)).Offers.Should().BeEmpty();
        (await Birthdays(fx).GetEligibilityAsync(customer.Id, 2)).Offers.Should().ContainSingle(o => o.Id == storeB.Id);
        var act = async () => await Billing(fx).CreateBillAsync(Sale(customer.Id, storeB.Id, 4635));
        await act.Should().ThrowAsync<BusinessAppException>().WithMessage("*not valid for the current store*");
    }

    [Fact]
    public async Task WhatsApp_job_sends_once_and_does_not_duplicate()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var wa = new RecordingWhatsAppService();
        var svc = Birthdays(fx, wa);
        var first = await svc.ProcessDailyAsync();
        first.CustomersFound.Should().BeGreaterThanOrEqualTo(1);
        first.MessagesSent.Should().Be(1);
        wa.Sent.Should().ContainSingle();
        wa.Sent[0].Message.Should().Contain(customer.Name);
        wa.Sent[0].Message.Should().Contain("ONLY TODAY");
        wa.Sent[0].Message.Should().Contain("Birthday Special Offer");
        var second = await svc.ProcessDailyAsync();
        second.MessagesSkipped.Should().BeGreaterThanOrEqualTo(1);
        wa.Sent.Should().ContainSingle();
        var log = fx.Db.BirthdayMessageLogs.Single(l => l.CustomerId == customer.Id);
        log.Status.Should().Be(WhatsAppMessageStatus.Sent);
    }

    [Fact]
    public async Task WhatsApp_failure_is_logged_and_can_retry()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var wa = new RecordingWhatsAppService { Succeed = false };
        var svc = Birthdays(fx, wa);
        var first = await svc.ProcessDailyAsync();
        first.MessagesFailed.Should().Be(1);
        fx.Db.BirthdayMessageLogs.Single(l => l.CustomerId == customer.Id).Status.Should().Be(WhatsAppMessageStatus.Failed);
        wa.Succeed = true;
        var retry = await svc.ProcessDailyAsync();
        retry.MessagesSent.Should().Be(1);
        fx.Db.BirthdayMessageLogs.Single(l => l.CustomerId == customer.Id).Status.Should().Be(WhatsAppMessageStatus.Sent);
    }

    [Fact]
    public async Task Sale_applies_selected_birthday_offer_and_snapshots_invoice()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var offer = AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var bill = await Billing(fx).CreateBillAsync(Sale(customer.Id, offer.Id, 4635));
        bill.BirthdayDiscount.Should().Be(500);
        bill.BirthdayDiscountPercent.Should().Be(10);
        bill.BirthdayOfferName.Should().Be("Birthday Special Offer");
        bill.GrandTotal.Should().Be(4635);
        var invoice = await Billing(fx).GetInvoiceAsync(bill.Id);
        invoice.BirthdayDiscount.Should().Be(500);
        invoice.BirthdayOfferName.Should().Be("Birthday Special Offer");
        invoice.DiscountLines.Should().Contain(l => l.Type == "Birthday" && l.Amount == 500);
        invoice.CustomerName.Should().Be("Prathmesh");
        invoice.CustomerMobile.Should().Be("9876543210");
        offer.Value = 25;
        fx.Db.SaveChanges();
        var afterChange = await Billing(fx).GetInvoiceAsync(bill.Id);
        afterChange.BirthdayDiscountPercent.Should().Be(10);
        afterChange.BirthdayDiscount.Should().Be(500);
    }

    [Fact]
    public async Task Birthday_offer_is_not_auto_applied_without_selection()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var bill = await Billing(fx).CreateBillAsync(Sale(customer.Id, null, 5150));
        bill.BirthdayDiscount.Should().Be(0);
        bill.GrandTotal.Should().Be(5150);
    }

    [Fact]
    public async Task Second_redemption_on_same_birthday_is_blocked()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var offer = AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        await Billing(fx).CreateBillAsync(Sale(customer.Id, offer.Id, 4635));
        var eligibility = await Birthdays(fx).GetEligibilityAsync(customer.Id, 1);
        eligibility.AlreadyRedeemed.Should().BeTrue();
        eligibility.Offers.Should().BeEmpty();
        eligibility.Message.Should().Contain("Already Redeemed");
        var act = async () => await Billing(fx).CreateBillAsync(Sale(customer.Id, offer.Id, 4635));
        await act.Should().ThrowAsync<BusinessAppException>().WithMessage("*already redeemed*");
    }

    [Fact]
    public async Task Birthday_offer_stacks_with_store_discount()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var birthday = AddOffer(fx);
        fx.Db.StoreDiscounts.Add(new StoreDiscount
        {
            StoreId = 1,
            Name = "Store Promo",
            OfferCategory = OfferCategory.Store,
            DiscountKind = DiscountKind.Percentage,
            Value = 5,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        fx.Db.SaveChanges();
        var storeOfferId = fx.Db.StoreDiscounts.Single(d => d.Name == "Store Promo").Id;
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var bill = await Billing(fx).CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customer.Id,
            BirthdayOfferId = birthday.Id,
            StoreDiscountId = storeOfferId,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 4377.50m }]
        });
        bill.BirthdayDiscount.Should().Be(500);
        bill.StoreDiscountAmount.Should().Be(250);
        bill.GrandTotal.Should().Be(4377.50m);
        var invoice = await Billing(fx).GetInvoiceAsync(bill.Id);
        invoice.DiscountLines.Should().Contain(l => l.Type == "Birthday" && l.Amount == 500);
        invoice.DiscountLines.Should().Contain(l => l.Type == "Store" && l.Amount == 250);
    }

    [Fact]
    public async Task Return_and_exchange_keep_original_redemption()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var offer = AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var billing = Billing(fx);
        var bill = await billing.CreateBillAsync(Sale(customer.Id, offer.Id, 4635));
        var returns = new ReturnService(fx.Db, fx.User, billing, new AuditService(fx.Db, fx.User),
            new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db),
                new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))));
        await returns.CreateReturnAsync(new CreateReturnRequest
        {
            OriginalBillId = bill.Id,
            Items = [new CreateReturnItemRequest { OriginalBillItemId = bill.Items[0].Id, Quantity = 1 }]
        });
        fx.Db.BirthdayOfferRedemptions.Should().ContainSingle(r => r.BillId == bill.Id && r.Status == BirthdayRedemptionStatus.Redeemed);
        (await Birthdays(fx).GetEligibilityAsync(customer.Id, 1)).AlreadyRedeemed.Should().BeTrue();

        var walkIn = fx.Db.Customers.First(c => c.MobileNumber == "9000000000");
        var original2 = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = walkIn.Id,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = walkIn.Id,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [],
            Adjustments =
            [
                new SaleAdjustmentRequest
                {
                    Kind = ReturnKind.Exchange,
                    OriginalBillId = original2.Id,
                    Items = [new CreateReturnItemRequest { OriginalBillItemId = original2.Items[0].Id, Quantity = 1 }]
                }
            ]
        });
        fx.Db.BirthdayOfferRedemptions.Should().ContainSingle(r => r.BillId == bill.Id && r.Status == BirthdayRedemptionStatus.Redeemed);
        (await Billing(fx).GetInvoiceAsync(bill.Id)).BirthdayDiscount.Should().Be(500);
    }

    [Fact]
    public async Task Cancelled_sale_releases_birthday_offer()
    {
        await using var fx = new SqliteFixture();
        var today = BusinessCalendar.Today();
        var offer = AddOffer(fx);
        var customer = await CreateBirthdayCustomerAsync(fx, new DateOnly(2000, today.Month, today.Day));
        var billing = Billing(fx);
        var bill = await billing.CreateBillAsync(Sale(customer.Id, offer.Id, 4635));
        await billing.CancelBillAsync(bill.Id, "test cancel");
        fx.Db.BirthdayOfferRedemptions.Single(r => r.BillId == bill.Id).Status.Should().Be(BirthdayRedemptionStatus.Cancelled);
        var eligibility = await Birthdays(fx).GetEligibilityAsync(customer.Id, 1);
        eligibility.AlreadyRedeemed.Should().BeFalse();
        eligibility.Offers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Future_date_of_birth_is_rejected()
    {
        await using var fx = new SqliteFixture();
        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var act = async () => await customers.CreateAsync(new CreateCustomerRequest
        {
            StoreId = 1,
            Name = "Future",
            MobileNumber = "9000000099",
            DateOfBirth = BusinessCalendar.Today().AddDays(1)
        });
        await act.Should().ThrowAsync<ValidationAppException>().WithMessage("*future*");
    }

    private static CreateBillRequest Sale(int customerId, int? offerId, decimal paid) => new()
    {
        StoreId = 1,
        CustomerId = customerId,
        BirthdayOfferId = offerId,
        Items = [new CreateBillItemRequest { ProductId = 1, Quantity = 1 }],
        Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = paid }]
    };
}
