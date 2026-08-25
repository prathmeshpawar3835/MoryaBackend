using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GramShopPOS.Infrastructure.Configurations;

public sealed class StoreDiscountConfiguration : IEntityTypeConfiguration<StoreDiscount>
{
    public void Configure(EntityTypeBuilder<StoreDiscount> builder)
    {
        builder.ToTable("StoreDiscounts");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.HasIndex(x => x.StoreId);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.ContactPerson).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.GSTNumber).HasMaxLength(20);
        builder.HasIndex(x => x.StoreId);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class RepairJobConfiguration : IEntityTypeConfiguration<RepairJob>
{
    public void Configure(EntityTypeBuilder<RepairJob> builder)
    {
        builder.ToTable("RepairJobs");
        builder.Property(x => x.JobNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.JobNumber).IsUnique();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.MobileNumber);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50);
        builder.HasIndex(x => x.InvoiceNumber);
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProductDetails).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.ReceivedDate);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BillItem).WithMany().HasForeignKey(x => x.BillItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class RepairJobHistoryConfiguration : IEntityTypeConfiguration<RepairJobHistory>
{
    public void Configure(EntityTypeBuilder<RepairJobHistory> builder)
    {
        builder.ToTable("RepairJobHistories");
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne(x => x.RepairJob).WithMany(x => x.History).HasForeignKey(x => x.RepairJobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
