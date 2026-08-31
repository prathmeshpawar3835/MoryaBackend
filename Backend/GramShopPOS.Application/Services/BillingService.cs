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
    private readonly IReferralService _referrals;
    private readonly IReturnDocumentService _returns;
    private readonly IBirthdayService _birthdays;
    private readonly IProductUnitService _units;

    public BillingService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IStockEngine stock,
        IDocumentNumberGenerator numbers,
        IAuditService audit,
        IReferralService referrals,
        IReturnDocumentService returns,
        IBirthdayService birthdays,
        IProductUnitService? units = null)
    {
        _db = db;
        _currentUser = currentUser;
        _stock = stock;
        _numbers = numbers;
        _audit = audit;
        _referrals = referrals;
        _returns = returns;
        _birthdays = birthdays;
        _units = units ?? new ProductUnitService(_db, _currentUser);
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

        var eligible = Money.Round(lineInputs.Sum(l => (l.Qty * l.Rate) - l.Discount));
        var storeDiscount = await ResolveStoreDiscountAsync(request.StoreDiscountId, storeId, eligible, cancellationToken);
        var storeDiscountAmount = storeDiscount.Amount;
        var referralPreview = billType == BillType.Sale && customer is not null
            ? await _referrals.PreviewAsync(customer, request.ReferralCode, request.ReferringMobileNumber, eligible, storeId, cancellationToken)
            : new DTOs.Operations.ReferralPreviewDto { EligibleAmount = eligible };
        if (billType == BillType.Sale && customer is null && (!string.IsNullOrWhiteSpace(request.ReferralCode) || !string.IsNullOrWhiteSpace(request.ReferringMobileNumber)))
        {
            throw new ValidationAppException("Select or create a customer before applying a referral code.");
        }

        var birthday = billType == BillType.Sale && customer is not null
            ? await _birthdays.ResolveForSaleAsync(customer, storeId, request.BirthdayOfferId, eligible, cancellationToken)
            : DTOs.Operations.BirthdayDiscountApplication.None;
        if (billType == BillType.Sale && customer is null && request.BirthdayOfferId.HasValue)
        {
            throw new ValidationAppException("Select or create a customer before applying a birthday offer.");
        }

        var birthdayDiscount = birthday.Amount;
        var combinedDiscount = Money.Round(request.BillDiscount + storeDiscountAmount + referralPreview.NewCustomerDiscount + birthdayDiscount);
        var totals = BillCalculator.CalculateTotals(
            lineInputs.Select(l => (l.Qty, l.Rate, l.Discount, l.Tax)).ToList(),
            combinedDiscount);

        var salesPersonId = await StaffResolver.ResolveSalesPersonIdAsync(_db, _currentUser, storeId, request.SalesPersonId, cancellationToken);

        var adjustments = request.Adjustments ?? [];
        if (adjustments.Count > 0 && customer is null)
        {
            throw new ValidationAppException("A customer is required for return, exchange, or buyback during billing.");
        }

        var createdAdjustments = new List<ReturnDto>();
        if (billType == BillType.Sale)
        {
            foreach (var adj in adjustments)
            {
                if (adj.Kind is not (ReturnKind.Return or ReturnKind.Exchange or ReturnKind.Buyback))
                {
                    throw new ValidationAppException("Choose Return, Exchange, or Buyback as the adjustment type.");
                }

                var original = await _db.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == adj.OriginalBillId, cancellationToken)
                    ?? throw new NotFoundAppException("Original invoice for this return, exchange, or buyback was not found.");
                if (original.CustomerId != customer!.Id)
                {
                    throw new ValidationAppException("The original invoice belongs to a different customer. Select the matching customer before applying a return, exchange, or buyback.");
                }

                createdAdjustments.Add(await _returns.CreateCoreAsync(
                    new CreateReturnRequest
                    {
                        OriginalBillId = adj.OriginalBillId,
                        Reason = adj.Reason,
                        SalesPersonId = salesPersonId,
                        Amount = adj.Amount,
                        Items = adj.Items
                    },
                    adj.Kind,
                    new ReturnCreateOptions { PostLedger = false, AmountOverride = adj.Amount },
                    cancellationToken));
            }
        }

        var returnAdj = Money.Round(createdAdjustments.Where(a => a.ReturnKind == ReturnKind.Return).Sum(a => a.ReturnAmount));
        var exchangeAdj = Money.Round(createdAdjustments.Where(a => a.ReturnKind == ReturnKind.Exchange).Sum(a => a.ReturnAmount));
        var buybackAdj = Money.Round(createdAdjustments.Where(a => a.ReturnKind == ReturnKind.Buyback).Sum(a => a.ReturnAmount));
        var totalAdj = Money.Round(returnAdj + exchangeAdj + buybackAdj);
        var appliedAdj = Money.Round(Math.Min(totalAdj, totals.GrandTotal));
        var creditGenerated = Money.Round(Math.Max(0, totalAdj - totals.GrandTotal));
        var payableBeforeWallet = Money.Round(Math.Max(0, totals.GrandTotal - totalAdj));
        var payable = Money.Round(Math.Max(0, payableBeforeWallet - request.WalletRedeemAmount));

        var creditPayment = request.Payments.Where(p => p.PaymentMode == PaymentMode.Credit).Sum(p => p.Amount);
        var collected = request.Payments.Where(p => p.PaymentMode != PaymentMode.Credit).ToList();
        if (collected.Any(p => p.Amount < 0) || request.WalletRedeemAmount < 0 || creditPayment < 0)
        {
            throw new ValidationAppException("Payment amounts cannot be negative.");
        }

        try
        {
            BillCalculator.ValidatePayments(payableBeforeWallet, request.WalletRedeemAmount, collected.Select(p => p.Amount).ToList(), creditPayment);
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

            if (customer.WalletBalance < request.WalletRedeemAmount)
            {
                throw new ValidationAppException(CreditUsage.OveruseMessage(customer.WalletBalance, request.WalletRedeemAmount));
            }

            customer.WalletBalance = Money.Round(customer.WalletBalance - request.WalletRedeemAmount);
            customer.UpdatedDate = DateTime.UtcNow;
        }

        if (creditPayment > 0 && customer is null)
        {
            throw new ValidationAppException("A customer is required for credit/udhaar.");
        }

        var prefix = string.IsNullOrWhiteSpace(store.InvoicePrefix) ? store.StoreCode : store.InvoicePrefix;
        var billNumber = await _numbers.NextBillNumberAsync(storeId, prefix, settings.FinancialYearStartMonth, cancellationToken);

        var paid = Money.Round(collected.Sum(p => p.Amount) + request.WalletRedeemAmount + appliedAdj);
        var due = Money.Round(totals.GrandTotal - paid);
        if (due < 0)
        {
            due = 0;
        }

        var status = due <= 0 ? BillStatus.Completed : (collected.Sum(p => p.Amount) + request.WalletRedeemAmount + appliedAdj == 0 ? BillStatus.Credit : BillStatus.PartiallyPaid);

        var bill = new Bill
        {
            StoreId = storeId,
            CustomerId = customer?.Id,
            SalesPersonId = salesPersonId,
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
            ReferralDiscount = referralPreview.NewCustomerDiscount,
            ReferralDiscountPercent = referralPreview.Applies && settings.RewardType == RewardType.Percentage
                ? settings.NewCustomerReward
                : 0,
            ReferrerCustomerId = referralPreview.Referrer?.ReferrerCustomerId,
            ReferrerName = referralPreview.Referrer?.ReferrerName,
            ReferrerCode = referralPreview.Referrer?.ReferrerCode,
            ReferrerBenefitPercent = referralPreview.Applies && settings.RewardType == RewardType.Percentage
                ? settings.ReferrerReward
                : 0,
            ReferrerBenefitAmount = referralPreview.ReferrerBenefit,
            StoreDiscountAmount = storeDiscountAmount,
            StoreDiscountPercent = storeDiscount.Percent,
            StoreDiscountName = storeDiscount.Name,
            StoreDiscountId = request.StoreDiscountId,
            BirthdayDiscount = birthdayDiscount,
            BirthdayDiscountPercent = birthday.Percent,
            BirthdayOfferId = birthday.OfferId,
            BirthdayOfferName = birthday.Name,
            ReturnAdjustment = returnAdj,
            ExchangeAdjustment = exchangeAdj,
            BuybackAdjustment = buybackAdj,
            CreditGenerated = creditGenerated,
            PayableAmount = payable,
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
            var billItem = new BillItem
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
            };
            _db.BillItems.Add(billItem);
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
            await _units.MarkSoldAsync(storeId, src.Product.Id, calc.Quantity, billItem.Id, src.Request.ProductUnitIds, cancellationToken);
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
            foreach (var adj in createdAdjustments)
            {
                var type = adj.ReturnKind switch
                {
                    ReturnKind.Exchange => LedgerTransactionType.ExchangeAdjustment,
                    ReturnKind.Buyback => LedgerTransactionType.Buyback,
                    _ => LedgerTransactionType.Return
                };
                await AddLedgerAsync(customer, storeId, adj.Id, adj.ReturnNumber, 0, adj.ReturnAmount, type, $"{adj.ReturnKind} {adj.ReturnNumber} applied to {billNumber}", cancellationToken);
            }

            var collectedTotal = Money.Round(collected.Sum(p => p.Amount));
            if (totalAdj == 0)
            {
                if (paid > 0)
                {
                    await AddLedgerAsync(customer, storeId, bill.Id, billNumber, 0, paid, LedgerTransactionType.PaymentReceived, $"Payment against {billNumber}", cancellationToken);
                }
            }
            else if (collectedTotal > 0)
            {
                await AddLedgerAsync(customer, storeId, bill.Id, billNumber, 0, collectedTotal, LedgerTransactionType.PaymentReceived, $"Payment against {billNumber}", cancellationToken);
            }

            if (request.WalletRedeemAmount > 0)
            {
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    CustomerId = customer.Id,
                    StoreId = storeId,
                    Amount = -request.WalletRedeemAmount,
                    BalanceAfter = customer.WalletBalance,
                    TransactionType = LedgerTransactionType.WalletRedeem,
                    Description = $"Redeemed on {billNumber}",
                    ReferenceId = bill.Id,
                    ReferenceNumber = billNumber,
                    UserId = _currentUser.UserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                });
                await AddLedgerAsync(customer, storeId, bill.Id, billNumber, 0, request.WalletRedeemAmount, LedgerTransactionType.WalletRedeem, $"Credit used in sale {billNumber}", cancellationToken);
            }

            if (creditGenerated > 0)
            {
                customer.WalletBalance = Money.Round(customer.WalletBalance + creditGenerated);
                customer.UpdatedDate = DateTime.UtcNow;
                _db.WalletTransactions.Add(new WalletTransaction
                {
                    CustomerId = customer.Id,
                    StoreId = storeId,
                    Amount = creditGenerated,
                    BalanceAfter = customer.WalletBalance,
                    TransactionType = LedgerTransactionType.WalletCredit,
                    Description = $"Credit generated from exchange/return/buyback on {billNumber}",
                    ReferenceId = bill.Id,
                    ReferenceNumber = billNumber,
                    UserId = _currentUser.UserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                });
            }

            if (billType == BillType.Sale)
            {
                await _referrals.ProcessSaleAsync(customer, bill, request, eligible, referralPreview.NewCustomerDiscount, cancellationToken);
                await _birthdays.RecordRedemptionAsync(customer, bill, birthday, salesPersonId, cancellationToken);
            }
        }

        foreach (var adj in createdAdjustments)
        {
            var entity = await _db.Returns.FirstAsync(r => r.Id == adj.Id, cancellationToken);
            entity.AppliedToBillId = bill.Id;
            if (adj.ReturnKind == ReturnKind.Exchange)
            {
                entity.ExchangeBillId = bill.Id;
            }
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
            .Include(b => b.StoreDiscount)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Bill not found.");
        _currentUser.Access().EnsureStoreAccess(bill.StoreId);
        var dto = Map(bill, _currentUser.IsAdmin);
        var linked = await _db.Returns.AsNoTracking().Include(r => r.Items)
            .Include(r => r.SalesPerson)
            .Include(r => r.AppliedToBill)
            .Where(r => r.AppliedToBillId == id || (r.ExchangeBillId == id && r.ReturnKind == ReturnKind.Exchange))
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
        dto.Adjustments = linked.Select(ReturnDocumentService.Map).ToList();
        await ApplyItemFulfillmentAsync(dto, cancellationToken);
        return dto;
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

        await _units.RestoreForBillAsync(bill.Id, cancellationToken);

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
                customer.WalletBalance = Money.Round(customer.WalletBalance + bill.WalletRedeemed);
                customer.UpdatedDate = DateTime.UtcNow;
            }
        }

        bill.Status = BillStatus.Cancelled;
        bill.UpdatedDate = DateTime.UtcNow;
        bill.Notes = string.IsNullOrWhiteSpace(reason) ? bill.Notes : $"{bill.Notes} | Cancel: {reason}";
        await _birthdays.ReleaseRedemptionForCancelledBillAsync(bill.Id, cancellationToken);
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
        var discountLines = InvoiceDiscountComposer.Build(
            bill.ItemDiscountTotal,
            bill.BillDiscount,
            bill.ReferralDiscount,
            bill.ReferralDiscountPercent,
            bill.StoreDiscountAmount,
            bill.StoreDiscountPercent,
            bill.StoreDiscountName,
            bill.BirthdayDiscount,
            bill.BirthdayDiscountPercent,
            bill.BirthdayOfferName);
        var totalDiscount = InvoiceDiscountComposer.Total(discountLines);
        var otherDiscount = InvoiceDiscountComposer.OtherAmount(
            bill.BillDiscount,
            bill.ReferralDiscount,
            bill.StoreDiscountAmount,
            bill.BirthdayDiscount);
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
            CustomerCode = customer?.CustomerCode,
            CustomerReferralCode = customer?.ReferralCode,
            CustomerDateOfBirth = customer?.DateOfBirth,
            SalesPersonName = bill.SalesPersonName,
            Products = bill.Items,
            Subtotal = bill.Subtotal,
            ItemDiscount = bill.ItemDiscountTotal,
            OtherDiscount = otherDiscount,
            Discount = totalDiscount,
            ReferralDiscount = bill.ReferralDiscount,
            ReferralDiscountPercent = bill.ReferralDiscountPercent,
            BirthdayDiscount = bill.BirthdayDiscount,
            BirthdayDiscountPercent = bill.BirthdayDiscountPercent,
            BirthdayOfferName = bill.BirthdayOfferName,
            StoreDiscount = bill.StoreDiscountAmount,
            StoreDiscountPercent = bill.StoreDiscountPercent,
            StoreDiscountName = bill.StoreDiscountName,
            TotalDiscount = totalDiscount,
            DiscountLines = discountLines,
            HasReferral = bill.ReferralDiscount > 0 || !string.IsNullOrWhiteSpace(bill.ReferrerCode),
            ReferrerName = bill.ReferrerName,
            ReferrerCode = bill.ReferrerCode,
            ReferrerBenefitPercent = bill.ReferrerBenefitPercent,
            ReferrerBenefitAmount = bill.ReferrerBenefitAmount,
            Tax = bill.TaxAmount,
            Total = bill.GrandTotal,
            ReturnAdjustment = bill.ReturnAdjustment,
            ExchangeAdjustment = bill.ExchangeAdjustment,
            BuybackAdjustment = bill.BuybackAdjustment,
            WalletRedeemed = bill.WalletRedeemed,
            CreditGenerated = bill.CreditGenerated,
            PayableAmount = bill.ReturnAdjustment + bill.ExchangeAdjustment + bill.BuybackAdjustment > 0
                || bill.WalletRedeemed > 0
                || bill.CreditGenerated > 0
                || bill.PayableAmount > 0
                ? bill.PayableAmount
                : bill.GrandTotal,
            Payments = bill.Payments,
            Adjustments = bill.Adjustments,
            AmountPaid = bill.PaidAmount,
            AmountDue = bill.DueAmount,
            Footer = settings.InvoiceFooter,
            ReturnPolicy = settings.ReturnPolicy
        };
    }

    public async Task<WhatsAppShareDto> GetWhatsAppShareAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetInvoiceAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(invoice.CustomerMobile))
        {
            return new WhatsAppShareDto
            {
                Sent = false,
                InvoiceNumber = invoice.InvoiceNumber,
                Error = "Customer mobile number is not available.",
                Message = string.Empty,
                ShareUrl = string.Empty
            };
        }

        var digits = new string(invoice.CustomerMobile.Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
        {
            digits = "91" + digits;
        }

        var hasAdj = invoice.ReturnAdjustment + invoice.ExchangeAdjustment + invoice.BuybackAdjustment > 0;
        var extra = hasAdj ? " Your Exchange/Return/Buyback adjustment has been included in the invoice." : string.Empty;
        var referralLine = string.IsNullOrWhiteSpace(invoice.CustomerReferralCode)
            ? string.Empty
            : $"\nYour unique referral code: *{invoice.CustomerReferralCode}*";
        var message =
            $"Hello {invoice.CustomerName ?? "Customer"},\n\nThank you for shopping with {invoice.ShopName}.\n\nYour invoice *{invoice.InvoiceNumber}* has been generated successfully.{extra}{referralLine}\n\nPlease find your invoice details with the store. Final payable: ₹{invoice.PayableAmount:0.00}.\n\nThank you for choosing us.";
        var shareUrl = $"https://wa.me/{digits}?text={Uri.EscapeDataString(message)}";
        return new WhatsAppShareDto
        {
            Sent = false,
            Phone = digits,
            Message = message,
            ShareUrl = shareUrl,
            InvoiceNumber = invoice.InvoiceNumber
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
            CustomerCode = b.Customer != null ? b.Customer.CustomerCode : null,
            CustomerReferralCode = b.Customer != null ? b.Customer.ReferralCode : null,
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
            ReferralDiscount = b.ReferralDiscount,
            ReferralDiscountPercent = b.ReferralDiscountPercent,
            ReferrerName = b.ReferrerName,
            ReferrerCode = b.ReferrerCode,
            StoreDiscountAmount = b.StoreDiscountAmount,
            StoreDiscountPercent = b.StoreDiscountPercent,
            StoreDiscountName = b.StoreDiscountName,
            Notes = b.Notes,
            BirthdayDiscount = b.BirthdayDiscount,
            BirthdayDiscountPercent = b.BirthdayDiscountPercent,
            BirthdayOfferId = b.BirthdayOfferId,
            BirthdayOfferName = b.BirthdayOfferName,
            ReturnAdjustment = b.ReturnAdjustment,
            ExchangeAdjustment = b.ExchangeAdjustment,
            BuybackAdjustment = b.BuybackAdjustment,
            CreditGenerated = b.CreditGenerated,
            PayableAmount = b.PayableAmount
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

    private sealed record AppliedStoreDiscount(decimal Amount, string? Name, decimal Percent);

    private async Task<AppliedStoreDiscount> ResolveStoreDiscountAsync(int? discountId, int storeId, decimal eligible, CancellationToken cancellationToken)
    {
        if (!discountId.HasValue)
        {
            return new AppliedStoreDiscount(0, null, 0);
        }

        var discount = await _db.StoreDiscounts.FirstOrDefaultAsync(d => d.Id == discountId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Discount not found.");
        if (discount.OfferCategory == OfferCategory.Birthday)
        {
            throw new ValidationAppException("Select birthday offers from the birthday offer control, not as a store discount.");
        }

        if (!discount.IsActive || discount.StoreId != storeId)
        {
            throw new BusinessAppException("The selected discount is not active for this store.");
        }

        var today = DateTime.UtcNow.Date;
        if (discount.ValidFrom.HasValue && today < discount.ValidFrom.Value.Date)
        {
            throw new BusinessAppException("The selected discount is not yet valid.");
        }

        if (discount.ValidTo.HasValue && today > discount.ValidTo.Value.Date)
        {
            throw new BusinessAppException("The selected discount has expired.");
        }

        var amount = discount.DiscountKind == DiscountKind.Percentage
            ? ReferralCalculator.ComputeBenefit(eligible, discount.Value, RewardType.Percentage)
            : ReferralCalculator.ComputeBenefit(eligible, discount.Value, RewardType.FixedAmount);
        return new AppliedStoreDiscount(
            amount,
            discount.Name,
            discount.DiscountKind == DiscountKind.Percentage ? discount.Value : 0);
    }

    private async Task ApplyItemFulfillmentAsync(BillDto dto, CancellationToken cancellationToken)
    {
        if (dto.Items.Count == 0)
        {
            return;
        }

        var ids = dto.Items.Select(i => i.Id).ToList();
        var rows = await _db.ReturnItems.AsNoTracking()
            .Where(ri => ids.Contains(ri.OriginalBillItemId))
            .Select(ri => new { ri.OriginalBillItemId, ri.Quantity, ri.ProductReturn.ReturnKind })
            .ToListAsync(cancellationToken);
        foreach (var item in dto.Items)
        {
            var returned = rows.Where(r => r.OriginalBillItemId == item.Id && r.ReturnKind == ReturnKind.Return).Sum(r => r.Quantity);
            var exchanged = rows.Where(r => r.OriginalBillItemId == item.Id && r.ReturnKind == ReturnKind.Exchange).Sum(r => r.Quantity);
            var boughtBack = rows.Where(r => r.OriginalBillItemId == item.Id && r.ReturnKind == ReturnKind.Buyback).Sum(r => r.Quantity);
            item.ReturnedQuantity = returned;
            item.ExchangedQuantity = exchanged;
            item.BoughtBackQuantity = boughtBack;
            item.RemainingQuantity = Math.Max(0, item.Quantity - returned - exchanged - boughtBack);
            item.FulfillmentStatus = exchanged >= item.Quantity && item.Quantity > 0
                ? BillItemFulfillmentStatus.Exchanged
                : returned >= item.Quantity && item.Quantity > 0
                    ? BillItemFulfillmentStatus.Returned
                    : boughtBack >= item.Quantity && item.Quantity > 0
                        ? BillItemFulfillmentStatus.BoughtBack
                        : exchanged > 0
                            ? BillItemFulfillmentStatus.PartiallyExchanged
                            : returned > 0
                                ? BillItemFulfillmentStatus.PartiallyReturned
                                : boughtBack > 0
                                    ? BillItemFulfillmentStatus.PartiallyBoughtBack
                                    : BillItemFulfillmentStatus.Sold;
        }
    }

    private BillDto Map(Bill b, bool includePurchasePrice = false) => new()
    {
        Id = b.Id,
        StoreId = b.StoreId,
        StoreCode = b.Store?.StoreCode ?? string.Empty,
        StoreName = b.Store?.StoreName ?? string.Empty,
        CustomerId = b.CustomerId,
        CustomerName = b.Customer?.Name,
        CustomerMobile = b.Customer?.MobileNumber,
        CustomerCode = b.Customer?.CustomerCode,
        CustomerReferralCode = b.Customer?.ReferralCode,
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
        ReferralDiscount = b.ReferralDiscount,
        ReferralDiscountPercent = b.ReferralDiscountPercent,
        ReferrerCustomerId = b.ReferrerCustomerId,
        ReferrerName = b.ReferrerName,
        ReferrerCode = b.ReferrerCode,
        ReferrerBenefitPercent = b.ReferrerBenefitPercent,
        ReferrerBenefitAmount = b.ReferrerBenefitAmount,
        StoreDiscountAmount = b.StoreDiscountAmount,
        StoreDiscountPercent = b.StoreDiscountPercent,
        StoreDiscountId = b.StoreDiscountId,
        StoreDiscountName = b.StoreDiscountName ?? b.StoreDiscount?.Name,
        BirthdayDiscount = b.BirthdayDiscount,
        BirthdayDiscountPercent = b.BirthdayDiscountPercent,
        BirthdayOfferId = b.BirthdayOfferId,
        BirthdayOfferName = b.BirthdayOfferName,
        ReturnAdjustment = b.ReturnAdjustment,
        ExchangeAdjustment = b.ExchangeAdjustment,
        BuybackAdjustment = b.BuybackAdjustment,
        CreditGenerated = b.CreditGenerated,
        PayableAmount = b.PayableAmount,
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
            Total = i.Total,
            RemainingQuantity = i.Quantity,
            FulfillmentStatus = BillItemFulfillmentStatus.Sold
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
