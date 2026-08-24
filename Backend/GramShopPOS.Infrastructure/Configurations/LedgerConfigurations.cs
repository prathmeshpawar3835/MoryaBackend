using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GramShopPOS.Infrastructure.Configurations;

public sealed class CustomerLedgerConfiguration : IEntityTypeConfiguration<CustomerLedger>
{
    public void Configure(EntityTypeBuilder<CustomerLedger> builder)
    {
        builder.ToTable("CustomerLedgers");
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.StoreId);
        builder.Property(x => x.Debit).Money();
        builder.Property(x => x.Credit).Money();
        builder.Property(x => x.Balance).Money();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Customer).WithMany(x => x.LedgerEntries).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayments");
        builder.Property(x => x.Amount).Money();
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("Referrals");
        builder.HasIndex(x => x.ReferrerCustomerId);
        builder.HasIndex(x => x.ReferredCustomerId);
        builder.Property(x => x.RewardAmount).Money();
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReferrerCustomer).WithMany().HasForeignKey(x => x.ReferrerCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReferredCustomer).WithMany().HasForeignKey(x => x.ReferredCustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReferralRewardConfiguration : IEntityTypeConfiguration<ReferralReward>
{
    public void Configure(EntityTypeBuilder<ReferralReward> builder)
    {
        builder.ToTable("ReferralRewards");
        builder.Property(x => x.Amount).Money();
        builder.HasOne(x => x.Referral).WithMany(x => x.Rewards).HasForeignKey(x => x.ReferralId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");
        builder.Property(x => x.Amount).Money();
        builder.Property(x => x.BalanceAfter).Money();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Customer).WithMany(x => x.WalletTransactions).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BillSequenceConfiguration : IEntityTypeConfiguration<BillSequence>
{
    public void Configure(EntityTypeBuilder<BillSequence> builder)
    {
        builder.ToTable("BillSequences");
        builder.HasIndex(x => new { x.StoreId, x.FinancialYearCode }).IsUnique();
        builder.Property(x => x.FinancialYearCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReturnSequenceConfiguration : IEntityTypeConfiguration<ReturnSequence>
{
    public void Configure(EntityTypeBuilder<ReturnSequence> builder)
    {
        builder.ToTable("ReturnSequences");
        builder.HasIndex(x => new { x.StoreId, x.FinancialYearCode }).IsUnique();
        builder.Property(x => x.FinancialYearCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BusinessSettingConfiguration : IEntityTypeConfiguration<BusinessSetting>
{
    public void Configure(EntityTypeBuilder<BusinessSetting> builder)
    {
        builder.ToTable("BusinessSettings");
        builder.Property(x => x.ShopName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DefaultTaxPercent).HasPrecision(5, 2);
        builder.Property(x => x.NewCustomerReward).Money();
        builder.Property(x => x.ReferrerReward).Money();
        builder.Property(x => x.LowStockDefaultLevel).HasPrecision(18, 3);
    }
}

public sealed class TaxSettingConfiguration : IEntityTypeConfiguration<TaxSetting>
{
    public void Configure(EntityTypeBuilder<TaxSetting> builder)
    {
        builder.ToTable("TaxSettings");
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Percent).HasPrecision(5, 2);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(50);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.HasIndex(x => x.CreatedDate);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("RevokedTokens");
        builder.Property(x => x.Jti).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Jti).IsUnique();
    }
}

public sealed class ProductImportBatchConfiguration : IEntityTypeConfiguration<ProductImportBatch>
{
    public void Configure(EntityTypeBuilder<ProductImportBatch> builder)
    {
        builder.ToTable("ProductImportBatches");
        builder.HasIndex(x => x.BatchId).IsUnique();
    }
}
