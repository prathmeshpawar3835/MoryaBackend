using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Operations;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class BirthdayService : IBirthdayService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IWhatsAppService _whatsApp;
    private readonly IAuditService _audit;

    public BirthdayService(IAppDbContext db, ICurrentUser currentUser, IWhatsAppService whatsApp, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _whatsApp = whatsApp;
        _audit = audit;
    }

    public async Task<BirthdayEligibilityDto> GetEligibilityAsync(int customerId, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);
        var resolvedStoreId = storeId.HasValue
            ? _currentUser.Access().ResolveStoreId(storeId)
            : customer.StoreId;
        _currentUser.Access().EnsureStoreAccess(resolvedStoreId);

        var today = BusinessCalendar.Today();
        var isBirthday = BusinessCalendar.IsBirthdayToday(customer.DateOfBirth);
        var redemption = await _db.BirthdayOfferRedemptions.AsNoTracking()
            .Include(r => r.Bill)
            .Where(r => r.CustomerId == customer.Id && r.BirthdayDate == today && r.Status == BirthdayRedemptionStatus.Redeemed)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var offers = isBirthday && redemption is null
            ? await ActiveOffersQuery(resolvedStoreId, today).Select(d => new BirthdayOfferSummaryDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                DiscountKind = d.DiscountKind,
                Value = d.Value,
                StoreId = d.StoreId,
                StoreName = d.Store.StoreName
            }).ToListAsync(cancellationToken)
            : [];

        string? message = null;
        if (customer.DateOfBirth is null)
        {
            message = null;
        }
        else if (!isBirthday)
        {
            message = null;
        }
        else if (redemption is not null)
        {
            message = "Birthday Offer Already Redeemed Today";
        }
        else if (offers.Count == 0)
        {
            message = "Happy Birthday! No birthday offer is configured for this store today.";
        }

        return new BirthdayEligibilityDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            MobileNumber = customer.MobileNumber,
            CustomerCode = customer.CustomerCode,
            DateOfBirth = customer.DateOfBirth,
            IsBirthdayToday = isBirthday,
            AlreadyRedeemed = redemption is not null,
            RedeemedInvoiceNumber = redemption?.Bill.BillNumber,
            Message = message,
            Offers = offers
        };
    }

    public async Task<BirthdayDiscountApplication> ResolveForSaleAsync(
        Customer customer,
        int storeId,
        int? birthdayOfferId,
        decimal eligibleAmount,
        CancellationToken cancellationToken = default)
    {
        if (!birthdayOfferId.HasValue)
        {
            return BirthdayDiscountApplication.None;
        }

        if (!BusinessCalendar.IsBirthdayToday(customer.DateOfBirth))
        {
            throw new ValidationAppException("Birthday offer can only be applied on the customer's birthday.");
        }

        var today = BusinessCalendar.Today();
        var offer = await _db.StoreDiscounts.FirstOrDefaultAsync(d => d.Id == birthdayOfferId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Birthday offer not found.");
        if (offer.OfferCategory != OfferCategory.Birthday)
        {
            throw new ValidationAppException("The selected offer is not a birthday offer.");
        }

        if (offer.StoreId != storeId)
        {
            throw new BusinessAppException("This birthday offer is not valid for the current store.");
        }

        if (!offer.IsActive)
        {
            throw new BusinessAppException("The birthday offer is inactive.");
        }

        if (offer.ValidFrom.HasValue && today < DateOnly.FromDateTime(offer.ValidFrom.Value))
        {
            throw new BusinessAppException("The birthday offer is not yet valid.");
        }

        if (offer.ValidTo.HasValue && today > DateOnly.FromDateTime(offer.ValidTo.Value))
        {
            throw new BusinessAppException("The birthday offer has expired.");
        }

        var already = await _db.BirthdayOfferRedemptions.AnyAsync(
            r => r.CustomerId == customer.Id
                && r.BirthdayDate == today
                && r.Status == BirthdayRedemptionStatus.Redeemed,
            cancellationToken);
        if (already)
        {
            throw new BusinessAppException("Birthday offer already redeemed today.");
        }

        var rewardType = offer.DiscountKind == DiscountKind.Percentage ? RewardType.Percentage : RewardType.FixedAmount;
        var amount = ReferralCalculator.ComputeBenefit(eligibleAmount, offer.Value, rewardType);
        var percent = offer.DiscountKind == DiscountKind.Percentage ? offer.Value : 0;
        return new BirthdayDiscountApplication(amount, percent, offer.Name, offer.Id, offer.Description);
    }

    public async Task RecordRedemptionAsync(
        Customer customer,
        Bill bill,
        BirthdayDiscountApplication applied,
        int salesPersonId,
        CancellationToken cancellationToken = default)
    {
        if (!applied.Applies || !applied.OfferId.HasValue)
        {
            return;
        }

        _db.BirthdayOfferRedemptions.Add(new BirthdayOfferRedemption
        {
            CustomerId = customer.Id,
            StoreId = bill.StoreId,
            BirthdayOfferId = applied.OfferId.Value,
            BillId = bill.Id,
            SalesPersonId = salesPersonId,
            BirthdayDate = BusinessCalendar.Today(),
            DiscountPercent = applied.Percent,
            DiscountAmount = applied.Amount,
            Status = BirthdayRedemptionStatus.Redeemed,
            CreatedDate = DateTime.Now,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        });
        await _audit.LogAsync(
            AuditActions.BirthdayOfferRedeemed,
            nameof(BirthdayOfferRedemption),
            bill.BillNumber,
            null,
            new { applied.Name, applied.Amount, applied.Percent, bill.BillNumber },
            bill.StoreId,
            cancellationToken);
    }

    public async Task ReleaseRedemptionForCancelledBillAsync(int billId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.BirthdayOfferRedemptions
            .Where(r => r.BillId == billId && r.Status == BirthdayRedemptionStatus.Redeemed)
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            row.Status = BirthdayRedemptionStatus.Cancelled;
            row.UpdatedDate = DateTime.Now;
            row.UpdatedBy = _currentUser.UserId;
        }
    }

    public async Task<DailyBirthdayRunResult> ProcessDailyAsync(CancellationToken cancellationToken = default)
    {
        var today = BusinessCalendar.Today();
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var customers = await _db.Customers.AsNoTracking()
            .Include(c => c.Store)
            .Where(c => c.IsActive && !c.IsDeleted && c.DateOfBirth != null)
            .ToListAsync(cancellationToken);
        var birthdayCustomers = customers.Where(c => BusinessCalendar.IsBirthdayOn(c.DateOfBirth, today)).ToList();

        var result = new DailyBirthdayRunResult { CustomersFound = birthdayCustomers.Count };
        foreach (var customer in birthdayCustomers)
        {
            var existing = await _db.BirthdayMessageLogs
                .FirstOrDefaultAsync(l => l.CustomerId == customer.Id && l.BirthdayDate == today, cancellationToken);
            if (existing is { Status: WhatsAppMessageStatus.Sent })
            {
                result.MessagesSkipped++;
                continue;
            }

            var offer = await ActiveOffersQuery(customer.StoreId, today).FirstOrDefaultAsync(cancellationToken);
            var message = BuildMessage(customer, customer.Store, offer, settings, today);
            var log = existing ?? new BirthdayMessageLog
            {
                CustomerId = customer.Id,
                StoreId = customer.StoreId,
                BirthdayDate = today,
                MobileNumber = customer.MobileNumber,
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            log.BirthdayOfferId = offer?.Id;
            log.OfferName = offer?.Name;
            log.Message = message;
            log.Status = WhatsAppMessageStatus.Pending;
            log.Error = null;
            if (existing is null)
            {
                _db.BirthdayMessageLogs.Add(log);
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                result.MessagesSkipped++;
                continue;
            }

            var send = await _whatsApp.SendTextAsync(customer.MobileNumber, message, cancellationToken);
            if (send.Success)
            {
                log.Status = WhatsAppMessageStatus.Sent;
                log.SentDate = DateTime.Now;
                log.Error = null;
                result.MessagesSent++;
                await _audit.LogAsync(AuditActions.BirthdayWhatsAppSent, nameof(BirthdayMessageLog), log.Id.ToString(), null, new { customer.Id, customer.MobileNumber }, customer.StoreId, cancellationToken);
            }
            else
            {
                log.Status = WhatsAppMessageStatus.Failed;
                log.Error = send.Error ?? "WhatsApp send failed.";
                result.MessagesFailed++;
                await _audit.LogAsync(AuditActions.BirthdayWhatsAppFailed, nameof(BirthdayMessageLog), log.Id.ToString(), null, new { send.Error }, customer.StoreId, cancellationToken);
            }

            log.UpdatedDate = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<PagedResponse<BirthdayReportRowDto>> GetReportAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var today = BusinessCalendar.Today();
        var query = _db.Customers.AsNoTracking().Where(c => !c.IsDeleted && c.DateOfBirth != null);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(c => ids.Contains(c.StoreId));
        }

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

        var customers = await query.Include(c => c.Store).OrderBy(c => c.Name).ToListAsync(cancellationToken);
        var customerIds = customers.Select(c => c.Id).ToList();
        var messages = await _db.BirthdayMessageLogs.AsNoTracking()
            .Where(l => customerIds.Contains(l.CustomerId) && l.BirthdayDate == today)
            .ToListAsync(cancellationToken);
        var redemptions = await _db.BirthdayOfferRedemptions.AsNoTracking()
            .Include(r => r.Bill)
            .Include(r => r.BirthdayOffer)
            .Where(r => customerIds.Contains(r.CustomerId) && r.BirthdayDate == today && r.Status == BirthdayRedemptionStatus.Redeemed)
            .ToListAsync(cancellationToken);

        var rows = customers.Select(c =>
        {
            var msg = messages.FirstOrDefault(m => m.CustomerId == c.Id);
            var red = redemptions.FirstOrDefault(r => r.CustomerId == c.Id);
            return new BirthdayReportRowDto
            {
                CustomerId = c.Id,
                CustomerName = c.Name,
                MobileNumber = c.MobileNumber,
                DateOfBirth = c.DateOfBirth,
                StoreName = c.Store.StoreName,
                BirthdayOffer = red?.BirthdayOffer.Name ?? msg?.OfferName,
                WhatsAppStatus = msg?.Status,
                Redeemed = red is not null,
                InvoiceNumber = red?.Bill.BillNumber,
                DiscountAmount = red?.DiscountAmount ?? 0
            };
        }).ToList();

        if (string.Equals(request.Period, "monthly", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(r => r.DateOfBirth is { } d && d.Month == today.Month).ToList();
        }
        else if (!string.Equals(request.Period, "all", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(r => BusinessCalendar.IsBirthdayOn(r.DateOfBirth, today)).ToList();
        }

        return PagedResponse<BirthdayReportRowDto>.Create(
            rows.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList(),
            request.PageNumber,
            request.PageSize,
            rows.Count);
    }

    private IQueryable<StoreDiscount> ActiveOffersQuery(int storeId, DateOnly today)
    {
        var day = today.ToDateTime(TimeOnly.MinValue);
        return _db.StoreDiscounts.AsNoTracking().Where(d =>
            !d.IsDeleted
            && d.IsActive
            && d.OfferCategory == OfferCategory.Birthday
            && d.StoreId == storeId
            && (d.ValidFrom == null || d.ValidFrom.Value.Date <= day)
            && (d.ValidTo == null || d.ValidTo.Value.Date >= day));
    }

    private static string BuildMessage(Customer customer, Store store, StoreDiscount? offer, BusinessSetting settings, DateOnly today)
    {
        var shop = string.IsNullOrWhiteSpace(store.StoreName) ? settings.ShopName : store.StoreName;
        var offerLine = offer is null
            ? "Please visit us to celebrate your special day."
            : offer.DiscountKind == DiscountKind.Percentage
                ? $"{offer.Name} — {offer.Value:0.##}% OFF"
                : $"{offer.Name} — ₹{offer.Value:0.00} OFF";
        var description = string.IsNullOrWhiteSpace(offer?.Description)
            ? "You can redeem this offer today while making your purchase at our store."
            : offer!.Description;
        return
            $"🎉 Happy Birthday, {customer.Name}! 🎂\n\n" +
            $"Wishing you a very Happy Birthday from {shop}!\n\n" +
            $"🎁 Your Special Birthday Offer:\n*{offerLine}*\n\n" +
            $"{description}\n\n" +
            $"This offer is valid ONLY TODAY — {today:dd MMM yyyy}.\n\n" +
            "Visit us and enjoy your special birthday benefit! 🎉\n\n" +
            "Thank you for being our valued customer.";
    }
}
