using FluentAssertions;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.DTOs.Operations;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Tests;

public sealed class ProductionUpgradeTests
{
    [Fact]
    public async Task New_customer_gets_unique_cus_code()
    {
        await using var fx = new SqliteFixture();
        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var created = await customers.CreateAsync(new CreateCustomerRequest
        {
            StoreId = 1,
            Name = "Priya",
            MobileNumber = "9888888888"
        });
        created.CustomerCode.Should().MatchRegex(@"^CUS\d{6}$");
        created.ReferralCode.Should().MatchRegex(@"^RF\d{8}$");
        created.CustomerCode.Should().NotBe(created.ReferralCode);
        var second = await customers.CreateAsync(new CreateCustomerRequest
        {
            StoreId = 1,
            Name = "Anika",
            MobileNumber = "9777777777"
        });
        second.ReferralCode.Should().NotBe(created.ReferralCode);
        var found = await customers.SearchAsync(created.CustomerCode, 1);
        found.Should().ContainSingle(c => c.Id == created.Id);
    }

    [Fact]
    public async Task Credit_overuse_on_sale_explains_available_balance()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = new BillingService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User),
            new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User)),
            new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))),
            new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));
        var act = async () => await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            WalletRedeemAmount = 2000,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 3150 }]
        });
        var ex = await act.Should().ThrowAsync<ValidationAppException>();
        ex.Which.Message.Should().Contain("Available customer credit is ₹500.00");
        ex.Which.Message.Should().Contain("You cannot use ₹2000.00");
    }

    [Fact]
    public async Task Repair_payment_posts_distinct_ledger_entries()
    {
        await using var fx = new SqliteFixture();
        var customer = fx.Db.Customers.First();
        var repairs = new RepairService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var job = await repairs.CreateAsync(new CreateRepairJobRequest
        {
            StoreId = 1,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            MobileNumber = customer.MobileNumber,
            ProductName = "1 Gram Chain",
            JobType = RepairJobType.Repair,
            EstimatedAmount = 2000,
            PaidAmount = 1000,
            PaymentMode = PaymentMode.Cash
        });
        job.EstimatedAmount.Should().Be(2000);
        job.PaidAmount.Should().Be(1000);
        job.DueAmount.Should().Be(1000);

        await repairs.CollectPaymentAsync(job.Id, new CollectRepairPaymentRequest
        {
            Amount = 1000,
            PaymentMode = PaymentMode.Upi,
            ReferenceNumber = "UPI-1"
        });
        var paid = await repairs.GetByIdAsync(job.Id);
        paid.DueAmount.Should().Be(0);
        paid.PaidAmount.Should().Be(2000);

        var ledger = await fx.Db.CustomerLedgers.AsNoTracking().Where(l => l.CustomerId == customer.Id).OrderBy(l => l.Id).ToListAsync();
        ledger.Should().Contain(l => l.TransactionType == LedgerTransactionType.RepairCharge && l.Debit == 2000);
        ledger.Should().Contain(l => l.TransactionType == LedgerTransactionType.RepairPayment && l.Credit == 1000);
        ledger.Count(l => l.TransactionType == LedgerTransactionType.RepairPayment).Should().Be(2);
    }
}
