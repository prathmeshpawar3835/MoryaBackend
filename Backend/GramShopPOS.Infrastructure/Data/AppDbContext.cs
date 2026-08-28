using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GramShopPOS.Infrastructure.Data;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreUser> StoreUsers => Set<StoreUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<ProductUnitSequence> ProductUnitSequences => Set<ProductUnitSequence>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<HeldBill> HeldBills => Set<HeldBill>();
    public DbSet<ProductReturn> Returns => Set<ProductReturn>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<CustomerLedger> CustomerLedgers => Set<CustomerLedger>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<ReferralReward> ReferralRewards => Set<ReferralReward>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<BillSequence> BillSequences => Set<BillSequence>();
    public DbSet<ReturnSequence> ReturnSequences => Set<ReturnSequence>();
    public DbSet<BusinessSetting> BusinessSettings => Set<BusinessSetting>();
    public DbSet<TaxSetting> TaxSettings => Set<TaxSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<ProductImportBatch> ProductImportBatches => Set<ProductImportBatch>();
    public DbSet<StoreDiscount> StoreDiscounts => Set<StoreDiscount>();
    public DbSet<BirthdayOfferRedemption> BirthdayOfferRedemptions => Set<BirthdayOfferRedemption>();
    public DbSet<BirthdayMessageLog> BirthdayMessageLogs => Set<BirthdayMessageLog>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<RepairJob> RepairJobs => Set<RepairJob>();
    public DbSet<RepairJobHistory> RepairJobHistories => Set<RepairJobHistory>();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        await Database.BeginTransactionAsync(cancellationToken);

    public async Task ReloadTrackedAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        var entry = Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            return;
        }

        await entry.ReloadAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var rowVersion = entity.FindProperty("RowVersion");
                if (rowVersion is not null)
                {
                    rowVersion.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                    rowVersion.IsConcurrencyToken = false;
                }

                foreach (var index in entity.GetIndexes().Where(i => i.GetFilter() is not null).ToList())
                {
                    index.SetFilter(null);
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
