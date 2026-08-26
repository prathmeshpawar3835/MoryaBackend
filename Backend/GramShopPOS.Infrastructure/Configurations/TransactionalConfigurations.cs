using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GramShopPOS.Infrastructure.Configurations;

public sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("StockTransfers");
        builder.Property(x => x.TransferNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.TransferNumber).IsUnique();
        builder.HasOne(x => x.FromStore).WithMany().HasForeignKey(x => x.FromStoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToStore).WithMany().HasForeignKey(x => x.ToStoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockTransferItemConfiguration : IEntityTypeConfiguration<StockTransferItem>
{
    public void Configure(EntityTypeBuilder<StockTransferItem> builder)
    {
        builder.ToTable("StockTransferItems");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasOne(x => x.StockTransfer).WithMany(x => x.Items).HasForeignKey(x => x.StockTransferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.Property(x => x.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Total).Money();
        builder.HasIndex(x => x.StoreId);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany(x => x.Purchases).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems");
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.PurchasePrice).Money();
        builder.Property(x => x.Total).Money();
        builder.HasOne(x => x.Purchase).WithMany(x => x.Items).HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.MobileNumber).IsUnique();
        builder.Property(x => x.ReferralCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.ReferralCode).IsUnique();
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.DateOfBirth);
        builder.Property(x => x.OutstandingBalance).Money();
        builder.Property(x => x.WalletBalance).Money();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.StoreId);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReferredByCustomer).WithMany().HasForeignKey(x => x.ReferredByCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("Bills");
        builder.Property(x => x.BillNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BillNumber).IsUnique();
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => x.BillDate);
        builder.HasIndex(x => x.CustomerId);
        builder.Property(x => x.Subtotal).Money();
        builder.Property(x => x.ItemDiscountTotal).Money();
        builder.Property(x => x.BillDiscount).Money();
        builder.Property(x => x.TaxAmount).Money();
        builder.Property(x => x.GrandTotal).Money();
        builder.Property(x => x.PaidAmount).Money();
        builder.Property(x => x.DueAmount).Money();
        builder.Property(x => x.WalletRedeemed).Money();
        builder.Property(x => x.ReferralDiscount).Money();
        builder.Property(x => x.ReferralDiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.ReferrerName).HasMaxLength(200);
        builder.Property(x => x.ReferrerCode).HasMaxLength(20);
        builder.Property(x => x.ReferrerBenefitPercent).HasPrecision(5, 2);
        builder.Property(x => x.ReferrerBenefitAmount).Money();
        builder.Property(x => x.StoreDiscountAmount).Money();
        builder.Property(x => x.StoreDiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.StoreDiscountName).HasMaxLength(200);
        builder.Property(x => x.BirthdayDiscount).Money();
        builder.Property(x => x.BirthdayDiscountPercent).HasPrecision(5, 2);
        builder.Property(x => x.BirthdayOfferName).HasMaxLength(200);
        builder.Property(x => x.ReturnAdjustment).Money();
        builder.Property(x => x.ExchangeAdjustment).Money();
        builder.Property(x => x.BuybackAdjustment).Money();
        builder.Property(x => x.CreditGenerated).Money();
        builder.Property(x => x.PayableAmount).Money();
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany(x => x.Bills).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StoreDiscount).WithMany().HasForeignKey(x => x.StoreDiscountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BirthdayOffer).WithMany().HasForeignKey(x => x.BirthdayOfferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReferrerCustomer).WithMany().HasForeignKey(x => x.ReferrerCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ExchangeOfBill).WithMany().HasForeignKey(x => x.ExchangeOfBillId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_Bills_GrandTotal", "[GrandTotal] >= 0"));
    }
}

public sealed class BillItemConfiguration : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> builder)
    {
        builder.ToTable("BillItems");
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Rate).Money();
        builder.Property(x => x.PurchasePrice).Money();
        builder.Property(x => x.DiscountAmount).Money();
        builder.Property(x => x.TaxPercent).HasPrecision(5, 2);
        builder.Property(x => x.TaxAmount).Money();
        builder.Property(x => x.Total).Money();
        builder.HasOne(x => x.Bill).WithMany(x => x.Items).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.Property(x => x.Amount).Money();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bill).WithMany(x => x.Payments).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] >= 0"));
    }
}

public sealed class HeldBillConfiguration : IEntityTypeConfiguration<HeldBill>
{
    public void Configure(EntityTypeBuilder<HeldBill> builder)
    {
        builder.ToTable("HeldBills");
        builder.Property(x => x.HoldReference).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ItemsJson).IsRequired();
        builder.Property(x => x.BillDiscount).Money();
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class ProductReturnConfiguration : IEntityTypeConfiguration<ProductReturn>
{
    public void Configure(EntityTypeBuilder<ProductReturn> builder)
    {
        builder.ToTable("Returns");
        builder.Property(x => x.ReturnNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ReturnNumber).IsUnique();
        builder.Property(x => x.OriginalBillNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReturnAmount).Money();
        builder.Property(x => x.GrossAmount).Money();
        builder.Property(x => x.DeductionAmount).Money();
        builder.Property(x => x.DeductionPercent).HasPrecision(5, 2);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OriginalBill).WithMany().HasForeignKey(x => x.OriginalBillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ExchangeBill).WithMany().HasForeignKey(x => x.ExchangeBillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AppliedToBill).WithMany().HasForeignKey(x => x.AppliedToBillId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AppliedToBillId);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
{
    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.ToTable("ReturnItems");
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Rate).Money();
        builder.Property(x => x.TaxAmount).Money();
        builder.Property(x => x.Total).Money();
        builder.HasOne(x => x.ProductReturn).WithMany(x => x.Items).HasForeignKey(x => x.ProductReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OriginalBillItem).WithMany().HasForeignKey(x => x.OriginalBillItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
