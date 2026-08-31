using FluentAssertions;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Enums;
using GramShopPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Tests;

public class ProductUnitTests
{
    [Fact]
    public async Task Opening_stock_creates_unique_numbers_per_piece()
    {
        await using var fx = new SqliteFixture();
        var categories = new CategoryService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var rings = await categories.CreateAsync(new CreateCategoryRequest { Name = "Ring", CodePrefix = "RNG" });
        var products = new ProductService(fx.Db, fx.User, new AuditService(fx.Db, fx.User), new StockEngine(fx.Db), new ExcelWorkbookService());
        await products.CreateAsync(new CreateProductRequest
        {
            ProductCode = "GOLD-RING",
            ProductName = "Gold Ring",
            CategoryId = rings.Id,
            PurchasePrice = 20000,
            SellingPrice = 22500,
            MRP = 25000,
            TaxPercent = 3,
            OpeningStockStoreId = 1,
            OpeningStock = 10
        });

        var numbers = await fx.Db.ProductUnits.OrderBy(u => u.UniqueNumber).Select(u => u.UniqueNumber).ToListAsync();
        numbers.Should().HaveCount(10);
        numbers[0].Should().Be("RNG-000001");
        numbers[9].Should().Be("RNG-000010");
        numbers.Should().OnlyHaveUniqueItems();
        var prices = await fx.Db.ProductUnits.Select(u => new { u.SellingPrice, u.MRP, u.PurchasePrice }).ToListAsync();
        prices.Should().OnlyContain(p => p.SellingPrice == 22500 && p.MRP == 25000 && p.PurchasePrice == 20000);
    }

    [Fact]
    public async Task Pieces_of_the_same_product_can_have_different_selling_prices()
    {
        await using var fx = new SqliteFixture();
        var categories = new CategoryService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var rings = await categories.CreateAsync(new CreateCategoryRequest { Name = "Ring", CodePrefix = "RNG" });
        var products = new ProductService(fx.Db, fx.User, new AuditService(fx.Db, fx.User), new StockEngine(fx.Db), new ExcelWorkbookService());
        var product = await products.CreateAsync(new CreateProductRequest
        {
            ProductCode = "GOLD-RING",
            ProductName = "Gold Ring",
            CategoryId = rings.Id,
            PurchasePrice = 20000,
            SellingPrice = 22500,
            MRP = 25000,
            TaxPercent = 3,
            OpeningStockStoreId = 1,
            OpeningStock = 10
        });

        var units = new ProductUnitService(fx.Db, fx.User);
        var first = await fx.Db.ProductUnits.OrderBy(u => u.UniqueNumber).FirstAsync();
        var second = await fx.Db.ProductUnits.OrderBy(u => u.UniqueNumber).Skip(1).FirstAsync();
        await units.UpdatePricesAsync(first.Id, new UpdateProductUnitRequest { SellingPrice = 18000, MRP = 20000, PurchasePrice = 16000 });
        await units.UpdatePricesAsync(second.Id, new UpdateProductUnitRequest { SellingPrice = 24000, MRP = 27000, PurchasePrice = 21000 });

        var scannedCheap = await products.GetByBarcodeAsync("RNG-000001", 1);
        scannedCheap.SellingPrice.Should().Be(18000);
        scannedCheap.MRP.Should().Be(20000);
        scannedCheap.ProductUnitId.Should().Be(first.Id);

        var scannedDear = await products.GetByBarcodeAsync("RNG-000002", 1);
        scannedDear.SellingPrice.Should().Be(24000);
        scannedDear.MRP.Should().Be(27000);

        var listed = await units.GetAsync(new ProductUnitListRequest { ProductId = product.Id, PageSize = 20 });
        listed.Items.Should().Contain(u => u.UniqueNumber == "RNG-000001" && u.SellingPrice == 18000);
        listed.Items.Should().Contain(u => u.UniqueNumber == "RNG-000002" && u.SellingPrice == 24000);
        listed.Items.Should().Contain(u => u.UniqueNumber == "RNG-000003" && u.SellingPrice == 22500);

        var labels = await units.GetLabelDataAsync(new ProductUnitIdsRequest { Ids = [first.Id, second.Id] });
        labels.Should().Contain(l => l.UniqueNumber == "RNG-000001" && l.SellingPrice == 18000);
        labels.Should().Contain(l => l.UniqueNumber == "RNG-000002" && l.SellingPrice == 24000);

        var billing = Billing(fx);
        var customerId = fx.Db.Customers.First().Id;
        var bill = await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = product.Id, Quantity = 1, ProductUnitIds = [first.Id] }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 18540 }]
        });
        bill.Items.Should().ContainSingle();
        bill.Items[0].Rate.Should().Be(18000);

        var mixedLine = async () => await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = product.Id, Quantity = 2, ProductUnitIds = [second.Id, fx.Db.ProductUnits.OrderBy(u => u.UniqueNumber).Skip(2).First().Id] }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 1 }]
        });
        await mixedLine.Should().ThrowAsync<ValidationAppException>().WithMessage("*different selling prices*");

        var unscoped = async () => await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = product.Id, Quantity = 1 }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 1 }]
        });
        await unscoped.Should().ThrowAsync<ValidationAppException>().WithMessage("*Scan each unique number*");
    }

    private static BillingService Billing(SqliteFixture fx) =>
        new(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db),
            new AuditService(fx.Db, fx.User), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User)),
            new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db),
                new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))),
            new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));

    [Fact]
    public async Task Lookup_returns_product_and_rejects_sold_piece()
    {
        await using var fx = new SqliteFixture();
        var categories = new CategoryService(fx.Db, fx.User, new AuditService(fx.Db, fx.User));
        var cat = await categories.CreateAsync(new CreateCategoryRequest { Name = "Ring", CodePrefix = "RNG" });
        var products = new ProductService(fx.Db, fx.User, new AuditService(fx.Db, fx.User), new StockEngine(fx.Db), new ExcelWorkbookService());
        var product = await products.CreateAsync(new CreateProductRequest
        {
            ProductCode = "RING-SCAN",
            ProductName = "Scan Ring",
            CategoryId = cat.Id,
            PurchasePrice = 100,
            SellingPrice = 150,
            MRP = 160,
            TaxPercent = 3,
            OpeningStockStoreId = 1,
            OpeningStock = 2
        });

        var scanned = await products.GetByBarcodeAsync("RNG-000001", 1);
        scanned.UniqueNumber.Should().Be("RNG-000001");
        scanned.ProductUnitId.Should().NotBeNull();
        scanned.ProductName.Should().Be("Scan Ring");

        var billing = new BillingService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db),
            new AuditService(fx.Db, fx.User), new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User)),
            new ReturnDocumentService(fx.Db, fx.User, new StockEngine(fx.Db), new DocumentNumberGenerator(fx.Db),
                new ReferralService(fx.Db, fx.User, new AuditService(fx.Db, fx.User))),
            new BirthdayService(fx.Db, fx.User, new DisabledWhatsAppService(), new AuditService(fx.Db, fx.User)));
        var customerId = fx.Db.Customers.First().Id;
        await billing.CreateBillAsync(new CreateBillRequest
        {
            StoreId = 1,
            CustomerId = customerId,
            Items = [new CreateBillItemRequest { ProductId = product.Id, Quantity = 1, ProductUnitIds = [scanned.ProductUnitId!.Value] }],
            Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 154.5m }]
        });

        var sold = async () => await products.GetByBarcodeAsync("RNG-000001", 1);
        await sold.Should().ThrowAsync<BusinessAppException>().WithMessage("*sold*");
    }

    [Fact]
    public void Qr_png_is_a_valid_png_image()
    {
        var labels = new LabelDocumentService();
        var bytes = labels.QrPng("RNG-000001");
        bytes.Length.Should().BeGreaterThan(100);
        bytes[0].Should().Be(0x89);
        bytes[1].Should().Be((byte)'P');
        bytes[2].Should().Be((byte)'N');
        bytes[3].Should().Be((byte)'G');
    }
}
