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
    private readonly IBillingService _billing;
    private readonly IAuditService _audit;
    private readonly IReturnDocumentService _documents;

    public ReturnService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IBillingService billing,
        IAuditService audit,
        IReturnDocumentService documents)
    {
        _db = db;
        _currentUser = currentUser;
        _billing = billing;
        _audit = audit;
        _documents = documents;
    }

    public async Task<ReturnDto> CreateReturnAsync(CreateReturnRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var result = await _documents.CreateCoreAsync(request, ReturnKind.Return, null, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.ReturnCreated, nameof(ProductReturn), result.Id.ToString(), null, new { result.ReturnNumber, result.ReturnAmount }, result.StoreId, cancellationToken);
        return result;
    }

    public async Task<ExchangeDto> CreateExchangeAsync(CreateExchangeRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var ret = await _documents.CreateCoreAsync(new CreateReturnRequest
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

    public async Task<ReturnDto> CreateBuybackAsync(CreateBuybackRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var result = await _documents.CreateCoreAsync(new CreateReturnRequest
        {
            OriginalBillId = request.OriginalBillId,
            Reason = request.Reason,
            SalesPersonId = request.SalesPersonId,
            Items = request.Items
        }, ReturnKind.Buyback, new ReturnCreateOptions { AmountOverride = request.Amount, PostLedger = true }, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.BuybackCreated, nameof(ProductReturn), result.Id.ToString(), null, new { result.ReturnNumber, result.ReturnAmount }, result.StoreId, cancellationToken);
        return result;
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

    private static ReturnDto Map(ProductReturn r) => ReturnDocumentService.Map(r);
}
