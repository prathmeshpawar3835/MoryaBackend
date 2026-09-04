using GramShopPOS.Application.Interfaces;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using GramShopPOS.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Tests;

public sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;
    public int UserId { get; set; } = 1;
    public string UserName { get; set; } = "admin";
    public string Role { get; set; } = Roles.Admin;
    public bool IsAdmin => Role == Roles.Admin;
    public IReadOnlyList<int> AssignedStoreIds { get; set; } = [1];
    public string? IpAddress { get; set; } = "127.0.0.1";
    public string? JwtId { get; set; } = "test-jti";
}

public sealed class SqliteFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Db { get; }
    public TestCurrentUser User { get; } = new();
    public PasswordService Passwords { get; } = new();

    public SqliteFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
        Seed();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        return new AppDbContext(options);
    }

    private void Seed()
    {
        Db.Roles.AddRange(
            new Role { Name = Roles.Admin, IsActive = true, CreatedDate = DateTime.UtcNow },
            new Role { Name = Roles.SalesPerson, IsActive = true, CreatedDate = DateTime.UtcNow });
        Db.Stores.AddRange(
            new Store { StoreCode = "STORE01", StoreName = "Main", InvoicePrefix = "STORE01", IsActive = true, CreatedDate = DateTime.UtcNow },
            new Store { StoreCode = "STORE02", StoreName = "Second", InvoicePrefix = "STORE02", IsActive = true, CreatedDate = DateTime.UtcNow });
        Db.SaveChanges();

        var admin = new ApplicationUser { UserName = "admin", FullName = "Admin", PasswordHash = Passwords.Hash("ChangeMe@123"), IsActive = true, CreatedDate = DateTime.UtcNow };
        var sales = new ApplicationUser { UserName = "salesperson", FullName = "Sales", PasswordHash = Passwords.Hash("ChangeMe@123"), IsActive = true, CreatedDate = DateTime.UtcNow };
        Db.Users.AddRange(admin, sales);
        Db.SaveChanges();
        Db.UserRoles.AddRange(
            new UserRole { UserId = admin.Id, RoleId = Db.Roles.First(r => r.Name == Roles.Admin).Id },
            new UserRole { UserId = sales.Id, RoleId = Db.Roles.First(r => r.Name == Roles.SalesPerson).Id });
        Db.StoreUsers.Add(new StoreUser { UserId = sales.Id, StoreId = 1, IsPrimary = true, CreatedDate = DateTime.UtcNow });
        Db.Categories.Add(new Category { Name = "Chains", IsActive = true, CreatedDate = DateTime.UtcNow });
        Db.SaveChanges();
        Db.Products.Add(new Product
        {
            ProductCode = "1G-CHAIN-001",
            Barcode = "8901",
            ProductName = "1 Gram Chain",
            CategoryId = Db.Categories.First().Id,
            Unit = "PCS",
            PurchasePrice = 4000,
            SellingPrice = 5000,
            MRP = 5500,
            TaxPercent = 3,
            MinimumStockLevel = 1,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        Db.BusinessSettings.Add(new BusinessSetting
        {
            ShopName = "Test Shop",
            InvoicePrefix = "INV",
            FinancialYearStartMonth = 4,
            AllowNegativeStock = false,
            ReferralEnabled = true,
            NewCustomerReward = 50,
            ReferrerReward = 100,
            RewardType = RewardType.FixedAmount,
            RewardTrigger = RewardTrigger.FirstPurchase,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        Db.SaveChanges();
        Db.Inventories.Add(new Inventory { StoreId = 1, ProductId = Db.Products.First().Id, Quantity = 10, IsActive = true, CreatedDate = DateTime.UtcNow });
        Db.Customers.Add(new Customer
        {
            StoreId = 1,
            Name = "Walk In",
            MobileNumber = "9000000000",
            ReferralCode = "RF100001",
            CustomerCode = "CUS000001",
            WalletBalance = 500,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        Db.SaveChanges();
        User.UserId = admin.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
