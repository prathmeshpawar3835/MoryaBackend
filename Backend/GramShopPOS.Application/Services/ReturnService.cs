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
    private readonly IWhatsAppService? _whatsApp;
    private readonly IPdfService? _pdf;

    public ReturnService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IBillingService billing,
        IAuditService audit,
        IReturnDocumentService documents,
        IWhatsAppService? whatsApp = null,
        IPdfService? pdf = null)
    {
        _db = db;
        _currentUser = currentUser;
        _billing = billing;
        _audit = audit;
        _documents = documents;
        _whatsApp = whatsApp;
        _pdf = pdf;
    }

    public async Task<ReturnDto> CreateReturnAsync(CreateReturnRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var result = await _documents.CreateCoreAsync(request, ReturnKind.Return, new ReturnCreateOptions { AmountOverride = request.Amount }, cancellationToken);
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
            Amount = request.Amount,
            Items = request.ReturnItems
        }, ReturnKind.Exchange, new ReturnCreateOptions { AmountOverride = request.Amount }, cancellationToken);

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
            Amount = request.Amount,
            Items = request.Items
        }, ReturnKind.Buyback, new ReturnCreateOptions { PostLedger = true, AmountOverride = request.Amount }, cancellationToken);
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
            CustomerName = r.Customer != null ? r.Customer.Name : null,
            CustomerCode = r.Customer != null ? r.Customer.CustomerCode : null,
            CustomerMobile = r.Customer != null ? r.Customer.MobileNumber : null,
            StoreName = r.Store.StoreName,
            ReturnAmount = r.ReturnAmount,
            GrossAmount = r.GrossAmount,
            DeductionPercent = r.DeductionPercent,
            DeductionAmount = r.DeductionAmount,
            Reason = r.Reason,
            ReturnKind = r.ReturnKind,
            ExchangeBillId = r.ExchangeBillId,
            ExchangeBillNumber = r.ExchangeBill != null ? r.ExchangeBill.BillNumber : null,
            SalesPersonId = r.SalesPersonId,
            SalesPersonName = r.SalesPerson != null ? r.SalesPerson.FullName : null
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<ReturnDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Returns.AsNoTracking()
            .Include(r => r.Items)
            .Include(r => r.Customer)
            .Include(r => r.Store)
            .Include(r => r.SalesPerson)
            .Include(r => r.AppliedToBill)
            .Include(r => r.ExchangeBill)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Return not found.");
        _currentUser.Access().EnsureStoreAccess(entity.StoreId);
        return Map(entity);
    }

    public async Task<WhatsAppShareDto> SendWhatsAppAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        var kind = entity.ReturnKind switch
        {
            ReturnKind.Exchange => "exchange",
            ReturnKind.Buyback => "buyback",
            _ => "return"
        };
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var shop = string.IsNullOrWhiteSpace(settings.ShopName) ? entity.StoreName : settings.ShopName;
        var linked = entity.ExchangeBillNumber is { Length: > 0 }
            ? $"\nLinked invoice: *{entity.ExchangeBillNumber}*."
            : string.Empty;
        var message =
            $"Hello {entity.CustomerName ?? "Customer"},\n\nYour {kind} receipt *{entity.ReturnNumber}* is attached as a PDF.\n\nAmount: ₹{entity.ReturnAmount:0.00}\nOriginal invoice: *{entity.OriginalBillNumber}*.{linked}\n\nThank you for choosing {shop}.";
        var share = WhatsAppDelivery.Preview(entity.CustomerMobile, message, entity.ReturnNumber);
        if (string.IsNullOrWhiteSpace(share.Phone) || _pdf is null || _whatsApp is null)
        {
            return share;
        }

        var pdf = await _pdf.ReturnNotePdfAsync(id, cancellationToken);
        var result = await WhatsAppDelivery.SendPdfAsync(_whatsApp, share, pdf.Content, pdf.FileName, cancellationToken);
        if (result.DocumentAttached && entity.ExchangeBillId is int billId)
        {
            var invoicePdf = await _pdf.InvoicePdfAsync(billId, cancellationToken);
            var invoiceShare = WhatsAppDelivery.Preview(
                entity.CustomerMobile,
                $"Hello {entity.CustomerName ?? "Customer"},\n\nYour exchange invoice *{entity.ExchangeBillNumber}* is attached as a PDF.",
                entity.ExchangeBillNumber ?? invoicePdf.FileName);
            await WhatsAppDelivery.SendPdfAsync(_whatsApp, invoiceShare, invoicePdf.Content, invoicePdf.FileName, cancellationToken);
        }

        if (result.DocumentAttached)
        {
            await _audit.LogAsync(AuditActions.DocumentWhatsAppSent, nameof(ProductReturn), id.ToString(), null, new { entity.ReturnNumber, entity.ReturnKind, result.Phone }, entity.StoreId, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(result.Error))
        {
            await _audit.LogAsync(AuditActions.DocumentWhatsAppFailed, nameof(ProductReturn), id.ToString(), null, new { result.Error }, entity.StoreId, cancellationToken);
        }

        return result;
    }

    private static ReturnDto Map(ProductReturn r) => ReturnDocumentService.Map(r);
}
