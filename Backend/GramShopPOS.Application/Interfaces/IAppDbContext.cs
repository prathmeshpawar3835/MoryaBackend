using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace GramShopPOS.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Store> Stores { get; }
    DbSet<StoreUser> StoreUsers { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<StockTransfer> StockTransfers { get; }
    DbSet<StockTransferItem> StockTransferItems { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseItem> PurchaseItems { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Bill> Bills { get; }
    DbSet<BillItem> BillItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<HeldBill> HeldBills { get; }
    DbSet<ProductReturn> Returns { get; }
    DbSet<ReturnItem> ReturnItems { get; }
    DbSet<CustomerLedger> CustomerLedgers { get; }
    DbSet<CustomerPayment> CustomerPayments { get; }
    DbSet<Referral> Referrals { get; }
    DbSet<ReferralReward> ReferralRewards { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }
    DbSet<BillSequence> BillSequences { get; }
    DbSet<ReturnSequence> ReturnSequences { get; }
    DbSet<BusinessSetting> BusinessSettings { get; }
    DbSet<TaxSetting> TaxSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<RevokedToken> RevokedTokens { get; }
    DbSet<ProductImportBatch> ProductImportBatches { get; }
    DbSet<StoreDiscount> StoreDiscounts { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<RepairJob> RepairJobs { get; }
    DbSet<RepairJobHistory> RepairJobHistories { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task ReloadTrackedAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
}
