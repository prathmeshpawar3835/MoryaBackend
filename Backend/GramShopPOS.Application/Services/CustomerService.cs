using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public CustomerService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResponse<CustomerDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = BaseQuery();
        if (request.StoreId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(request.StoreId.Value);
            query = query.Where(c => c.StoreId == request.StoreId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(c => c.Name.Contains(s) || c.MobileNumber.Contains(s) || c.ReferralCode.Contains(s));
        }

        var projected = query.OrderBy(c => c.Name).Select(MapExpr);
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<CustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().Include(c => c.Store).Include(c => c.ReferredByCustomer)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);
        return Map(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            throw new ValidationAppException("Name and mobile number are required.");
        }

        if (await _db.Customers.AnyAsync(c => c.MobileNumber == request.MobileNumber.Trim() && !c.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("A customer with this mobile number already exists.");
        }

        int? referrerId = null;
        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            var referrer = await _db.Customers.FirstOrDefaultAsync(c => c.ReferralCode == request.ReferralCode.Trim() && !c.IsDeleted, cancellationToken)
                ?? throw new ValidationAppException("Invalid customer / referral code.");
            if (referrer.MobileNumber == request.MobileNumber.Trim())
            {
                throw new ValidationAppException("A customer cannot refer themselves.");
            }

            referrerId = referrer.Id;
        }
        else if (!string.IsNullOrWhiteSpace(request.ReferringMobileNumber))
        {
            if (request.ReferringMobileNumber.Trim() == request.MobileNumber.Trim())
            {
                throw new ValidationAppException("A customer cannot refer themselves.");
            }

            referrerId = await _db.Customers.Where(c => c.MobileNumber == request.ReferringMobileNumber.Trim() && !c.IsDeleted).Select(c => (int?)c.Id).FirstOrDefaultAsync(cancellationToken)
                ?? throw new ValidationAppException("Referring customer mobile was not found.");
        }

        var customer = new Customer
        {
            StoreId = storeId,
            Name = request.Name.Trim(),
            MobileNumber = request.MobileNumber.Trim(),
            Address = request.Address,
            ReferralCode = await UniqueReferralCodeAsync(cancellationToken),
            ReferredByCustomerId = referrerId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.CustomerCreated, nameof(Customer), customer.Id.ToString(), null, customer, storeId, cancellationToken);
        return await GetByIdAsync(customer.Id, cancellationToken);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);
        if (await _db.Customers.AnyAsync(c => c.MobileNumber == request.MobileNumber.Trim() && c.Id != id && !c.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("A customer with this mobile number already exists.");
        }

        customer.Name = request.Name.Trim();
        customer.MobileNumber = request.MobileNumber.Trim();
        customer.Address = request.Address;
        customer.IsActive = request.IsActive;
        customer.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.CustomerUpdated, nameof(Customer), id.ToString(), null, customer, customer.StoreId, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string query, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var q = BaseQuery();
        if (storeId.HasValue || !_currentUser.IsAdmin)
        {
            var resolved = _currentUser.Access().ResolveStoreId(storeId);
            q = q.Where(c => c.StoreId == resolved);
        }

        var s = query.Trim();
        return await q.Where(c => c.Name.Contains(s) || c.MobileNumber.Contains(s) || c.ReferralCode.Contains(s))
            .OrderBy(c => c.Name)
            .Take(50)
            .Select(MapExpr)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetByMobileAsync(string mobile, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var number = mobile.Trim();
        if (string.IsNullOrWhiteSpace(number))
        {
            return null;
        }

        var q = BaseQuery().Where(c => c.MobileNumber == number);
        if (storeId.HasValue || !_currentUser.IsAdmin)
        {
            var resolved = _currentUser.Access().ResolveStoreId(storeId);
            q = q.Where(c => c.StoreId == resolved);
        }

        return await q.Select(MapExpr).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerHistoryDto> GetHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await GetByIdAsync(id, cancellationToken);
        var bills = await _db.Bills.AsNoTracking()
            .Where(b => b.CustomerId == id)
            .OrderByDescending(b => b.BillDate)
            .Take(50)
            .Select(b => new BillDto
            {
                Id = b.Id,
                StoreId = b.StoreId,
                BillNumber = b.BillNumber,
                BillDate = b.BillDate,
                GrandTotal = b.GrandTotal,
                PaidAmount = b.PaidAmount,
                DueAmount = b.DueAmount,
                Status = b.Status,
                BillType = b.BillType
            })
            .ToListAsync(cancellationToken);
        var returns = await _db.Returns.AsNoTracking()
            .Where(r => r.CustomerId == id)
            .OrderByDescending(r => r.ReturnDate)
            .Take(50)
            .Select(r => new ReturnDto
            {
                Id = r.Id,
                OriginalBillId = r.OriginalBillId,
                OriginalBillNumber = r.OriginalBillNumber,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                ReturnAmount = r.ReturnAmount,
                ReturnKind = r.ReturnKind
            })
            .ToListAsync(cancellationToken);
        return new CustomerHistoryDto { Customer = customer, Bills = bills, Returns = returns };
    }

    public async Task<PagedResponse<LedgerEntryDto>> GetLedgerAsync(int id, PagedRequest request, CancellationToken cancellationToken = default)
    {
        await GetByIdAsync(id, cancellationToken);
        var query = _db.CustomerLedgers.AsNoTracking().Where(l => l.CustomerId == id).OrderByDescending(l => l.Id)
            .Select(l => new LedgerEntryDto
            {
                Id = l.Id,
                TransactionDate = l.TransactionDate,
                TransactionType = l.TransactionType,
                Description = l.Description,
                ReferenceNumber = l.ReferenceNumber,
                Debit = l.Debit,
                Credit = l.Credit,
                Balance = l.Balance
            });
        return await query.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PaymentDto> ReceivePaymentAsync(int customerId, CustomerPaymentRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        if (request.Amount <= 0)
        {
            throw new ValidationAppException("Payment amount must be greater than zero.");
        }

        if (request.PaymentMode is PaymentMode.Credit or PaymentMode.Wallet)
        {
            throw new ValidationAppException("Customer settlement must be Cash, UPI or Card.");
        }

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);

        var payment = new Payment
        {
            StoreId = storeId,
            CustomerId = customer.Id,
            PaymentMode = request.PaymentMode,
            Amount = Money.Round(request.Amount),
            ReferenceNumber = request.ReferenceNumber,
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            Notes = request.Notes,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);
        _db.CustomerPayments.Add(new CustomerPayment
        {
            CustomerId = customer.Id,
            StoreId = storeId,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Notes = request.Notes,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });

        var latest = await _db.CustomerLedgers.Where(l => l.CustomerId == customer.Id).OrderByDescending(l => l.Id).Select(l => (decimal?)l.Balance).FirstOrDefaultAsync(cancellationToken) ?? 0;
        var balance = Money.Round(latest - payment.Amount);
        _db.CustomerLedgers.Add(new CustomerLedger
        {
            CustomerId = customer.Id,
            StoreId = storeId,
            ReferenceId = payment.Id,
            ReferenceNumber = payment.ReferenceNumber,
            Debit = 0,
            Credit = payment.Amount,
            Balance = balance,
            TransactionType = LedgerTransactionType.PaymentReceived,
            Description = request.Notes ?? "Customer payment",
            TransactionDate = payment.PaymentDate,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });
        customer.OutstandingBalance = balance;
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.PaymentReceived, nameof(Payment), payment.Id.ToString(), null, new { payment.Amount }, storeId, cancellationToken);
        return new PaymentDto { Id = payment.Id, PaymentMode = payment.PaymentMode, Amount = payment.Amount, ReferenceNumber = payment.ReferenceNumber, PaymentDate = payment.PaymentDate };
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(int customerId, CancellationToken cancellationToken = default)
    {
        await GetByIdAsync(customerId, cancellationToken);
        return await _db.Payments.AsNoTracking()
            .Where(p => p.CustomerId == customerId && p.BillId == null)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                PaymentMode = p.PaymentMode,
                Amount = p.Amount,
                ReferenceNumber = p.ReferenceNumber,
                PaymentDate = p.PaymentDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<WalletDto> GetWalletAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);
        var tx = await _db.WalletTransactions.AsNoTracking()
            .Where(w => w.CustomerId == customerId)
            .OrderByDescending(w => w.Id)
            .Take(100)
            .Select(w => new WalletTransactionDto
            {
                Id = w.Id,
                Amount = w.Amount,
                BalanceAfter = w.BalanceAfter,
                TransactionType = w.TransactionType,
                Description = w.Description,
                CreatedDate = w.CreatedDate
            })
            .ToListAsync(cancellationToken);
        return new WalletDto { CustomerId = customerId, Balance = customer.WalletBalance, Transactions = tx };
    }

    public async Task RedeemWalletAsync(int customerId, WalletRedeemRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        if (request.Amount <= 0)
        {
            throw new ValidationAppException("Redeem amount must be greater than zero.");
        }

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);

        var rows = await _db.Customers
            .Where(c => c.Id == customerId && c.WalletBalance >= request.Amount)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.WalletBalance, c => c.WalletBalance - request.Amount), cancellationToken);
        if (rows == 0)
        {
            throw new BusinessAppException("Insufficient wallet balance or wallet was updated concurrently.");
        }

        var updated = await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId, cancellationToken);
        _db.WalletTransactions.Add(new WalletTransaction
        {
            CustomerId = customerId,
            StoreId = storeId,
            Amount = -request.Amount,
            BalanceAfter = updated.WalletBalance,
            TransactionType = LedgerTransactionType.WalletRedeem,
            Description = request.Notes ?? "Wallet redemption",
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });
        var latest = await _db.CustomerLedgers.Where(l => l.CustomerId == customerId).OrderByDescending(l => l.Id).Select(l => (decimal?)l.Balance).FirstOrDefaultAsync(cancellationToken) ?? 0;
        _db.CustomerLedgers.Add(new CustomerLedger
        {
            CustomerId = customerId,
            StoreId = storeId,
            Debit = 0,
            Credit = request.Amount,
            Balance = Money.Round(latest - request.Amount),
            TransactionType = LedgerTransactionType.WalletRedeem,
            Description = request.Notes ?? "Wallet redemption",
            TransactionDate = DateTime.UtcNow,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.WalletUsed, nameof(Customer), customerId.ToString(), null, new { request.Amount }, storeId, cancellationToken);
    }

    private IQueryable<Customer> BaseQuery()
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(c => ids.Contains(c.StoreId));
        }

        return query;
    }

    private async Task<string> UniqueReferralCodeAsync(CancellationToken cancellationToken)
    {
        string code;
        do
        {
            code = $"RF{Random.Shared.Next(100000, 999999)}";
        } while (await _db.Customers.AnyAsync(c => c.ReferralCode == code, cancellationToken));

        return code;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Customer, CustomerDto>> MapExpr = c => new CustomerDto
    {
        Id = c.Id,
        StoreId = c.StoreId,
        StoreName = c.Store.StoreName,
        Name = c.Name,
        MobileNumber = c.MobileNumber,
        Address = c.Address,
        ReferralCode = c.ReferralCode,
        CustomerCode = c.ReferralCode,
        ReferredByCustomerId = c.ReferredByCustomerId,
        ReferredByName = c.ReferredByCustomer != null ? c.ReferredByCustomer.Name : null,
        OutstandingBalance = c.OutstandingBalance,
        WalletBalance = c.WalletBalance,
        IsActive = c.IsActive,
        CreatedDate = c.CreatedDate
    };

    private static CustomerDto Map(Customer c) => new()
    {
        Id = c.Id,
        StoreId = c.StoreId,
        StoreName = c.Store?.StoreName ?? string.Empty,
        Name = c.Name,
        MobileNumber = c.MobileNumber,
        Address = c.Address,
        ReferralCode = c.ReferralCode,
        CustomerCode = c.ReferralCode,
        ReferredByCustomerId = c.ReferredByCustomerId,
        ReferredByName = c.ReferredByCustomer?.Name,
        OutstandingBalance = c.OutstandingBalance,
        WalletBalance = c.WalletBalance,
        IsActive = c.IsActive,
        CreatedDate = c.CreatedDate
    };
}
