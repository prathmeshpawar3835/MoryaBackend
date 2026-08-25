using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class ReturnService : IReturnService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockEngine _stock;
    private readonly IDocumentNumberGenerator _numbers;
    private readonly IBillingService _billing;
    private readonly IAuditService _audit;
    private readonly IReferralService _referrals;

    public ReturnService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IStockEngine stock,
        IDocumentNumberGenerator numbers,
        IBillingService billing,
        IAuditService audit,
        IReferralService referrals)
    {
        _db = db;
        _currentUser = currentUser;
        _stock = stock;
        _numbers = numbers;
        _billing = billing;
        _audit = audit;
        _referrals = referrals;
    }

    public async Task<ReturnDto> CreateReturnAsync(CreateReturnRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var result = await CreateReturnCoreAsync(request, ReturnKind.Return, null, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.ReturnCreated, nameof(ProductReturn), result.Id.ToString(), null, new { result.ReturnNumber, result.ReturnAmount }, result.StoreId, cancellationToken);
        return result;
    }

    public async Task<ExchangeDto> CreateExchangeAsync(CreateExchangeRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var ret = await CreateReturnCoreAsync(new CreateReturnRequest
        {
            OriginalBillId = request.OriginalBillId,
            Reason = request.Reason,
            SalesPersonId = request.SalesPersonId,
            Items = request.ReturnItems
        }, ReturnKind.Exchange, null, cancellationToken);

        var original = await _db.Bills.FirstAsync(b => b.Id == request.OriginalBillId, cancellationToken);
        var salesPersonId = await StaffResolver.ResolveSalesPersonIdAsync(_db, _currentUser, original.StoreId, request.SalesPersonId, cancellationToken);
        var billing = (BillingService)_billing;
        var newBill = await billing.CreateBillCoreAsync(new CreateBillRequest
        {
            StoreId = original.StoreId,
            CustomerId = original.CustomerId,
            BillDiscount = request.BillDiscount,
            WalletRedeemAmount = request.WalletRedeemAmount,
            SalesPersonId = salesPersonId,
            Items = request.NewItems,
            Payments = request.Payments
        }, original.StoreId, BillType.Exchange, original.Id, cancellationToken);

        var entity = await _db.Returns.FirstAsync(r => r.Id == ret.Id, cancellationToken);
        entity.ExchangeBillId = newBill.Id;
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.ExchangeCreated, nameof(ProductReturn), ret.Id.ToString(), null, new { ret.ReturnNumber, newBill.BillNumber }, original.StoreId, cancellationToken);

        return new ExchangeDto
        {
            Return = await GetByIdAsync(ret.Id, cancellationToken),
            NewBill = newBill,
            DifferencePayable = Money.Round(newBill.GrandTotal - ret.ReturnAmount)
        };
    }

    public async Task<PagedResponse<ReturnDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.Returns.AsNoTracking().AsQueryable();
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
            query = query.Where(r => r.ReturnNumber.Contains(s) || r.OriginalBillNumber.Contains(s));
        }

        var projected = query.OrderByDescending(r => r.ReturnDate).Select(r => new ReturnDto
        {
            Id = r.Id,
            StoreId = r.StoreId,
            OriginalBillId = r.OriginalBillId,
            OriginalBillNumber = r.OriginalBillNumber,
            ReturnNumber = r.ReturnNumber,
            ReturnDate = r.ReturnDate,
            CustomerId = r.CustomerId,
            ReturnAmount = r.ReturnAmount,
            Reason = r.Reason,
            ReturnKind = r.ReturnKind,
            ExchangeBillId = r.ExchangeBillId
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<ReturnDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Returns.AsNoTracking().Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Return not found.");
        _currentUser.Access().EnsureStoreAccess(entity.StoreId);
        return Map(entity);
    }

    private async Task<ReturnDto> CreateReturnCoreAsync(
        CreateReturnRequest request,
        ReturnKind kind,
        int? exchangeBillId,
        CancellationToken cancellationToken)
    {
        var bill = await _db.Bills.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == request.OriginalBillId, cancellationToken)
            ?? throw new NotFoundAppException("Original bill not found.");
        _currentUser.Access().EnsureStoreAccess(bill.StoreId);
        if (bill.Status == BillStatus.Cancelled)
        {
            throw new BusinessAppException("Cannot return a cancelled bill.");
        }

        if (request.Items.Count == 0)
        {
            throw new ValidationAppException("Return must contain items.");
        }

        var settings = await _db.BusinessSettings.FirstAsync(cancellationToken);
        var salesPersonId = await StaffResolver.ResolveSalesPersonIdAsync(_db, _currentUser, bill.StoreId, request.SalesPersonId, cancellationToken);
        var returnNumber = await _numbers.NextReturnNumberAsync(bill.StoreId, "CN", settings.FinancialYearStartMonth, cancellationToken);

        var ret = new ProductReturn
        {
            StoreId = bill.StoreId,
            OriginalBillId = bill.Id,
            OriginalBillNumber = bill.BillNumber,
            ReturnNumber = returnNumber,
            ReturnDate = DateTime.UtcNow,
            CustomerId = bill.CustomerId,
            Reason = request.Reason,
            ReturnKind = kind,
            UserId = _currentUser.UserId,
            SalesPersonId = salesPersonId,
            ExchangeBillId = exchangeBillId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.Returns.Add(ret);
        await _db.SaveChangesAsync(cancellationToken);

        decimal amount = 0;
        foreach (var item in request.Items)
        {
            var billItem = bill.Items.FirstOrDefault(i => i.Id == item.OriginalBillItemId)
                ?? throw new ValidationAppException("Return item does not belong to the original bill.");
            if (item.Quantity <= 0 || item.Quantity > billItem.Quantity)
            {
                throw new ValidationAppException("Invalid return quantity.");
            }

            var alreadyReturned = await _db.ReturnItems
                .Where(ri => ri.OriginalBillItemId == billItem.Id)
                .SumAsync(ri => (decimal?)ri.Quantity, cancellationToken) ?? 0;
            if (alreadyReturned + item.Quantity > billItem.Quantity)
            {
                throw new BusinessAppException($"Quantity exceeds remaining returnable quantity for {billItem.ProductCode}.");
            }

            var lineTotal = Money.Round(billItem.Total * (item.Quantity / billItem.Quantity));
            amount += lineTotal;
            _db.ReturnItems.Add(new ReturnItem
            {
                ProductReturnId = ret.Id,
                OriginalBillItemId = billItem.Id,
                ProductId = billItem.ProductId,
                ProductCode = billItem.ProductCode,
                ProductName = billItem.ProductName,
                Quantity = item.Quantity,
                Rate = billItem.Rate,
                TaxAmount = Money.Round(billItem.TaxAmount * (item.Quantity / billItem.Quantity)),
                Total = lineTotal,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
            await _stock.ChangeAsync(bill.StoreId, billItem.ProductId, item.Quantity, kind == ReturnKind.Exchange ? StockMovementType.Exchange : StockMovementType.Return, ret.Id, returnNumber, request.Reason, true, _currentUser.UserId, cancellationToken);
        }

        ret.ReturnAmount = Money.Round(amount);
        if (bill.CustomerId.HasValue)
        {
            var customer = await _db.Customers.FirstAsync(c => c.Id == bill.CustomerId, cancellationToken);
            var latest = await _db.CustomerLedgers.Where(l => l.CustomerId == customer.Id).OrderByDescending(l => l.Id).Select(l => (decimal?)l.Balance).FirstOrDefaultAsync(cancellationToken) ?? 0;
            var balance = Money.Round(latest - ret.ReturnAmount);
            _db.CustomerLedgers.Add(new CustomerLedger
            {
                CustomerId = customer.Id,
                StoreId = bill.StoreId,
                ReferenceId = ret.Id,
                ReferenceNumber = returnNumber,
                Debit = 0,
                Credit = ret.ReturnAmount,
                Balance = balance,
                TransactionType = LedgerTransactionType.Return,
                Description = $"Return {returnNumber} against {bill.BillNumber}",
                TransactionDate = DateTime.UtcNow,
                UserId = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
            customer.OutstandingBalance = balance;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _referrals.AdjustForReturnAsync(bill, ret, cancellationToken);
        return Map(ret);
    }

    private static ReturnDto Map(ProductReturn r) => new()
    {
        Id = r.Id,
        StoreId = r.StoreId,
        OriginalBillId = r.OriginalBillId,
        OriginalBillNumber = r.OriginalBillNumber,
        ReturnNumber = r.ReturnNumber,
        ReturnDate = r.ReturnDate,
        CustomerId = r.CustomerId,
        ReturnAmount = r.ReturnAmount,
        Reason = r.Reason,
        ReturnKind = r.ReturnKind,
        ExchangeBillId = r.ExchangeBillId,
        SalesPersonId = r.SalesPersonId,
        Items = r.Items?.Select(i => new ReturnItemDto
        {
            OriginalBillItemId = i.OriginalBillItemId,
            ProductId = i.ProductId,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            Rate = i.Rate,
            Total = i.Total
        }).ToList() ?? []
    };
}
