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
        invoice.CustomerReferralCode.Should().Be(referred.ReferralCode);
        invoice.CustomerReferralCode.Should().NotBe(referrer.ReferralCode);
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
        fx.Db.CustomerLedgers.Should().ContainSingle(r => r.BillId == first.Id && r.DiscountGiven == 500 && r.NewCustomerPercent == 10);
    }

    [Fact]
    public async Task Referral_accepts_customer_code_as_well_as_referral_code()
    {
        await using var fx = new SqliteFixture();
        var referrer = fx.Db.Customers.First();
        var referrals = new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var byCustomerCode = await referrals.ValidateCodeAsync(referrer.CustomerCode, null, 1);
        byCustomerCode.Valid.Should().BeTrue();
        byCustomerCode.ReferrerCustomerId.Should().Be(referrer.Id);

        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var referred = await customers.CreateAsync(new Application.DTOs.Customers.CreateCustomerRequest
        {
            StoreId = 1,
            Name = "Code Referred",
            MobileNumber = "9111111114",
            ReferralCode = referrer.CustomerCode
        });
        referred.ReferredByCustomerId.Should().Be(referrer.Id);

        var billing = CreateBilling(fx);
        var bill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = referred.Id,
            ReferralCode = referrer.CustomerCode,
            Items = [new CreateBillItemRequest { ProductId = fx.Db.Products.First().Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5098.50m }]
        });
        bill.ReferralDiscount.Should().Be(50);
        var invoice = await billing.GetInvoiceAsync(bill.Id);
        invoice.CustomerReferralCode.Should().NotBeNullOrWhiteSpace();
        invoice.CustomerCode.Should().Be(referred.CustomerCode);
    }

    [Fact]
    public async Task Ledger_summary_shows_remaining_credit_when_credit_exceeds_debit()
    {
        await using var fx = new SqliteFixture();
        var customer = fx.Db.Customers.First();
        fx.Db.CustomerLedgers.AddRange(
            new GramShopPOS.Domain.Entities.CustomerLedger
            {
                CustomerId = customer.Id,
                StoreId = 1,
                Debit = 1000,
                Credit = 0,
                Balance = 1000,
                TransactionType = LedgerTransactionType.Sale,
                Description = "Sale",
                TransactionDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            },
            new GramShopPOS.Domain.Entities.CustomerLedger
            {
                CustomerId = customer.Id,
                StoreId = 1,
                Debit = 0,
                Credit = 1500,
                Balance = -500,
                TransactionType = LedgerTransactionType.PaymentReceived,
                Description = "Payment",
                TransactionDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
        customer.OutstandingBalance = -500;
        fx.Db.SaveChanges();

        var customers = new CustomerService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var summary = await customers.GetLedgerSummaryAsync(customer.Id);
        summary.TotalDebit.Should().Be(1000);
        summary.TotalCredit.Should().Be(1500);
        summary.CurrentBalance.Should().Be(-500);
        summary.OverdueAmount.Should().Be(0);
        summary.AdvanceCredit.Should().Be(500);

        var dto = await customers.GetByIdAsync(customer.Id);
        dto.TotalDebit.Should().Be(1000);
        dto.TotalCredit.Should().Be(1500);
        dto.OverdueAmount.Should().Be(0);
        dto.AdvanceCredit.Should().Be(500);
        dto.ReferralCode.Should().NotBeNullOrWhiteSpace();
    }

    private static BillingService CreateBilling(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new AuditService(fx.Db, fx.User), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User)), new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))), new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));
}
