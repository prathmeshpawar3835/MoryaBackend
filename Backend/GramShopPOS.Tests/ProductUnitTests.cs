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
    }

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
}
