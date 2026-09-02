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
                new Role { Name = Roles.Admin, Description = "Full access", IsActive = true, CreatedDate = DateTime.Now },
                new Role { Name = Roles.SalesPerson, Description = "Store sales access", IsActive = true, CreatedDate = DateTime.Now });
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
                CreatedDate = DateTime.Now
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
                CreatedDate = DateTime.Now
            };
            var sales = new ApplicationUser
            {
                UserName = "salesperson",
                FullName = "Default Sales Person",
                PasswordHash = _passwords.Hash("ChangeMe@123"),
                MustChangePassword = true,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            _db.Users.AddRange(admin, sales);
            await _db.SaveChangesAsync(cancellationToken);
            _db.UserRoles.AddRange(
                new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
                new UserRole { UserId = sales.Id, RoleId = salesRole.Id });
            _db.StoreUsers.Add(new StoreUser { UserId = sales.Id, StoreId = store.Id, IsPrimary = true, CreatedDate = DateTime.Now });
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default admin and salesperson users.");
        }

        if (!await _db.Categories.AnyAsync(cancellationToken))
        {
            _db.Categories.AddRange(
                new Category { Name = "Chains", CodePrefix = "CHN", Description = "Gold chains", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Name = "Rings", CodePrefix = "RNG", Description = "Gold rings", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Name = "Earrings", CodePrefix = "ERG", Description = "Gold earrings", IsActive = true, CreatedDate = DateTime.Now });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Products.AnyAsync(cancellationToken))
        {
            var chains = await _db.Categories.FirstAsync(c => c.Name == "Chains", cancellationToken);
            var rings = await _db.Categories.FirstAsync(c => c.Name == "Rings", cancellationToken);
            var store = await _db.Stores.FirstAsync(cancellationToken);
            var products = new[]
            {
                new Product { ProductCode = "1G-CHAIN-001", Barcode = "890000000001", ProductName = "1 Gram Gold Chain", CategoryId = chains.Id, Unit = "PCS", PurchasePrice = 4500, SellingPrice = 5200, MRP = 5500, TaxPercent = 3, MinimumStockLevel = 2, IsActive = true, CreatedDate = DateTime.Now },
                new Product { ProductCode = "1G-RING-001", Barcode = "890000000002", ProductName = "1 Gram Gold Ring", CategoryId = rings.Id, Unit = "PCS", PurchasePrice = 4300, SellingPrice = 5000, MRP = 5300, TaxPercent = 3, MinimumStockLevel = 2, IsActive = true, CreatedDate = DateTime.Now }
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
                    CreatedDate = DateTime.Now
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
                    CreatedDate = DateTime.Now,
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
                CreatedDate = DateTime.Now
            });
            _db.TaxSettings.AddRange(
                new TaxSetting { Name = "GST 3%", Percent = 3, IsDefault = true, IsActive = true, CreatedDate = DateTime.Now },
                new TaxSetting { Name = "GST 5%", Percent = 5, IsDefault = false, IsActive = true, CreatedDate = DateTime.Now });
            await _db.SaveChangesAsync(cancellationToken);
        }

        var birthdayPercent = (await _db.BusinessSettings.FirstAsync(cancellationToken)).BirthdayDiscountPercent;
        if (birthdayPercent <= 0)
        {
            birthdayPercent = 10;
        }

        var stores = await _db.Stores.Where(s => s.IsActive && !s.IsDeleted).ToListAsync(cancellationToken);
        foreach (var store in stores)
        {
            var exists = await _db.StoreDiscounts.AnyAsync(
                d => d.StoreId == store.Id && d.OfferCategory == OfferCategory.Birthday && !d.IsDeleted,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            _db.StoreDiscounts.Add(new StoreDiscount
            {
                StoreId = store.Id,
                Name = "Birthday Special Offer",
                Description = "Valid only on your birthday",
                OfferCategory = OfferCategory.Birthday,
                DiscountKind = DiscountKind.Percentage,
                Value = birthdayPercent,
                IsActive = true,
                CreatedDate = DateTime.Now
            });
        }

        await EnsureCategoryPrefixesAndUnitsAsync(cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCategoryPrefixesAndUnitsAsync(CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.Where(c => !c.IsDeleted).ToListAsync(cancellationToken);
        foreach (var category in categories.Where(c => string.IsNullOrWhiteSpace(c.CodePrefix)))
        {
            var suggested = CategoryPrefixes.Suggest(category.Name);
            var prefix = suggested;
            var n = 2;
            while (categories.Any(c => c.Id != category.Id && c.CodePrefix == prefix))
            {
                prefix = $"{suggested}{n}";
                n++;
            }

            category.CodePrefix = prefix;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var inventories = await _db.Inventories.AsNoTracking()
            .Include(i => i.Product).ThenInclude(p => p.Category)
            .Where(i => !i.IsDeleted && i.Quantity > 0)
            .ToListAsync(cancellationToken);

        foreach (var group in inventories.GroupBy(i => new { i.ProductId, i.StoreId }))
        {
            var sample = group.First();
            if (!PieceCountCompatible(sample.Product.Unit, sample.Quantity, out var needed) || needed == 0)
            {
                continue;
            }

            var existing = await _db.ProductUnits.CountAsync(
                u => u.ProductId == sample.ProductId && u.StoreId == sample.StoreId && !u.IsDeleted && u.Status != ProductUnitStatus.Removed,
                cancellationToken);
            var missing = needed - existing;
            if (missing <= 0)
            {
                continue;
            }

            var prefix = string.IsNullOrWhiteSpace(sample.Product.Category.CodePrefix)
                ? CategoryPrefixes.Suggest(sample.Product.Category.Name)
                : sample.Product.Category.CodePrefix!;
            var seq = await _db.ProductUnitSequences.FirstOrDefaultAsync(s => s.Prefix == prefix, cancellationToken);
            if (seq is null)
            {
                seq = new ProductUnitSequence { Prefix = prefix, LastNumber = 0, CreatedDate = DateTime.Now, IsActive = true };
                _db.ProductUnitSequences.Add(seq);
                await _db.SaveChangesAsync(cancellationToken);
            }

            var start = seq.LastNumber + 1;
            seq.LastNumber += missing;
            for (var i = 0; i < missing; i++)
            {
                _db.ProductUnits.Add(new ProductUnit
                {
                    ProductId = sample.ProductId,
                    StoreId = sample.StoreId,
                    UniqueNumber = $"{prefix}-{(start + i):000000}",
                    Status = ProductUnitStatus.Available,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool PieceCountCompatible(string? unit, decimal quantity, out int count)
    {
        count = 0;
        var u = (unit ?? "PCS").Trim().ToUpperInvariant();
        if (u is not ("PCS" or "PC" or "PIECE" or "PIECES" or "NOS" or "NO"))
        {
            return false;
        }

        if (quantity <= 0 || quantity != Math.Truncate(quantity) || quantity > 100_000)
        {
            return false;
        }

        count = (int)quantity;
        return true;
    }
}
