using FluentAssertions;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Tests;

public sealed class StoreAccessAndExchangeTests
{
    private static ReferralService Referrals(SqliteFixture fx) =>
        new(fx.Db, fx.User, new AuditService(fx.Db, fx.User));

    private static ReturnDocumentService Docs(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), Referrals(fx));

    private static BillingService Billing(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User), Referrals(fx), Docs(fx), new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));

    private static ReturnService Returns(SqliteFixture fx, BillingService billing) =>
        new(fx.Db, fx.User, billing, new AuditService(fx.Db, fx.User), Docs(fx));

    private static CustomerService Customers(SqliteFixture fx) =>
        new(fx.Db, fx.User, new AuditService(fx.Db, fx.User));

    private static void AsAdmin(SqliteFixture fx)
    {
        var admin = fx.Db.Users.First(u => u.UserName == "admin");
        fx.User.Role = Roles.Admin;
        fx.User.UserName = admin.UserName;
        fx.User.UserId = admin.Id;
        fx.User.AssignedStoreIds = [1];
    }

    private static void AsStoreUser(SqliteFixture fx, int storeId)
    {
        var sales = fx.Db.Users.First(u => u.UserName == "salesperson");
        fx.User.Role = Roles.SalesPerson;
        fx.User.UserName = sales.UserName;
        fx.User.UserId = sales.Id;
        fx.User.AssignedStoreIds = [storeId];
        fx.Db.StoreUsers.RemoveRange(fx.Db.StoreUsers.Where(s => s.UserId == sales.Id));
        fx.Db.StoreUsers.Add(new StoreUser { UserId = sales.Id, StoreId = storeId, IsPrimary = true, CreatedDate = DateTime.UtcNow });
        fx.Db.SaveChanges();
    }

    [Fact]
    public async Task Admin_sees_both_store_customers_and_same_mobile_is_allowed_across_stores()
    {
        await using var fx = new SqliteFixture();
        AsAdmin(fx);
        var customers = Customers(fx);
        var first = await customers.CreateAsync(new CreateCustomerRequest { StoreId = 1, Name = "Asha Store A", MobileNumber = "9811111111" });
        var second = await customers.CreateAsync(new CreateCustomerRequest { StoreId = 2, Name = "Asha Store B", MobileNumber = "9811111111" });

        first.StoreId.Should().Be(1);
        second.StoreId.Should().Be(2);
        first.Id.Should().NotBe(second.Id);

        var all = await customers.GetAsync(new PagedRequest { PageSize = 50 });
        all.Items.Select(c => c.Id).Should().Contain([first.Id, second.Id]);

        var storeB = await customers.GetAsync(new PagedRequest { StoreId = 2, PageSize = 50 });
        storeB.Items.Should().ContainSingle(c => c.Id == second.Id);
        storeB.Items.Should().NotContain(c => c.Id == first.Id);

        var act = () => customers.CreateAsync(new CreateCustomerRequest { StoreId = 1, Name = "Duplicate", MobileNumber = "9811111111" });
        await act.Should().ThrowAsync<ConflictAppException>();
    }

    [Fact]
    public async Task Store_user_cannot_read_or_change_another_store()
    {
        await using var fx = new SqliteFixture();
        AsAdmin(fx);
        var productId = fx.Db.Products.First().Id;
        fx.Db.Inventories.Add(new Inventory { StoreId = 2, ProductId = productId, Quantity = 5, IsActive = true, CreatedDate = DateTime.UtcNow });
        await fx.Db.SaveChangesAsync();

        var customers = Customers(fx);
        var billing = Billing(fx);
        var storeBCustomer = await customers.CreateAsync(new CreateCustomerRequest { StoreId = 2, Name = "Store B Only", MobileNumber = "9822222222" });
        var storeBBill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 2,
            CustomerId = storeBCustomer.Id,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });

        AsStoreUser(fx, 1);
        var storeUserCustomers = Customers(fx);
        var storeUserBilling = Billing(fx);

        var visible = await storeUserCustomers.GetAsync(new PagedRequest { PageSize = 50 });
        visible.Items.Should().NotContain(c => c.Id == storeBCustomer.Id);

        var read = () => storeUserCustomers.GetByIdAsync(storeBCustomer.Id);
        await read.Should().ThrowAsync<ForbiddenAppException>();

        var update = () => storeUserCustomers.UpdateAsync(storeBCustomer.Id, new UpdateCustomerRequest { Name = "Hijack", MobileNumber = "9822222222", IsActive = true });
        await update.Should().ThrowAsync<ForbiddenAppException>();

        var bill = () => storeUserBilling.GetBillAsync(storeBBill.Id);
        await bill.Should().ThrowAsync<ForbiddenAppException>();

        var create = () => storeUserCustomers.CreateAsync(new CreateCustomerRequest { StoreId = 2, Name = "Intruder", MobileNumber = "9833333333" });
        await create.Should().ThrowAsync<ForbiddenAppException>();
    }

    [Fact]
    public async Task Customer_ledgers_stay_on_their_own_store_records()
    {
        await using var fx = new SqliteFixture();
        AsAdmin(fx);
        var productId = fx.Db.Products.First().Id;
        fx.Db.Inventories.Add(new Inventory { StoreId = 2, ProductId = productId, Quantity = 5, IsActive = true, CreatedDate = DateTime.UtcNow });
        await fx.Db.SaveChangesAsync();

        var customers = Customers(fx);
        var billing = Billing(fx);
        var storeA = await customers.CreateAsync(new CreateCustomerRequest { StoreId = 1, Name = "Meena A", MobileNumber = "9844444444" });
        var storeB = await customers.CreateAsync(new CreateCustomerRequest { StoreId = 2, Name = "Meena B", MobileNumber = "9844444444" });
        var billA = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = storeA.Id,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        var billB = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 2,
            CustomerId = storeB.Id,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });

        var wrongStore = () => billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = storeB.Id,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        await wrongStore.Should().ThrowAsync<ValidationAppException>();

        var ledgerA = await customers.GetLedgerAsync(storeA.Id, new PagedRequest { PageSize = 20 });
        var ledgerB = await customers.GetLedgerAsync(storeB.Id, new PagedRequest { PageSize = 20 });
        ledgerA.Items.Should().Contain(l => l.ReferenceNumber == billA.BillNumber);
        ledgerA.Items.Should().NotContain(l => l.ReferenceNumber == billB.BillNumber);
        ledgerB.Items.Should().Contain(l => l.ReferenceNumber == billB.BillNumber);
        ledgerB.Items.Should().NotContain(l => l.ReferenceNumber == billA.BillNumber);
        fx.Db.CustomerLedgers.Where(l => l.CustomerId == storeA.Id).All(l => l.StoreId == 1).Should().BeTrue();
        fx.Db.CustomerLedgers.Where(l => l.CustomerId == storeB.Id).All(l => l.StoreId == 2).Should().BeTrue();
    }

    [Fact]
    public async Task Exchange_applies_credit_and_rejects_a_payment_that_does_not_match()
    {
        await using var fx = new SqliteFixture();
        var settings = fx.Db.BusinessSettings.First();
        settings.ExchangeDeductionPercent = 10;
        await fx.Db.SaveChangesAsync();

        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var returns = Returns(fx, billing);
        var original = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });

        var before = fx.Db.Returns.Count();
        var mismatch = () => returns.CreateExchangeAsync(new CreateExchangeRequest
        {
            OriginalBillId = original.Id,
            ReturnItems = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }],
            NewItems = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        await mismatch.Should().ThrowAsync<ValidationAppException>().WithMessage("*515.00*");
        fx.Db.ChangeTracker.Clear();
        fx.Db.Returns.Count().Should().Be(before);

        var exchange = await returns.CreateExchangeAsync(new CreateExchangeRequest
        {
            OriginalBillId = original.Id,
            ReturnItems = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }],
            NewItems = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 515 }]
        });
        exchange.Return.ReturnAmount.Should().Be(4635);
        exchange.NewBill.GrandTotal.Should().Be(5150);
        exchange.NewBill.PayableAmount.Should().Be(515);
        exchange.NewBill.DueAmount.Should().Be(0);
        exchange.DifferencePayable.Should().Be(515);
    }

    [Fact]
    public async Task Partial_then_second_exchange_uses_only_the_remaining_quantity()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var returns = Returns(fx, billing);
        var original = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 2 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 10300 }]
        });

        var first = await returns.CreateExchangeAsync(new CreateExchangeRequest
        {
            OriginalBillId = original.Id,
            ReturnItems = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }],
            NewItems = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }]
        });
        first.Return.ReturnAmount.Should().Be(5150);
        first.DifferencePayable.Should().Be(0);
        first.NewBill.DueAmount.Should().Be(0);

        var second = await returns.CreateExchangeAsync(new CreateExchangeRequest
        {
            OriginalBillId = original.Id,
            ReturnItems = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }],
            NewItems = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }]
        });
        second.Return.ReturnAmount.Should().Be(5150);

        var over = () => returns.CreateExchangeAsync(new CreateExchangeRequest
        {
            OriginalBillId = original.Id,
            ReturnItems = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 1 }],
            NewItems = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }]
        });
        await over.Should().ThrowAsync<BusinessAppException>();
    }
}
