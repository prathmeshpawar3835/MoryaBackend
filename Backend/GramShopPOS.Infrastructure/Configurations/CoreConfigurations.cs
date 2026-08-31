using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GramShopPOS.Infrastructure.Configurations;

internal static class DecimalExtensions
{
    public static PropertyBuilder<decimal> Money(this PropertyBuilder<decimal> builder) =>
        builder.HasPrecision(18, 2);

    public static PropertyBuilder<decimal?> Money(this PropertyBuilder<decimal?> builder) =>
        builder.HasPrecision(18, 2);
}

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.UserName).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");
        builder.Property(x => x.StoreCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.StoreCode).IsUnique();
        builder.Property(x => x.StoreName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.ContactNumber).HasMaxLength(20);
        builder.Property(x => x.GSTNumber).HasMaxLength(20);
        builder.Property(x => x.InvoicePrefix).HasMaxLength(20);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class StoreUserConfiguration : IEntityTypeConfiguration<StoreUser>
{
    public void Configure(EntityTypeBuilder<StoreUser> builder)
    {
        builder.ToTable("StoreUsers");
        builder.HasKey(x => new { x.StoreId, x.UserId });
        builder.HasOne(x => x.Store).WithMany(x => x.StoreUsers).HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany(x => x.StoreUsers).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.CodePrefix).HasMaxLength(8);
        builder.HasIndex(x => x.CodePrefix).IsUnique().HasFilter("[CodePrefix] IS NOT NULL AND [CodePrefix] <> ''");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ProductCode).IsUnique();
        builder.Property(x => x.Barcode).HasMaxLength(50);
        builder.HasIndex(x => x.Barcode).IsUnique().HasFilter("Barcode IS NOT NULL");
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.ProductName);
        builder.HasIndex(x => x.CategoryId);
        builder.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PurchasePrice).Money();
        builder.Property(x => x.SellingPrice).Money();
        builder.Property(x => x.MRP).Money();
        builder.Property(x => x.TaxPercent).HasPrecision(5, 2);
        builder.Property(x => x.MinimumStockLevel).HasPrecision(18, 3);
        builder.Property(x => x.ImagePath).HasMaxLength(500);
        builder.Property(x => x.WeightGrams).HasPrecision(18, 3);
        builder.Property(x => x.Metal).HasMaxLength(50);
        builder.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");
        builder.HasIndex(x => new { x.StoreId, x.ProductId }).IsUnique();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany(x => x.Inventories).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.CreatedDate);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.PreviousQuantity).HasPrecision(18, 3);
        builder.Property(x => x.NewQuantity).HasPrecision(18, 3);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(50);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable("ProductUnits");
        builder.Property(x => x.UniqueNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PurchasePrice).Money();
        builder.Property(x => x.SellingPrice).Money();
        builder.Property(x => x.MRP).Money();
        builder.HasIndex(x => x.UniqueNumber).IsUnique();
        builder.HasIndex(x => new { x.ProductId, x.StoreId, x.Status });
        builder.HasOne(x => x.Product).WithMany(x => x.Units).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BillItem).WithMany().HasForeignKey(x => x.BillItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class ProductUnitSequenceConfiguration : IEntityTypeConfiguration<ProductUnitSequence>
{
    public void Configure(EntityTypeBuilder<ProductUnitSequence> builder)
    {
        builder.ToTable("ProductUnitSequences");
        builder.Property(x => x.Prefix).HasMaxLength(8).IsRequired();
        builder.HasIndex(x => x.Prefix).IsUnique();
    }
}
