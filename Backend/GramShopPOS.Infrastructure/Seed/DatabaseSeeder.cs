using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using GramShopPOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GramShopPOS.Infrastructure.Seed;

public sealed class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext db, IPasswordService passwords, ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _passwords = passwords;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.MigrateAsync(cancellationToken);

        if (!await _db.Roles.AnyAsync(cancellationToken))
        {
            _db.Roles.AddRange(
                new Role { Name = Roles.Admin, Description = "Full access", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Role { Name = Roles.SalesPerson, Description = "Store sales access", IsActive = true, CreatedDate = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Stores.AnyAsync(cancellationToken))
        {
            _db.Stores.Add(new Store
            {
                StoreCode = "STORE01",
                StoreName = "Gram Shop Main",
                Address = "MG Road, Sample City",
                ContactNumber = "9999999999",
                GSTNumber = "22AAAAA0000A1Z5",
                InvoicePrefix = "STORE01",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Users.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var adminRole = await _db.Roles.FirstAsync(r => r.Name == Roles.Admin, cancellationToken);
            var salesRole = await _db.Roles.FirstAsync(r => r.Name == Roles.SalesPerson, cancellationToken);
            var store = await _db.Stores.FirstAsync(cancellationToken);
            var admin = new ApplicationUser
            {
                UserName = "admin",
                FullName = "System Administrator",
                PasswordHash = _passwords.Hash("ChangeMe@123"),
                MustChangePassword = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var sales = new ApplicationUser
            {
                UserName = "salesperson",
                FullName = "Default Sales Person",
                PasswordHash = _passwords.Hash("ChangeMe@123"),
                MustChangePassword = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _db.Users.AddRange(admin, sales);
            await _db.SaveChangesAsync(cancellationToken);
            _db.UserRoles.AddRange(
                new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
                new UserRole { UserId = sales.Id, RoleId = salesRole.Id });
            _db.StoreUsers.Add(new StoreUser { UserId = sales.Id, StoreId = store.Id, IsPrimary = true, CreatedDate = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default admin and salesperson users.");
        }

        if (!await _db.Categories.AnyAsync(cancellationToken))
        {
            _db.Categories.AddRange(
                new Category { Name = "Chains", Description = "Gold chains", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Category { Name = "Rings", Description = "Gold rings", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Category { Name = "Earrings", Description = "Gold earrings", IsActive = true, CreatedDate = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Products.AnyAsync(cancellationToken))
        {
            var chains = await _db.Categories.FirstAsync(c => c.Name == "Chains", cancellationToken);
            var rings = await _db.Categories.FirstAsync(c => c.Name == "Rings", cancellationToken);
            var store = await _db.Stores.FirstAsync(cancellationToken);
            var products = new[]
            {
                new Product { ProductCode = "1G-CHAIN-001", Barcode = "890000000001", ProductName = "1 Gram Gold Chain", CategoryId = chains.Id, Unit = "PCS", PurchasePrice = 4500, SellingPrice = 5200, MRP = 5500, TaxPercent = 3, MinimumStockLevel = 2, IsActive = true, CreatedDate = DateTime.UtcNow },
                new Product { ProductCode = "1G-RING-001", Barcode = "890000000002", ProductName = "1 Gram Gold Ring", CategoryId = rings.Id, Unit = "PCS", PurchasePrice = 4300, SellingPrice = 5000, MRP = 5300, TaxPercent = 3, MinimumStockLevel = 2, IsActive = true, CreatedDate = DateTime.UtcNow }
            };
            _db.Products.AddRange(products);
            await _db.SaveChangesAsync(cancellationToken);
            foreach (var product in products)
            {
                _db.Inventories.Add(new Inventory
                {
                    StoreId = store.Id,
                    ProductId = product.Id,
                    Quantity = 10,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });
                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    StoreId = store.Id,
                    Quantity = 10,
                    PreviousQuantity = 0,
                    NewQuantity = 10,
                    MovementType = StockMovementType.OpeningStock,
                    Reason = "Seed opening stock",
                    UserId = (await _db.Users.FirstAsync(u => u.UserName == "admin", cancellationToken)).Id,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.BusinessSettings.AnyAsync(cancellationToken))
        {
            _db.BusinessSettings.Add(new BusinessSetting
            {
                ShopName = "1 Gram Jewellery Shop",
                Address = "MG Road, Sample City",
                Mobile = "9999999999",
                Email = "shop@example.com",
                GSTNumber = "22AAAAA0000A1Z5",
                InvoiceFooter = "Thank you for shopping with us.",
                ReturnPolicy = "Returns accepted within 7 days with original invoice.",
                InvoicePrefix = "INV",
                InvoiceNumberFormat = "{PREFIX}-FY{FY}-{SEQ:000000}",
                FinancialYearStartMonth = 4,
                AllowNegativeStock = false,
                DefaultTaxPercent = 3,
                LowStockDefaultLevel = 2,
                ReferralEnabled = true,
                NewCustomerReward = 10,
                ReferrerReward = 5,
                RewardType = RewardType.Percentage,
                RewardTrigger = RewardTrigger.FirstPurchase,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });
            _db.TaxSettings.AddRange(
                new TaxSetting { Name = "GST 3%", Percent = 3, IsDefault = true, IsActive = true, CreatedDate = DateTime.UtcNow },
                new TaxSetting { Name = "GST 5%", Percent = 5, IsDefault = false, IsActive = true, CreatedDate = DateTime.UtcNow });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
