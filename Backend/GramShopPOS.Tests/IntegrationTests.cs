using FluentAssertions;
using GramShopPOS.Application.DTOs.Auth;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GramShopPOS.Tests;

public class IntegrationTests
{
    private static (AuthService Auth, AuditService Audit) Auth(SqliteFixture fx)
    {
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(x => x.CreateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<int>>()))
            .Returns(("token", DateTime.UtcNow.AddHours(1), "jti"));
        var env = new Mock<IAppEnvironment>();
        env.SetupGet(x => x.IsDevelopment).Returns(true);
        var audit = new AuditService(fx.Db, fx.User);
        var auth = new AuthService(fx.Db, fx.Passwords, jwt.Object, fx.User, audit, env.Object);
        return (auth, audit);
    }

    private static ProductService Products(SqliteFixture fx)
    {
        var excel = new Mock<IExcelWorkbookService>();
        return new ProductService(fx.Db, fx.User, new AuditService(fx.Db, fx.User), new StockEngine(fx.Db), excel.Object);
    }

    private static ReferralService Referrals(SqliteFixture fx) =>
        new(fx.Db, fx.User, new AuditService(fx.Db, fx.User));

    private static ReturnDocumentService Docs(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), Referrals(fx));

    private static BillingService Billing(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User), Referrals(fx), Docs(fx), new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));

    [Fact]
    public async Task Admin_and_salesperson_can_login()
    {
        await using var fx = new SqliteFixture();
        var (auth, _) = Auth(fx);
        var admin = await auth.LoginAsync(new LoginRequest { UserName = "admin", Password = "ChangeMe@123" });
        admin.Role.Should().Be(Roles.Admin);
        admin.AccessToken.Should().NotBeNullOrWhiteSpace();
        var sales = await auth.LoginAsync(new LoginRequest { UserName = "salesperson", Password = "ChangeMe@123" });
        sales.Role.Should().Be(Roles.SalesPerson);
        sales.AssignedStores.Should().ContainSingle(s => s.StoreId == 1);
    }

    [Fact]
    public async Task Product_and_category_crud_works()
    {
        await using var fx = new SqliteFixture();
        var categories = new CategoryService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var createdCat = await categories.CreateAsync(new CreateCategoryRequest { Name = "Bangles" });
        createdCat.Name.Should().Be("Bangles");
        var products = Products(fx);
        var product = await products.CreateAsync(new CreateProductRequest
        {
            ProductCode = "1G-BAN-001",
            ProductName = "Bangle",
            CategoryId = createdCat.Id,
            PurchasePrice = 100,
            SellingPrice = 150,
            MRP = 160,
            TaxPercent = 3,
            OpeningStockStoreId = 1,
            OpeningStock = 5
        });
        product.ProductCode.Should().Be("1G-BAN-001");
        var updated = await products.UpdateAsync(product.Id, new UpdateProductRequest
        {
            ProductName = "Bangle Updated",
            CategoryId = createdCat.Id,
            Unit = "PCS",
            PurchasePrice = 110,
            SellingPrice = 160,
            MRP = 170,
            TaxPercent = 3,
            IsActive = true
        });
        updated.ProductName.Should().Be("Bangle Updated");
        await products.DeleteAsync(product.Id);
        var act = async () => await products.GetByIdAsync(product.Id, 1);
        await act.Should().ThrowAsync<NotFoundAppException>();
    }

    [Fact]
    public async Task Salesperson_cannot_read_other_store_inventory()
    {
        await using var fx = new SqliteFixture();
        fx.User.Role = Roles.SalesPerson;
        fx.User.UserId = fx.Db.Users.First(u => u.UserName == "salesperson").Id;
        fx.User.AssignedStoreIds = [1];
        var inventory = new InventoryService(fx.Db, fx.User, new StockEngine(fx.Db), new AuditService(fx.Db, fx.User));
        var act = async () => await inventory.GetByProductAsync(fx.Db.Products.First().Id, 2);
        await act.Should().ThrowAsync<ForbiddenAppException>();
    }

    [Fact]
    public async Task Purchase_increases_stock_and_creates_movement()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var purchases = new PurchaseService(fx.Db, fx.User, new StockEngine(fx.Db), new AuditService(fx.Db, fx.User));
        var purchase = await purchases.CreateAsync(new CreatePurchaseRequest
        {
            StoreId = 1,
            SupplierName = "Wholesaler",
            InvoiceNumber = "P-1",
            Items = [new CreatePurchaseItemRequest { ProductId = productId, Quantity = 4, PurchasePrice = 4100 }]
        });
        purchase.Total.Should().Be(16400);
        (await fx.Db.Inventories.AsNoTracking().FirstAsync(i => i.ProductId == productId)).Quantity.Should().Be(14);
        fx.Db.StockMovements.Any(m => m.MovementType == StockMovementType.Purchase).Should().BeTrue();
    }

    [Fact]
    public async Task Bill_creation_deducts_stock_and_generates_unique_number()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var bill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 2 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 10300 }]
        });
        bill.BillNumber.Should().StartWith("STORE01-FY");
        bill.GrandTotal.Should().Be(10300);
        bill.DueAmount.Should().Be(0);
        (await fx.Db.Inventories.AsNoTracking().FirstAsync(i => i.ProductId == productId)).Quantity.Should().Be(8);
    }

    [Fact]
    public async Task Credit_sale_creates_customer_due()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var billing = Billing(fx);
        var bill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Credit, Amount = 5150 }]
        });
        bill.DueAmount.Should().Be(5150);
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId)).OutstandingBalance.Should().Be(5150);

        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        await customers.ReceivePaymentAsync(customerId, new CustomerPaymentRequest
        {
            StoreId = 1,
            PaymentMode = PaymentMode.Upi,
            Amount = 150
        });
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId)).OutstandingBalance.Should().Be(5000);
    }

    [Fact]
    public async Task Return_restores_inventory_without_changing_original_bill()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var billing = Billing(fx);
        var bill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = fx.Db.Customers.First().Id,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
        });
        var returns = new ReturnService(fx.Db, fx.User, billing, new AuditService(fx.Db, fx.User), Docs(fx));
        var ret = await returns.CreateReturnAsync(new CreateReturnRequest
        {
            OriginalBillId = bill.Id,
            Items = [new CreateReturnItemRequest { OriginalBillItemId = bill.Items[0].Id, Quantity = 1 }]
        });
        ret.ReturnAmount.Should().Be(5150);
        (await fx.Db.Bills.FindAsync(bill.Id))!.Status.Should().NotBe(BillStatus.Cancelled);
        (await fx.Db.Inventories.FirstAsync(i => i.ProductId == productId)).Quantity.Should().Be(10);
    }

    [Fact]
    public async Task Combined_sale_and_return_adjusts_payable_and_restores_stock()
    {
        await using var fx = new SqliteFixture();
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
            Payments = [],
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
        combined.GrandTotal.Should().Be(5150);
        combined.ReturnAdjustment.Should().Be(5150);
        combined.PayableAmount.Should().Be(0);
        combined.DueAmount.Should().Be(0);
        combined.Adjustments.Should().ContainSingle(a => a.ReturnKind == ReturnKind.Return);
        (await fx.Db.Inventories.AsNoTracking().FirstAsync(i => i.ProductId == productId)).Quantity.Should().Be(9);
    }

    [Fact]
    public async Task Combined_return_greater_than_sale_generates_wallet_credit()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;
        var opening = (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId)).WalletBalance;
        var billing = Billing(fx);
        var original = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 2 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 10300 }]
        });
        var combined = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [],
            Adjustments =
            [
                new SaleAdjustmentRequest
                {
                    Kind = ReturnKind.Return,
                    OriginalBillId = original.Id,
                    Items = [new CreateReturnItemRequest { OriginalBillItemId = original.Items[0].Id, Quantity = 2 }]
                }
            ]
        });
        combined.GrandTotal.Should().Be(5150);
        combined.ReturnAdjustment.Should().Be(10300);
        combined.CreditGenerated.Should().Be(5150);
        combined.PayableAmount.Should().Be(0);
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId)).WalletBalance.Should().Be(opening + 5150);
    }

    [Fact]
    public async Task Combined_return_rejects_already_returned_item()
    {
        await using var fx = new SqliteFixture();
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
        await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [],
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
        var act = async () => await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }],
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
        await act.Should().ThrowAsync<BusinessAppException>();
    }

    [Fact]
    public async Task Wallet_redemption_cannot_go_negative()
    {
        await using var fx = new SqliteFixture();
        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var customerId = fx.Db.Customers.First().Id;
        var act = async () => await customers.RedeemWalletAsync(customerId, new WalletRedeemRequest { StoreId = 1, Amount = 600 });
        var ex = await act.Should().ThrowAsync<ValidationAppException>();
        ex.Which.Message.Should().Contain("Available customer credit is ₹500.00");
        await customers.RedeemWalletAsync(customerId, new WalletRedeemRequest { StoreId = 1, Amount = 400 });
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId)).WalletBalance.Should().Be(100);
    }
}
