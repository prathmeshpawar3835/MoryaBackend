using System.Text.Json;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class BillingService : IBillingService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockEngine _stock;
    private readonly IDocumentNumberGenerator _numbers;
    private readonly IAuditService _audit;

    public BillingService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IStockEngine stock,
        IDocumentNumberGenerator numbers,
        IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _stock = stock;
        _numbers = numbers;
        _audit = audit;
    }

    public async Task<BillDto> CreateBillAsync(CreateBillRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        if (request.Items.Count == 0)
        {
            throw new ValidationAppException("A bill must contain at least one item.");
        }

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var dto = await CreateBillCoreAsync(request, storeId, BillType.Sale, null, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.BillCreated, nameof(Bill), dto.Id.ToString(), null, new { dto.BillNumber, dto.GrandTotal }, storeId, cancellationToken);
        return dto;
    }

    internal async Task<BillDto> CreateBillCoreAsync(
        CreateBillRequest request,
        int storeId,
        BillType billType,
        int? exchangeOfBillId,
        CancellationToken cancellationToken)
    {
        var settings = await _db.BusinessSettings.FirstAsync(cancellationToken);
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.IsActive && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Store not found.");

        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId && !c.IsDeleted, cancellationToken)
                ?? throw new NotFoundAppException("Customer not found.");
            _currentUser.Access().EnsureStoreAccess(customer.StoreId);
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id) && !p.IsDeleted).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new ValidationAppException("One or more products are invalid.");
        }

        if (products.Any(p => !p.IsActive))
        {
            throw new BusinessAppException("One or more products are inactive.");
        }

        var lineInputs = new List<(decimal Qty, decimal Rate, decimal Discount, decimal Tax, Product Product, CreateBillItemRequest Request)>();
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new ValidationAppException("Item quantity must be greater than zero.");
            }

            var product = products.First(p => p.Id == item.ProductId);
            lineInputs.Add((item.Quantity, product.SellingPrice, item.DiscountAmount, product.TaxPercent, product, item));
        }

        var totals = BillCalculator.CalculateTotals(
            lineInputs.Select(l => (l.Qty, l.Rate, l.Discount, l.Tax)).ToList(),
            request.BillDiscount);

        var creditPayment = request.Payments.Where(p => p.PaymentMode == PaymentMode.Credit).Sum(p => p.Amount);
        var collected = request.Payments.Where(p => p.PaymentMode != PaymentMode.Credit).ToList();
        if (collected.Any(p => p.Amount < 0) || request.WalletRedeemAmount < 0 || creditPayment < 0)
        {
            throw new ValidationAppException("Payment amounts cannot be negative.");
        }

        try
        {
            BillCalculator.ValidatePayments(totals.GrandTotal, request.WalletRedeemAmount, collected.Select(p => p.Amount).ToList(), creditPayment);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationAppException(ex.Message);
        }

        if (request.WalletRedeemAmount > 0)
        {
            if (customer is null)
            {
                throw new ValidationAppException("A customer is required to redeem wallet balance.");
            }

            var walletRows = await _db.Customers
                .Where(c => c.Id == customer.Id && c.WalletBalance >= request.WalletRedeemAmount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.WalletBalance, c => c.WalletBalance - request.WalletRedeemAmount)
                    .SetProperty(c => c.UpdatedDate, DateTime.UtcNow), cancellationToken);
            if (walletRows == 0)
            {
                throw new BusinessAppException("Insufficient wallet balance or wallet was updated concurrently.");
            }
        }

        if (creditPayment > 0 && customer is null)
        {
            throw new ValidationAppException("A customer is required for credit/udhaar.");
        }

        var prefix = string.IsNullOrWhiteSpace(store.InvoicePrefix) ? store.StoreCode : store.InvoicePrefix;
        var billNumber = await _numbers.NextBillNumberAsync(storeId, prefix, settings.FinancialYearStartMonth, cancellationToken);

        var paid = Money.Round(collected.Sum(p => p.Amount) + request.WalletRedeemAmount);
        var due = Money.Round(totals.GrandTotal - paid);
        var status = due <= 0 ? BillStatus.Completed : (paid == 0 ? BillStatus.Credit : BillStatus.PartiallyPaid);

        var bill = new Bill
        {
            StoreId = storeId,
            CustomerId = customer?.Id,
            SalesPersonId = _currentUser.UserId,
            BillNumber = billNumber,
            BillDate = DateTime.UtcNow,
            BillType = billType,
            Status = status,
            Subtotal = totals.Subtotal,
            ItemDiscountTotal = totals.ItemDiscountTotal,
            BillDiscount = totals.BillDiscount,
            TaxAmount = totals.TaxAmount,
            GrandTotal = totals.GrandTotal,
            PaidAmount = paid,
            DueAmount = due,
            WalletRedeemed = request.WalletRedeemAmount,
            Notes = request.Notes,
            ExchangeOfBillId = exchangeOfBillId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < lineInputs.Count; i++)
        {
            var src = lineInputs[i];
            var calc = totals.Lines[i];
            _db.BillItems.Add(new BillItem
            {
                BillId = bill.Id,
                ProductId = src.Product.Id,
                ProductCode = src.Product.ProductCode,
                ProductName = src.Product.ProductName,
                Quantity = calc.Quantity,
                Rate = calc.Rate,
                PurchasePrice = src.Product.PurchasePrice,
                DiscountAmount = calc.DiscountAmount,
                TaxPercent = calc.TaxPercent,
                TaxAmount = calc.TaxAmount,
                Total = calc.Total,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
            await _stock.ChangeAsync(
                storeId,
                src.Product.Id,
                -calc.Quantity,
                billType == BillType.Exchange ? StockMovementType.Exchange : StockMovementType.Sale,
                bill.Id,
                billNumber,
                null,
                settings.AllowNegativeStock,
                _currentUser.UserId,
                cancellationToken);
        }

        foreach (var payment in collected.Where(p => p.Amount > 0))
        {
            _db.Payments.Add(new Payment
            {
                StoreId = storeId,
                BillId = bill.Id,
                CustomerId = customer?.Id,
                PaymentMode = payment.PaymentMode,
                Amount = Money.Round(payment.Amount),
                ReferenceNumber = payment.ReferenceNumber,
                PaymentDate = DateTime.UtcNow,
                UserId = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
        }

        if (customer is not null)
        {
            await AddLedgerAsync(customer, storeId, bill.Id, billNumber, totals.GrandTotal, 0, LedgerTransactionType.Sale, $"Sale {billNumber}", cancellationToken);
            if (paid > 0)
            {
                await AddLedgerAsync(customer, storeId, bill.Id, billNumber, 0, paid, LedgerTransactionType.PaymentReceived, $"Payment against {billNumber}", cancellationToken);
            }

            if (request.WalletRedeemAmount > 0)
            {
                var walletAfter = await _db.Customers.AsNoTracking().Where(c => c.Id == customer.Id).Select(c => c.WalletBalance).FirstAsync(cancellationToken);
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    CustomerId = customer.Id,
                    StoreId = storeId,
                    Amount = -request.WalletRedeemAmount,
                    BalanceAfter = walletAfter,
                    TransactionType = LedgerTransactionType.WalletRedeem,
                    Description = $"Redeemed on {billNumber}",
                    ReferenceId = bill.Id,
                    ReferenceNumber = billNumber,
                    UserId = _currentUser.UserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                });
                await AddLedgerAsync(customer, storeId, bill.Id, billNumber, 0, request.WalletRedeemAmount, LedgerTransactionType.WalletRedeem, $"Wallet redeemed on {billNumber}", cancellationToken);
            }

            await ProcessReferralAsync(customer, bill, settings, request, cancellationToken);
        }

        if (request.HeldBillId.HasValue)
        {
            var held = await _db.HeldBills.FirstOrDefaultAsync(h => h.Id == request.HeldBillId && !h.IsDeleted, cancellationToken);
            if (held is not null)
            {
                _currentUser.Access().EnsureStoreAccess(held.StoreId);
                held.IsDeleted = true;
                held.UpdatedDate = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetBillAsync(bill.Id, cancellationToken);
    }

    public async Task<PagedResponse<BillDto>> GetBillsAsync(BillListRequest request, CancellationToken cancellationToken = default) =>
        await QueryBillsAsync(request, cancellationToken);

    public Task<PagedResponse<BillDto>> SearchBillsAsync(BillListRequest request, CancellationToken cancellationToken = default) =>
        QueryBillsAsync(request, cancellationToken);

    public async Task<BillDto> GetBillAsync(int id, CancellationToken cancellationToken = default)
    {
        var bill = await _db.Bills.AsNoTracking()
            .Include(b => b.Items)
            .Include(b => b.Payments)
            .Include(b => b.Store)
            .Include(b => b.Customer)
            .Include(b => b.SalesPerson)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Bill not found.");
        _currentUser.Access().EnsureStoreAccess(bill.StoreId);
        return Map(bill, _currentUser.IsAdmin);
    }

    public async Task CancelBillAsync(int id, string? reason, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var bill = await _db.Bills.Include(b => b.Items).Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Bill not found.");
        _currentUser.Access().EnsureStoreAccess(bill.StoreId);
        if (bill.Status == BillStatus.Cancelled)
        {
            throw new ConflictAppException("Bill is already cancelled.");
        }

        if (await _db.Returns.AnyAsync(r => r.OriginalBillId == id, cancellationToken))
        {
            throw new BusinessAppException("Cannot cancel a bill that has returns.");
        }

        var settings = await _db.BusinessSettings.FirstAsync(cancellationToken);
        foreach (var item in bill.Items)
        {
            await _stock.ChangeAsync(bill.StoreId, item.ProductId, item.Quantity, StockMovementType.Return, bill.Id, bill.BillNumber, reason ?? "Bill cancelled", true, _currentUser.UserId, cancellationToken);
        }

        if (bill.CustomerId.HasValue)
        {
            var customer = await _db.Customers.FirstAsync(c => c.Id == bill.CustomerId, cancellationToken);
            await AddLedgerAsync(customer, bill.StoreId, bill.Id, bill.BillNumber, 0, bill.GrandTotal, LedgerTransactionType.Return, $"Cancelled {bill.BillNumber}", cancellationToken);
            if (bill.PaidAmount > 0)
            {
                await AddLedgerAsync(customer, bill.StoreId, bill.Id, bill.BillNumber, bill.PaidAmount, 0, LedgerTransactionType.Credit, $"Reverse payment {bill.BillNumber}", cancellationToken);
            }

            if (bill.WalletRedeemed > 0)
            {
                await _db.Customers.Where(c => c.Id == customer.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.WalletBalance, c => c.WalletBalance + bill.WalletRedeemed), cancellationToken);
            }
        }

        bill.Status = BillStatus.Cancelled;
        bill.UpdatedDate = DateTime.UtcNow;
        bill.Notes = string.IsNullOrWhiteSpace(reason) ? bill.Notes : $"{bill.Notes} | Cancel: {reason}";
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.BillCancelled, nameof(Bill), bill.Id.ToString(), null, new { reason }, bill.StoreId, cancellationToken);
    }

    public async Task<InvoiceDto> GetInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var bill = await GetBillAsync(id, cancellationToken);
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var store = await _db.Stores.AsNoTracking().FirstAsync(s => s.Id == bill.StoreId, cancellationToken);
        var customer = bill.CustomerId.HasValue
            ? await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == bill.CustomerId, cancellationToken)
            : null;
        return new InvoiceDto
        {
            ShopName = settings.ShopName,
            LogoPath = settings.LogoPath,
            BusinessAddress = settings.Address,
            BusinessMobile = settings.Mobile,
            BusinessEmail = settings.Email,
            GSTNumber = settings.GSTNumber,
            StoreName = store.StoreName,
            StoreAddress = store.Address,
            StoreContact = store.ContactNumber,
            StoreGST = store.GSTNumber,
            InvoiceNumber = bill.BillNumber,
            InvoiceDate = bill.BillDate,
            CustomerName = customer?.Name,
            CustomerMobile = customer?.MobileNumber,
            CustomerAddress = customer?.Address,
            Products = bill.Items,
            Subtotal = bill.Subtotal,
            Discount = bill.ItemDiscountTotal + bill.BillDiscount,
            Tax = bill.TaxAmount,
            Total = bill.GrandTotal,
            Payments = bill.Payments,
            AmountPaid = bill.PaidAmount,
            AmountDue = bill.DueAmount,
            Footer = settings.InvoiceFooter,
            ReturnPolicy = settings.ReturnPolicy
        };
    }

    public async Task<HeldBillDto> HoldBillAsync(HeldBillRequest request, CancellationToken cancellationToken = default)
    {
        var storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        var held = new HeldBill
        {
            StoreId = storeId,
            CustomerId = request.CustomerId,
            SalesPersonId = _currentUser.UserId,
            HoldReference = $"HOLD-{DateTime.UtcNow:yyyyMMddHHmmss}-{storeId}",
            Notes = request.Notes,
            BillDiscount = request.BillDiscount,
            ItemsJson = JsonSerializer.Serialize(request.Items),
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.HeldBills.Add(held);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.HeldBillCreated, nameof(HeldBill), held.Id.ToString(), null, held.HoldReference, storeId, cancellationToken);
        return MapHeld(held);
    }

    public async Task<IReadOnlyList<HeldBillDto>> GetHeldBillsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.HeldBills.AsNoTracking().Where(h => !h.IsDeleted);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(h => ids.Contains(h.StoreId));
        }

        if (storeId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(storeId.Value);
            query = query.Where(h => h.StoreId == storeId.Value);
        }

        var items = await query.OrderByDescending(h => h.CreatedDate).ToListAsync(cancellationToken);
        return items.Select(MapHeld).ToList();
    }

    public async Task<HeldBillDto> GetHeldBillAsync(int id, CancellationToken cancellationToken = default)
    {
        var held = await _db.HeldBills.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Held bill not found.");
        _currentUser.Access().EnsureStoreAccess(held.StoreId);
        return MapHeld(held);
    }

    public async Task<HeldBillDto> ResumeHeldBillAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await GetHeldBillAsync(id, cancellationToken);
        await _audit.LogAsync(AuditActions.HeldBillResumed, nameof(HeldBill), id.ToString(), null, dto.HoldReference, dto.StoreId, cancellationToken);
        return dto;
    }

    public async Task DeleteHeldBillAsync(int id, CancellationToken cancellationToken = default)
    {
        var held = await _db.HeldBills.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Held bill not found.");
        _currentUser.Access().EnsureStoreAccess(held.StoreId);
        held.IsDeleted = true;
        held.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<PagedResponse<BillDto>> QueryBillsAsync(BillListRequest request, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.Bills.AsNoTracking().AsQueryable();
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(b => ids.Contains(b.StoreId));
        }

        if (request.StoreId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(request.StoreId.Value);
            query = query.Where(b => b.StoreId == request.StoreId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == request.CustomerId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(b => b.BillNumber.Contains(s) || (b.Customer != null && (b.Customer.Name.Contains(s) || b.Customer.MobileNumber.Contains(s))));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(b => b.BillDate >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(b => b.BillDate < to);
        }

        var projected = query.OrderByDescending(b => b.BillDate).Select(b => new BillDto
        {
            Id = b.Id,
            StoreId = b.StoreId,
            StoreCode = b.Store.StoreCode,
            StoreName = b.Store.StoreName,
            CustomerId = b.CustomerId,
            CustomerName = b.Customer != null ? b.Customer.Name : null,
            CustomerMobile = b.Customer != null ? b.Customer.MobileNumber : null,
            SalesPersonId = b.SalesPersonId,
            SalesPersonName = b.SalesPerson.FullName,
            BillNumber = b.BillNumber,
            BillDate = b.BillDate,
            BillType = b.BillType,
            Status = b.Status,
            Subtotal = b.Subtotal,
            ItemDiscountTotal = b.ItemDiscountTotal,
            BillDiscount = b.BillDiscount,
            TaxAmount = b.TaxAmount,
            GrandTotal = b.GrandTotal,
            PaidAmount = b.PaidAmount,
            DueAmount = b.DueAmount,
            WalletRedeemed = b.WalletRedeemed,
            Notes = b.Notes
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    private async Task AddLedgerAsync(
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
        _db.CustomerLedgers.Add(new CustomerLedger
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
            TransactionDate = DateTime.UtcNow,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });
        customer.OutstandingBalance = balance;
        customer.UpdatedDate = DateTime.UtcNow;
    }

    private async Task ProcessReferralAsync(Customer customer, Bill bill, BusinessSetting settings, CreateBillRequest request, CancellationToken cancellationToken)
    {
        if (!settings.ReferralEnabled)
        {
            return;
        }

        var existing = await _db.Referrals.FirstOrDefaultAsync(r => r.ReferredCustomerId == customer.Id, cancellationToken);
        if (existing is null)
        {
            Customer? referrer = null;
            if (!string.IsNullOrWhiteSpace(request.ReferralCode))
            {
                referrer = await _db.Customers.FirstOrDefaultAsync(c => c.ReferralCode == request.ReferralCode && c.Id != customer.Id, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.ReferringMobileNumber))
            {
                referrer = await _db.Customers.FirstOrDefaultAsync(c => c.MobileNumber == request.ReferringMobileNumber && c.Id != customer.Id, cancellationToken);
            }
            else if (customer.ReferredByCustomerId.HasValue)
            {
                referrer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customer.ReferredByCustomerId, cancellationToken);
            }

            if (referrer is null)
            {
                return;
            }

            existing = new Referral
            {
                StoreId = bill.StoreId,
                ReferrerCustomerId = referrer.Id,
                ReferredCustomerId = customer.Id,
                BillId = bill.Id,
                ReferralDate = DateTime.UtcNow,
                Status = ReferralRewardStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            _db.Referrals.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var priorSales = await _db.Bills.CountAsync(b => b.CustomerId == customer.Id && b.Status != BillStatus.Cancelled && b.Id != bill.Id, cancellationToken);
        if (settings.RewardTrigger == RewardTrigger.FirstPurchase && priorSales > 0)
        {
            return;
        }

        var baseAmount = bill.GrandTotal;
        decimal referrerAmount = settings.RewardType == RewardType.Percentage
            ? Money.Round(baseAmount * settings.ReferrerReward / 100m)
            : settings.ReferrerReward;
        decimal newCustAmount = settings.RewardType == RewardType.Percentage
            ? Money.Round(baseAmount * settings.NewCustomerReward / 100m)
            : settings.NewCustomerReward;

        existing.BillId = bill.Id;
        existing.RewardAmount = Money.Round(referrerAmount + newCustAmount);
        existing.Status = ReferralRewardStatus.Credited;

        await CreditWalletAsync(existing.ReferrerCustomerId, bill.StoreId, referrerAmount, bill, true, existing.Id, cancellationToken);
        await CreditWalletAsync(customer.Id, bill.StoreId, newCustAmount, bill, false, existing.Id, cancellationToken);
        await _audit.LogAsync(AuditActions.ReferralReward, nameof(Referral), existing.Id.ToString(), null, new { referrerAmount, newCustAmount }, bill.StoreId, cancellationToken);
    }

    private async Task CreditWalletAsync(int customerId, int storeId, decimal amount, Bill bill, bool isReferrer, int referralId, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return;
        }

        await _db.Customers.Where(c => c.Id == customerId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.WalletBalance, c => c.WalletBalance + amount), cancellationToken);
        var newBalance = await _db.Customers.AsNoTracking().Where(c => c.Id == customerId).Select(c => c.WalletBalance).SingleAsync(cancellationToken);
        var customer = await _db.Customers.FirstAsync(c => c.Id == customerId, cancellationToken);
        customer.WalletBalance = newBalance;
        _db.WalletTransactions.Add(new WalletTransaction
        {
            CustomerId = customerId,
            StoreId = storeId,
            Amount = amount,
            BalanceAfter = customer.WalletBalance,
            TransactionType = LedgerTransactionType.WalletCredit,
            Description = isReferrer ? $"Referral reward {bill.BillNumber}" : $"Welcome reward {bill.BillNumber}",
            ReferenceId = bill.Id,
            ReferenceNumber = bill.BillNumber,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });
        _db.ReferralRewards.Add(new ReferralReward
        {
            ReferralId = referralId,
            CustomerId = customerId,
            BillId = bill.Id,
            Amount = amount,
            Status = ReferralRewardStatus.Credited,
            IsReferrerReward = isReferrer,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });
        await AddLedgerAsync(customer, storeId, bill.Id, bill.BillNumber, 0, 0, LedgerTransactionType.WalletCredit, $"Wallet credit {bill.BillNumber}", cancellationToken);
        customer.WalletBalance = customer.WalletBalance;
    }

    private static BillDto Map(Bill b, bool includePurchasePrice = false) => new()
    {
        Id = b.Id,
        StoreId = b.StoreId,
        StoreCode = b.Store?.StoreCode ?? string.Empty,
        StoreName = b.Store?.StoreName ?? string.Empty,
        CustomerId = b.CustomerId,
        CustomerName = b.Customer?.Name,
        CustomerMobile = b.Customer?.MobileNumber,
        SalesPersonId = b.SalesPersonId,
        SalesPersonName = b.SalesPerson?.FullName ?? string.Empty,
        BillNumber = b.BillNumber,
        BillDate = b.BillDate,
        BillType = b.BillType,
        Status = b.Status,
        Subtotal = b.Subtotal,
        ItemDiscountTotal = b.ItemDiscountTotal,
        BillDiscount = b.BillDiscount,
        TaxAmount = b.TaxAmount,
        GrandTotal = b.GrandTotal,
        PaidAmount = b.PaidAmount,
        DueAmount = b.DueAmount,
        WalletRedeemed = b.WalletRedeemed,
        Notes = b.Notes,
        Items = b.Items.Select(i => new BillItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            Rate = i.Rate,
            PurchasePrice = includePurchasePrice ? i.PurchasePrice : 0,
            DiscountAmount = i.DiscountAmount,
            TaxPercent = i.TaxPercent,
            TaxAmount = i.TaxAmount,
            Total = i.Total
        }).ToList(),
        Payments = b.Payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            PaymentMode = p.PaymentMode,
            Amount = p.Amount,
            ReferenceNumber = p.ReferenceNumber,
            PaymentDate = p.PaymentDate
        }).ToList()
    };

    private static HeldBillDto MapHeld(HeldBill h) => new()
    {
        Id = h.Id,
        StoreId = h.StoreId,
        CustomerId = h.CustomerId,
        HoldReference = h.HoldReference,
        Notes = h.Notes,
        BillDiscount = h.BillDiscount,
        CreatedDate = h.CreatedDate,
        Items = JsonSerializer.Deserialize<List<CreateBillItemRequest>>(h.ItemsJson) ?? []
    };
}
