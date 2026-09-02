using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.DTOs.Operations;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class ReferralService : IReferralService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public ReferralService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResponse<ReferralDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.Referrals.AsNoTracking().AsQueryable();
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(r => ids.Contains(r.StoreId));
        }

        if (request.StoreId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(request.StoreId.Value);
            query = query.Where(r => r.StoreId == request.StoreId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(r =>
                r.ReferralCode.Contains(s) ||
                r.ReferrerCustomer.Name.Contains(s) ||
                r.ReferredCustomer.Name.Contains(s) ||
                r.ReferrerCustomer.ReferralCode.Contains(s) ||
                r.ReferrerCustomer.CustomerCode.Contains(s) ||
                r.ReferredCustomer.CustomerCode.Contains(s) ||
                r.ReferredCustomer.ReferralCode.Contains(s));
        }

        var projected = query.OrderByDescending(r => r.ReferralDate).Select(r => new ReferralDto
        {
            Id = r.Id,
            ReferrerCustomerId = r.ReferrerCustomerId,
            ReferrerName = r.ReferrerCustomer.Name,
            ReferredCustomerId = r.ReferredCustomerId,
            ReferredName = r.ReferredCustomer.Name,
            RewardAmount = r.RewardAmount,
            SaleAmount = r.SaleAmount,
            DiscountGiven = r.DiscountGiven,
            ReferralCode = r.ReferralCode,
            BillNumber = r.Bill != null ? r.Bill.BillNumber : null,
            Status = r.Status,
            ReferralDate = r.ReferralDate
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<ReferralValidationDto> ValidateCodeAsync(string code, int? excludeCustomerId, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        if (string.IsNullOrWhiteSpace(code))
        {
            return new ReferralValidationDto { Valid = false, Message = "Referral code is required." };
        }

        var query = CustomerReferral.MatchingCode(_db.Customers.AsNoTracking().Where(c => !c.IsDeleted && c.IsActive), code);
        if (storeId.HasValue)
        {
            var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
            if (settings.ReferralStoreWise)
            {
                query = query.Where(c => c.StoreId == storeId.Value);
            }
        }

        var referrer = await query.FirstOrDefaultAsync(cancellationToken);
        if (referrer is null)
        {
            return new ReferralValidationDto { Valid = false, Message = "Invalid customer / referral code." };
        }

        if (excludeCustomerId.HasValue && referrer.Id == excludeCustomerId.Value)
        {
            return new ReferralValidationDto { Valid = false, Message = "A customer cannot refer themselves." };
        }

        var settingsRow = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        return new ReferralValidationDto
        {
            Valid = true,
            Message = "Referral code is valid.",
            ReferrerCustomerId = referrer.Id,
            ReferrerName = referrer.Name,
            ReferrerMobile = referrer.MobileNumber,
            ReferrerCode = referrer.ReferralCode,
            ReferrerWalletBalance = referrer.WalletBalance,
            NewCustomerDiscountRate = settingsRow.NewCustomerReward,
            ReferrerBenefitRate = settingsRow.ReferrerReward,
            RewardType = settingsRow.RewardType
        };
    }

    public async Task<ReferralPreviewDto> PreviewAsync(
        Customer? customer,
        string? referralCode,
        string? referringMobile,
        decimal eligibleAmount,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (!settings.ReferralEnabled)
        {
            return new ReferralPreviewDto { EligibleAmount = eligibleAmount };
        }

        if (string.IsNullOrWhiteSpace(referralCode) && string.IsNullOrWhiteSpace(referringMobile))
        {
            return new ReferralPreviewDto { EligibleAmount = eligibleAmount };
        }

        var referrer = await ResolveReferrerAsync(customer, referralCode, referringMobile, storeId, settings, true, cancellationToken);
        if (referrer is null)
        {
            return new ReferralPreviewDto { EligibleAmount = eligibleAmount };
        }

        if (customer is not null)
        {
            var priorSales = await _db.Bills.CountAsync(
                b => b.CustomerId == customer.Id && b.Status != BillStatus.Cancelled && b.BillType == BillType.Sale,
                cancellationToken);
            if (priorSales > 0)
            {
                return new ReferralPreviewDto { EligibleAmount = eligibleAmount };
            }
        }

        var discount = ReferralCalculator.ComputeBenefit(eligibleAmount, settings.NewCustomerReward, settings.RewardType);
        var benefit = ReferralCalculator.ComputeBenefit(eligibleAmount, settings.ReferrerReward, settings.RewardType);
        return new ReferralPreviewDto
        {
            Applies = true,
            EligibleAmount = eligibleAmount,
            NewCustomerDiscount = discount,
            ReferrerBenefit = benefit,
            Referrer = new ReferralValidationDto
            {
                Valid = true,
                ReferrerCustomerId = referrer.Id,
                ReferrerName = referrer.Name,
                ReferrerMobile = referrer.MobileNumber,
                ReferrerCode = referrer.ReferralCode,
                ReferrerWalletBalance = referrer.WalletBalance,
                NewCustomerDiscountRate = settings.NewCustomerReward,
                ReferrerBenefitRate = settings.ReferrerReward,
                RewardType = settings.RewardType
            }
        };
    }

    public async Task ProcessSaleAsync(Customer customer, Bill bill, CreateBillRequest request, decimal eligibleAmount, decimal referralDiscount, CancellationToken cancellationToken = default)
    {
        var settings = await _db.BusinessSettings.FirstAsync(cancellationToken);
        if (!settings.ReferralEnabled || bill.BillType != BillType.Sale)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ReferralCode) && string.IsNullOrWhiteSpace(request.ReferringMobileNumber))
        {
            return;
        }

        var already = await _db.ReferralRewards.AnyAsync(
            r => r.BillId == bill.Id && r.IsReferrerReward && !r.IsReversal,
            cancellationToken);
        if (already)
        {
            return;
        }

        var referrer = await ResolveReferrerAsync(customer, request.ReferralCode, request.ReferringMobileNumber, bill.StoreId, settings, true, cancellationToken);
        if (referrer is null)
        {
            return;
        }

        var priorSales = await _db.Bills.CountAsync(
            b => b.CustomerId == customer.Id && b.Status != BillStatus.Cancelled && b.BillType == BillType.Sale && b.Id != bill.Id,
            cancellationToken);
        if (priorSales > 0)
        {
            return;
        }

        var existing = await _db.Referrals.FirstOrDefaultAsync(r => r.ReferredCustomerId == customer.Id, cancellationToken);
        if (existing is not null && existing.BillId.HasValue && existing.BillId != bill.Id)
        {
            return;
        }

        if (existing is null)
        {
            existing = new Referral
            {
                StoreId = bill.StoreId,
                ReferrerCustomerId = referrer.Id,
                ReferredCustomerId = customer.Id,
                BillId = bill.Id,
                SalesPersonId = bill.SalesPersonId,
                ReferralCode = referrer.ReferralCode,
                ReferralDate = DateTime.Now,
                Status = ReferralRewardStatus.Pending,
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            _db.Referrals.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
            if (!customer.ReferredByCustomerId.HasValue)
            {
                customer.ReferredByCustomerId = referrer.Id;
            }
        }

        var benefit = bill.ReferrerBenefitAmount > 0
            ? bill.ReferrerBenefitAmount
            : ReferralCalculator.ComputeBenefit(eligibleAmount, settings.ReferrerReward, settings.RewardType);
        existing.BillId = bill.Id;
        existing.SalesPersonId = bill.SalesPersonId;
        existing.ReferralCode = referrer.ReferralCode;
        existing.SaleAmount = eligibleAmount;
        existing.DiscountGiven = referralDiscount;
        existing.NewCustomerPercent = bill.ReferralDiscountPercent;
        existing.ReferrerPercent = bill.ReferrerBenefitPercent;
        existing.RewardAmount = benefit;
        existing.Status = benefit > 0 ? ReferralRewardStatus.Credited : ReferralRewardStatus.Pending;

        if (benefit > 0)
        {
            await CreditReferrerAsync(existing, referrer, bill, benefit, cancellationToken);
        }

        await _audit.LogAsync(AuditActions.ReferralReward, nameof(Referral), existing.Id.ToString(), null, new { benefit, referralDiscount, bill.BillNumber }, bill.StoreId, cancellationToken);
    }

    public async Task AdjustForReturnAsync(Bill originalBill, ProductReturn ret, CancellationToken cancellationToken = default)
    {
        if (!originalBill.CustomerId.HasValue)
        {
            return;
        }

        var referral = await _db.Referrals
            .Include(r => r.Rewards)
            .FirstOrDefaultAsync(r => r.ReferredCustomerId == originalBill.CustomerId && r.BillId == originalBill.Id, cancellationToken)
            ?? await _db.Referrals
                .Include(r => r.Rewards)
                .FirstOrDefaultAsync(r => r.ReferredCustomerId == originalBill.CustomerId && r.BillId == null, cancellationToken);
        if (referral is null)
        {
            referral = await _db.Referrals.Include(r => r.Rewards)
                .Where(r => r.ReferredCustomerId == originalBill.CustomerId)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (referral is null || referral.RewardAmount <= 0 || referral.SaleAmount <= 0)
        {
            return;
        }

        var alreadyReversedForThisReturn = referral.Rewards.Any(r => r.IsReversal && r.ReturnId == ret.Id);
        if (alreadyReversedForThisReturn)
        {
            return;
        }

        var returnedEligible = originalBill.GrandTotal <= 0
            ? 0
            : Money.Round(referral.SaleAmount * (ret.ReturnAmount / originalBill.GrandTotal));
        var remainingEligible = Money.Round(referral.SaleAmount - returnedEligible);
        if (remainingEligible < 0)
        {
            remainingEligible = 0;
        }

        var remainingBenefit = ReferralCalculator.RemainingBenefit(referral.RewardAmount, referral.SaleAmount, remainingEligible);
        var credited = referral.Rewards.Where(r => r.IsReferrerReward && !r.IsReversal).Sum(r => r.Amount);
        var reversed = referral.Rewards.Where(r => r.IsReferrerReward && r.IsReversal).Sum(r => r.Amount);
        var net = Money.Round(credited - reversed);
        var debit = Money.Round(net - remainingBenefit);
        if (debit <= 0)
        {
            return;
        }

        var referrer = await _db.Customers.FirstAsync(c => c.Id == referral.ReferrerCustomerId, cancellationToken);
        var type = ret.ReturnKind == ReturnKind.Exchange ? LedgerTransactionType.ExchangeAdjustment : LedgerTransactionType.ReferralReversal;
        await DebitReferrerAsync(
            referral,
            referrer,
            originalBill,
            ret,
            debit,
            type,
            ret.ReturnKind == ReturnKind.Exchange
                ? $"Referral reversal on exchange {ret.ReturnNumber} / invoice {originalBill.BillNumber}"
                : $"Referral reversal on return {ret.ReturnNumber} / invoice {originalBill.BillNumber}",
            cancellationToken);
        await _audit.LogAsync(AuditActions.ReferralReversal, nameof(Referral), referral.Id.ToString(), null, new { debit, ret.ReturnNumber, originalBill.BillNumber }, originalBill.StoreId, cancellationToken);
    }

    private async Task<Customer?> ResolveReferrerAsync(
        Customer? customer,
        string? referralCode,
        string? referringMobile,
        int storeId,
        BusinessSetting settings,
        bool requireValidCode,
        CancellationToken cancellationToken)
    {
        Customer? referrer = null;
        if (!string.IsNullOrWhiteSpace(referralCode))
        {
            var query = CustomerReferral.MatchingCode(_db.Customers.Where(c => !c.IsDeleted && c.IsActive), referralCode);
            if (settings.ReferralStoreWise)
            {
                query = query.Where(c => c.StoreId == storeId);
            }

            referrer = await query.FirstOrDefaultAsync(cancellationToken);
            if (referrer is null)
            {
                if (requireValidCode)
                {
                    throw new ValidationAppException("Invalid customer / referral code.");
                }

                return null;
            }
        }
        else if (!string.IsNullOrWhiteSpace(referringMobile))
        {
            referrer = await _db.Customers.FirstOrDefaultAsync(c => c.MobileNumber == referringMobile.Trim() && !c.IsDeleted && c.IsActive, cancellationToken);
            if (referrer is null && requireValidCode)
            {
                throw new ValidationAppException("Referring customer mobile was not found.");
            }
        }

        if (referrer is null)
        {
            return null;
        }

        if (customer is not null && referrer.Id == customer.Id)
        {
            throw new ValidationAppException("A customer cannot refer themselves.");
        }

        return referrer;
    }

    private async Task CreditReferrerAsync(Referral referral, Customer referrer, Bill bill, decimal amount, CancellationToken cancellationToken)
    {
        await _db.Customers.Where(c => c.Id == referrer.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.WalletBalance, c => c.WalletBalance + amount), cancellationToken);
        await ReloadCustomerAsync(referrer, cancellationToken);
        _db.WalletTransactions.Add(new WalletTransaction
        {
            CustomerId = referrer.Id,
            StoreId = bill.StoreId,
            Amount = amount,
            BalanceAfter = referrer.WalletBalance,
            TransactionType = LedgerTransactionType.ReferralCredit,
            Description = $"Referral credit for {bill.BillNumber} (referred customer {bill.CustomerId})",
            ReferenceId = bill.Id,
            ReferenceNumber = bill.BillNumber,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.Now,
            IsActive = true
        });
        var ledger = await AddLedgerAsync(
            referrer,
            bill.StoreId,
            bill.Id,
            bill.BillNumber,
            0,
            amount,
            LedgerTransactionType.ReferralCredit,
            $"Referral credit · referred invoice {bill.BillNumber}",
            cancellationToken);
        _db.ReferralRewards.Add(new ReferralReward
        {
            ReferralId = referral.Id,
            CustomerId = referrer.Id,
            BillId = bill.Id,
            LedgerEntryId = ledger.Id,
            Amount = amount,
            Status = ReferralRewardStatus.Credited,
            IsReferrerReward = true,
            IsReversal = false,
            Description = $"Referral credit {bill.BillNumber}",
            CreatedDate = DateTime.Now,
            IsActive = true
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task DebitReferrerAsync(
        Referral referral,
        Customer referrer,
        Bill originalBill,
        ProductReturn ret,
        decimal amount,
        LedgerTransactionType type,
        string description,
        CancellationToken cancellationToken)
    {
        var walletDeduct = amount;
        var rows = await _db.Customers
            .Where(c => c.Id == referrer.Id && c.WalletBalance >= amount)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.WalletBalance, c => c.WalletBalance - amount), cancellationToken);
        if (rows == 0)
        {
            var current = await _db.Customers.AsNoTracking().Where(c => c.Id == referrer.Id).Select(c => c.WalletBalance).FirstAsync(cancellationToken);
            walletDeduct = current > 0 ? current : 0;
            if (walletDeduct > 0)
            {
                await _db.Customers.Where(c => c.Id == referrer.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.WalletBalance, 0), cancellationToken);
            }
        }

        await ReloadCustomerAsync(referrer, cancellationToken);
        if (walletDeduct > 0)
        {
            _db.WalletTransactions.Add(new WalletTransaction
            {
                CustomerId = referrer.Id,
                StoreId = originalBill.StoreId,
                Amount = -walletDeduct,
                BalanceAfter = referrer.WalletBalance,
                TransactionType = type,
                Description = description,
                ReferenceId = ret.Id,
                ReferenceNumber = ret.ReturnNumber,
                UserId = _currentUser.UserId,
                CreatedDate = DateTime.Now,
                IsActive = true
            });
        }

        var ledger = await AddLedgerAsync(
            referrer,
            originalBill.StoreId,
            ret.Id,
            ret.ReturnNumber,
            amount,
            0,
            type,
            description,
            cancellationToken);
        _db.ReferralRewards.Add(new ReferralReward
        {
            ReferralId = referral.Id,
            CustomerId = referrer.Id,
            BillId = originalBill.Id,
            ReturnId = ret.Id,
            LedgerEntryId = ledger.Id,
            Amount = amount,
            Status = ReferralRewardStatus.Cancelled,
            IsReferrerReward = true,
            IsReversal = true,
            Description = description,
            CreatedDate = DateTime.Now,
            IsActive = true
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<CustomerLedger> AddLedgerAsync(
        Customer customer,
        int storeId,
        int? referenceId,
        string reference,
        decimal debit,
        decimal credit,
        LedgerTransactionType type,
        string description,
        CancellationToken cancellationToken)
    {
        var latest = await _db.CustomerLedgers.Where(l => l.CustomerId == customer.Id)
            .OrderByDescending(l => l.Id)
            .Select(l => (decimal?)l.Balance)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;
        var balance = Money.Round(latest + debit - credit);
        var entry = new CustomerLedger
        {
            CustomerId = customer.Id,
            StoreId = storeId,
            ReferenceId = referenceId,
            ReferenceNumber = reference,
            Debit = Money.Round(debit),
            Credit = Money.Round(credit),
            Balance = balance,
            TransactionType = type,
            Description = description,
            TransactionDate = DateTime.Now,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.Now,
            IsActive = true
        };
        _db.CustomerLedgers.Add(entry);
        customer.OutstandingBalance = balance;
        customer.UpdatedDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    private async Task ReloadCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        var outstanding = customer.OutstandingBalance;
        var referredBy = customer.ReferredByCustomerId;
        await _db.ReloadTrackedAsync(customer, cancellationToken);
        customer.OutstandingBalance = outstanding;
        customer.ReferredByCustomerId = referredBy;
    }
}
