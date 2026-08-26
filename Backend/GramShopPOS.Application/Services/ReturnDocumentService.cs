using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class ReturnCreateOptions
{
    public bool PostLedger { get; init; } = true;
    public decimal? AmountOverride { get; init; }
    public int? AppliedToBillId { get; init; }
    public int? ExchangeBillId { get; init; }
}

public interface IReturnDocumentService
{
    Task<ReturnDto> CreateCoreAsync(CreateReturnRequest request, ReturnKind kind, ReturnCreateOptions? options = null, CancellationToken cancellationToken = default);
}

public sealed class ReturnDocumentService : IReturnDocumentService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockEngine _stock;
    private readonly IDocumentNumberGenerator _numbers;
    private readonly IReferralService _referrals;

    public ReturnDocumentService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IStockEngine stock,
        IDocumentNumberGenerator numbers,
        IReferralService referrals)
    {
        _db = db;
        _currentUser = currentUser;
        _stock = stock;
        _numbers = numbers;
        _referrals = referrals;
    }

    public async Task<ReturnDto> CreateCoreAsync(
        CreateReturnRequest request,
        ReturnKind kind,
        ReturnCreateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ReturnCreateOptions();
        var requestedOverride = options.AmountOverride ?? request.Amount;
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
            throw new ValidationAppException("Select at least one product to process this return, exchange, or buyback.");
        }

        var settings = await _db.BusinessSettings.FirstAsync(cancellationToken);
        var deductionPercent = AdjustmentDeduction.PercentFor(kind, settings);
        var salesPersonId = await StaffResolver.ResolveSalesPersonIdAsync(_db, _currentUser, bill.StoreId, request.SalesPersonId, cancellationToken);
        var prefix = kind switch
        {
            ReturnKind.Exchange => "EX",
            ReturnKind.Buyback => "BB",
            _ => "CN"
        };
        var returnNumber = await _numbers.NextReturnNumberAsync(bill.StoreId, prefix, settings.FinancialYearStartMonth, cancellationToken);

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
            ExchangeBillId = options.ExchangeBillId,
            AppliedToBillId = options.AppliedToBillId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.Returns.Add(ret);
        await _db.SaveChangesAsync(cancellationToken);

        decimal amount = 0;
        var pendingLines = new List<(ReturnItem Item, decimal OriginalShare)>();
        foreach (var item in request.Items)
        {
            var billItem = bill.Items.FirstOrDefault(i => i.Id == item.OriginalBillItemId)
                ?? throw new ValidationAppException("One of the selected items does not belong to the original invoice.");
            if (item.Quantity <= 0)
            {
                throw new ValidationAppException($"Enter a quantity greater than zero for {billItem.ProductName}.");
            }

            if (item.Quantity > billItem.Quantity)
            {
                throw new ValidationAppException($"Quantity for {billItem.ProductName} cannot exceed the original billed quantity of {billItem.Quantity}.");
            }

            var alreadyReturned = await _db.ReturnItems
                .Where(ri => ri.OriginalBillItemId == billItem.Id)
                .SumAsync(ri => (decimal?)ri.Quantity, cancellationToken) ?? 0;
            var remaining = billItem.Quantity - alreadyReturned;
            if (alreadyReturned + item.Quantity > billItem.Quantity)
            {
                throw new BusinessAppException($"Only {remaining} of {billItem.ProductName} ({billItem.ProductCode}) can still be returned, exchanged, or bought back.");
            }

            var lineGross = Money.Round(billItem.Total * (item.Quantity / billItem.Quantity));
            var lineNet = AdjustmentDeduction.NetOf(lineGross, deductionPercent);
            amount += lineNet;
            var row = new ReturnItem
            {
                ProductReturnId = ret.Id,
                OriginalBillItemId = billItem.Id,
                ProductId = billItem.ProductId,
                ProductCode = billItem.ProductCode,
                ProductName = billItem.ProductName,
                Quantity = item.Quantity,
                Rate = billItem.Rate,
                TaxAmount = Money.Round(billItem.TaxAmount * (item.Quantity / billItem.Quantity)),
                Total = lineNet,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            _db.ReturnItems.Add(row);
            pendingLines.Add((row, lineGross));
            var movement = kind == ReturnKind.Exchange ? StockMovementType.Exchange : StockMovementType.Return;
            await _stock.ChangeAsync(bill.StoreId, billItem.ProductId, item.Quantity, movement, ret.Id, returnNumber, request.Reason, true, _currentUser.UserId, cancellationToken);
        }

        var grossAmount = Money.Round(pendingLines.Sum(l => l.OriginalShare));
        var calculatedNet = Money.Round(amount);
        var finalAmount = calculatedNet;
        if (requestedOverride.HasValue && _currentUser.IsAdmin)
        {
            if (requestedOverride.Value < 0)
            {
                throw new ValidationAppException("Final amount cannot be negative.");
            }

            finalAmount = Money.Round(requestedOverride.Value);
            if (calculatedNet > 0 && pendingLines.Count > 0 && finalAmount != calculatedNet)
            {
                var ratio = finalAmount / calculatedNet;
                decimal allocated = 0;
                for (var i = 0; i < pendingLines.Count; i++)
                {
                    var row = pendingLines[i].Item;
                    if (i == pendingLines.Count - 1)
                    {
                        row.Total = Money.Round(finalAmount - allocated);
                    }
                    else
                    {
                        row.Total = Money.Round(row.Total * ratio);
                        allocated += row.Total;
                    }
                }
            }
        }
        ret.GrossAmount = grossAmount;
        ret.DeductionPercent = deductionPercent;
        ret.DeductionAmount = Money.Round(Math.Max(0, grossAmount - finalAmount));
        ret.ReturnAmount = finalAmount;
        if (options.PostLedger && bill.CustomerId.HasValue)
        {
            var customer = await _db.Customers.FirstAsync(c => c.Id == bill.CustomerId, cancellationToken);
            var latest = await _db.CustomerLedgers.Where(l => l.CustomerId == customer.Id).OrderByDescending(l => l.Id).Select(l => (decimal?)l.Balance).FirstOrDefaultAsync(cancellationToken) ?? 0;
            var ledgerType = kind switch
            {
                ReturnKind.Exchange => LedgerTransactionType.ExchangeAdjustment,
                ReturnKind.Buyback => LedgerTransactionType.Buyback,
                _ => LedgerTransactionType.Return
            };
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
                TransactionType = ledgerType,
                Description = $"{kind} {returnNumber} against {bill.BillNumber}",
                TransactionDate = DateTime.UtcNow,
                UserId = _currentUser.UserId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
            customer.OutstandingBalance = balance;
            if (kind == ReturnKind.Buyback)
            {
                customer.WalletBalance = Money.Round(customer.WalletBalance + ret.ReturnAmount);
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    CustomerId = customer.Id,
                    StoreId = bill.StoreId,
                    Amount = ret.ReturnAmount,
                    BalanceAfter = customer.WalletBalance,
                    TransactionType = LedgerTransactionType.Buyback,
                    Description = $"Buyback {returnNumber}",
                    ReferenceId = ret.Id,
                    ReferenceNumber = returnNumber,
                    UserId = _currentUser.UserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _referrals.AdjustForReturnAsync(bill, ret, cancellationToken);
        return Map(ret);
    }

    public static ReturnDto Map(ProductReturn r) => new()
    {
        Id = r.Id,
        StoreId = r.StoreId,
        OriginalBillId = r.OriginalBillId,
        OriginalBillNumber = r.OriginalBillNumber,
        ReturnNumber = r.ReturnNumber,
        ReturnDate = r.ReturnDate,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer?.Name,
        CustomerCode = r.Customer?.CustomerCode,
        CustomerMobile = r.Customer?.MobileNumber,
        StoreName = r.Store?.StoreName,
        ReturnAmount = r.ReturnAmount,
        GrossAmount = r.GrossAmount,
        DeductionPercent = r.DeductionPercent,
        DeductionAmount = r.DeductionAmount,
        Reason = r.Reason,
        ReturnKind = r.ReturnKind,
        ExchangeBillId = r.ExchangeBillId,
        SalesPersonId = r.SalesPersonId,
        SalesPersonName = r.SalesPerson?.FullName,
        AppliedToBillId = r.AppliedToBillId,
        AppliedToBillNumber = r.AppliedToBill?.BillNumber,
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
