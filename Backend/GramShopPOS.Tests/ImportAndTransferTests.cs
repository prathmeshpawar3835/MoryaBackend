using ClosedXML.Excel;
using FluentAssertions;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Enums;
using GramShopPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Tests;

public class ImportAndTransferTests
{
    [Fact]
    public async Task Import_preview_returns_row_errors_and_does_not_confirm_invalid_data()
    {
        await using var fx = new SqliteFixture();
        var excel = new ExcelWorkbookService();
        var products = new ProductService(fx.Db, fx.User, new AuditService(fx.Db, fx.User), new StockEngine(fx.Db), excel);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Products");
        var headers = new[] { "Product Code", "Product Name", "Category", "Unit", "Purchase Price", "Selling Price", "MRP", "Tax %", "Opening Stock", "Store Code", "Barcode" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Cell(2, 1).Value = "BAD";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var preview = await products.PreviewImportAsync(stream, "products.xlsx");
        preview.ErrorRowCount.Should().BeGreaterThan(0);
        var confirm = async () => await products.ConfirmImportAsync(preview.BatchId);
        await confirm.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void Import_template_is_real_xlsx()
    {
        var excel = new ExcelWorkbookService();
        var file = excel.CreateProductImportTemplate();
        file.Content.Length.Should().BeGreaterThan(100);
        file.FileName.Should().EndWith(".xlsx");
        using var stream = new MemoryStream(file.Content);
        using var workbook = new XLWorkbook(stream);
        workbook.Worksheets.First().Cell(1, 1).GetString().Should().Be("Product Code");
    }

    [Fact]
    public async Task Stock_transfer_moves_quantity_between_stores()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var inventory = new InventoryService(fx.Db, fx.User, new StockEngine(fx.Db), new AuditService(fx.Db, fx.User));
        await inventory.TransferAsync(new Application.DTOs.Inventory.StockTransferRequest
        {
            FromStoreId = 1,
            ToStoreId = 2,
            Reason = "Rebalance",
            Items = [new Application.DTOs.Inventory.StockTransferItemRequest { ProductId = productId, Quantity = 3 }]
        });
        (await fx.Db.Inventories.AsNoTracking().FirstAsync(i => i.StoreId == 1 && i.ProductId == productId)).Quantity.Should().Be(7);
        (await fx.Db.Inventories.AsNoTracking().FirstAsync(i => i.StoreId == 2 && i.ProductId == productId)).Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Referral_applies_new_customer_discount_and_referrer_ledger_credit()
    {
        await using var fx = new SqliteFixture();
        var referrer = fx.Db.Customers.First();
        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var referred = await customers.CreateAsync(new Application.DTOs.Customers.CreateCustomerRequest
        {
            StoreId = 1,
            Name = "New Customer",
            MobileNumber = "9111111111",
            ReferralCode = referrer.ReferralCode
        });
        var billing = new BillingService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User)), new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))), new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));
        var bill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = referred.Id,
            ReferralCode = referrer.ReferralCode,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5098.50m }]
        });
        bill.ReferralDiscount.Should().Be(50);
        bill.GrandTotal.Should().Be(5098.50m);
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == referrer.Id)).WalletBalance.Should().Be(600);
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == referred.Id)).WalletBalance.Should().Be(0);
        fx.Db.Referrals.Should().ContainSingle();
        fx.Db.CustomerLedgers.Should().Contain(l => l.CustomerId == referrer.Id && l.Credit == 100 && l.TransactionType == LedgerTransactionType.ReferralCredit);
    }

    [Fact]
    public async Task Referral_does_not_apply_on_sale_without_code_even_if_customer_was_linked()
    {
        await using var fx = new SqliteFixture();
        var referrer = fx.Db.Customers.First();
        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var referred = await customers.CreateAsync(new Application.DTOs.Customers.CreateCustomerRequest
        {
            StoreId = 1,
            Name = "Linked Customer",
            MobileNumber = "9111111112",
            ReferralCode = referrer.ReferralCode
        });
        var billing = CreateBilling(fx);
        var bill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = referred.Id,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150m }]
        });
        bill.ReferralDiscount.Should().Be(0);
        bill.GrandTotal.Should().Be(5150m);
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == referrer.Id)).WalletBalance.Should().Be(500);
        fx.Db.Referrals.Should().BeEmpty();
    }

    [Fact]
    public async Task Referral_discount_applies_only_on_first_invoice_and_snapshots_configured_percent()
    {
        await using var fx = new SqliteFixture();
        var settings = fx.Db.BusinessSettings.First();
        settings.RewardType = RewardType.Percentage;
        settings.NewCustomerReward = 10;
        settings.ReferrerReward = 5;
        fx.Db.SaveChanges();

        var referrer = fx.Db.Customers.First();
        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var referred = await customers.CreateAsync(new Application.DTOs.Customers.CreateCustomerRequest
        {
            StoreId = 1,
            Name = "Percent Customer",
            MobileNumber = "9111111113",
            ReferralCode = referrer.ReferralCode
        });
        var billing = CreateBilling(fx);
        var first = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = referred.Id,
            ReferralCode = referrer.ReferralCode,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 4635m }]
        });
        first.ReferralDiscount.Should().Be(500);
        first.ReferralDiscountPercent.Should().Be(10);
        first.ReferrerCode.Should().Be(referrer.ReferralCode);
        first.GrandTotal.Should().Be(4635m);

        settings.NewCustomerReward = 8;
        settings.ReferrerReward = 3;
        fx.Db.SaveChanges();

        var invoice = await billing.GetInvoiceAsync(first.Id);
        invoice.ReferralDiscount.Should().Be(500);
        invoice.ReferralDiscountPercent.Should().Be(10);
        invoice.HasReferral.Should().BeTrue();
        invoice.ReferrerCode.Should().Be(referrer.ReferralCode);
        invoice.CustomerName.Should().Be("Percent Customer");
        invoice.CustomerMobile.Should().Be("9111111113");
        invoice.CustomerCode.Should().NotBeNullOrWhiteSpace();
        invoice.DiscountLines.Should().Contain(l => l.Type == "Referral" && l.Amount == 500 && l.Percent == 10);
        invoice.TotalDiscount.Should().Be(500);

        var second = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = referred.Id,
            ReferralCode = referrer.ReferralCode,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150m }]
        });
        second.ReferralDiscount.Should().Be(0);
        second.GrandTotal.Should().Be(5150m);
        (await fx.Db.Customers.AsNoTracking().FirstAsync(c => c.Id == referrer.Id)).WalletBalance.Should().Be(750);
        fx.Db.Referrals.Should().ContainSingle(r => r.BillId == first.Id && r.DiscountGiven == 500 && r.NewCustomerPercent == 10);
    }

    private static BillingService CreateBilling(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User)), new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))), new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));
}
