using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Operations;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class DiscountService : IDiscountService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public DiscountService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<StoreDiscountDto>> GetAsync(int? storeId, bool activeOnly, OfferCategory? category = null, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.StoreDiscounts.AsNoTracking().Where(d => !d.IsDeleted);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(d => ids.Contains(d.StoreId));
        }

        if (storeId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(storeId.Value);
            query = query.Where(d => d.StoreId == storeId.Value);
        }

        query = query.Where(d => d.OfferCategory == (category ?? OfferCategory.Store)
            || ((category ?? OfferCategory.Store) == OfferCategory.Store && d.OfferCategory == 0));

        if (activeOnly)
        {
            var today = BusinessCalendar.Today().ToDateTime(TimeOnly.MinValue);
            query = query.Where(d => d.IsActive &&
                (d.ValidFrom == null || d.ValidFrom.Value.Date <= today) &&
                (d.ValidTo == null || d.ValidTo.Value.Date >= today));
        }

        return await query.OrderBy(d => d.Name).Select(d => new StoreDiscountDto
        {
            Id = d.Id,
            StoreId = d.StoreId,
            StoreName = d.Store.StoreName,
            Name = d.Name,
            Description = d.Description,
            OfferCategory = d.OfferCategory,
            DiscountKind = d.DiscountKind,
            Value = d.Value,
            ValidFrom = d.ValidFrom,
            ValidTo = d.ValidTo,
            IsActive = d.IsActive
        }).ToListAsync(cancellationToken);
    }

    public async Task<StoreDiscountDto> CreateAsync(StoreDiscountRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        _currentUser.Access().EnsureStoreAccess(request.StoreId);
        Validate(request);
        var entity = new StoreDiscount
        {
            StoreId = request.StoreId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            OfferCategory = request.OfferCategory == 0 ? OfferCategory.Store : request.OfferCategory,
            DiscountKind = request.DiscountKind,
            Value = Money.Round(request.Value),
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };
        _db.StoreDiscounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.DiscountCreated, nameof(StoreDiscount), entity.Id.ToString(), null, entity, request.StoreId, cancellationToken);
        return (await GetAsync(request.StoreId, false, entity.OfferCategory, cancellationToken)).First(d => d.Id == entity.Id);
    }

    public async Task<StoreDiscountDto> UpdateAsync(int id, StoreDiscountRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        Validate(request);
        var entity = await _db.StoreDiscounts.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Discount not found.");
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.OfferCategory = request.OfferCategory == 0 ? entity.OfferCategory : request.OfferCategory;
        entity.DiscountKind = request.DiscountKind;
        entity.Value = Money.Round(request.Value);
        entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo;
        entity.IsActive = request.IsActive;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.DiscountUpdated, nameof(StoreDiscount), id.ToString(), null, entity, entity.StoreId, cancellationToken);
        return (await GetAsync(entity.StoreId, false, entity.OfferCategory, cancellationToken)).First(d => d.Id == id);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var entity = await _db.StoreDiscounts.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Discount not found.");
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(StoreDiscountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationAppException("Discount name is required.");
        }

        if (request.Value <= 0)
        {
            throw new ValidationAppException("Discount value must be greater than zero.");
        }

        if (request.DiscountKind == DiscountKind.Percentage && request.Value > 100)
        {
            throw new ValidationAppException("Percentage discount cannot exceed 100.");
        }
    }
}

public sealed class SupplierService : ISupplierService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public SupplierService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResponse<SupplierDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.Suppliers.AsNoTracking().Where(s => !s.IsDeleted);
        if (request.StoreId.HasValue)
        {
            query = query.Where(s => s.StoreId == null || s.StoreId == request.StoreId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(s) || (x.Phone != null && x.Phone.Contains(s)));
        }

        var projected = query.OrderBy(s => s.Name).Select(s => new SupplierDto
        {
            Id = s.Id,
            StoreId = s.StoreId,
            StoreName = s.Store != null ? s.Store.StoreName : null,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            GSTNumber = s.GSTNumber,
            Notes = s.Notes,
            IsActive = s.IsActive,
            TotalPurchased = s.Purchases.Sum(p => p.Total)
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<SupplierDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _db.Suppliers.AsNoTracking().Include(x => x.Store).Include(x => x.Purchases)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Supplier not found.");
        return Map(s);
    }

    public async Task<SupplierDto> CreateAsync(SupplierRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationAppException("Supplier name is required.");
        }

        var entity = MapRequest(new Supplier(), request);
        entity.CreatedDate = DateTime.UtcNow;
        entity.CreatedBy = _currentUser.UserId;
        entity.IsActive = request.IsActive;
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.SupplierCreated, nameof(Supplier), entity.Id.ToString(), null, entity, request.StoreId, cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<SupplierDto> UpdateAsync(int id, SupplierRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Supplier not found.");
        MapRequest(entity, request);
        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.SupplierUpdated, nameof(Supplier), id.ToString(), null, entity, entity.StoreId, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private static Supplier MapRequest(Supplier entity, SupplierRequest request)
    {
        entity.StoreId = request.StoreId;
        entity.Name = request.Name.Trim();
        entity.ContactPerson = request.ContactPerson;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.GSTNumber = request.GSTNumber;
        entity.Notes = request.Notes;
        entity.IsActive = request.IsActive;
        return entity;
    }

    private static SupplierDto Map(Supplier s) => new()
    {
        Id = s.Id,
        StoreId = s.StoreId,
        StoreName = s.Store?.StoreName,
        Name = s.Name,
        ContactPerson = s.ContactPerson,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        GSTNumber = s.GSTNumber,
        Notes = s.Notes,
        IsActive = s.IsActive,
        TotalPurchased = s.Purchases?.Sum(p => p.Total) ?? 0
    };
}

public sealed class RepairService : IRepairService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public RepairService(IAppDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResponse<RepairJobDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.RepairJobs.AsNoTracking().Where(j => !j.IsDeleted);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(j => ids.Contains(j.StoreId));
        }

        if (request.StoreId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(request.StoreId.Value);
            query = query.Where(j => j.StoreId == request.StoreId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(j =>
                j.JobNumber.Contains(s) ||
                j.MobileNumber.Contains(s) ||
                j.CustomerName.Contains(s) ||
                (j.InvoiceNumber != null && j.InvoiceNumber.Contains(s)) ||
                j.ProductName.Contains(s));
        }

        var projected = query.OrderByDescending(j => j.ReceivedDate).Select(j => new RepairJobDto
        {
            Id = j.Id,
            StoreId = j.StoreId,
            JobNumber = j.JobNumber,
            CustomerId = j.CustomerId,
            CustomerName = j.CustomerName,
            MobileNumber = j.MobileNumber,
            BillId = j.BillId,
            InvoiceNumber = j.InvoiceNumber,
            ProductId = j.ProductId,
            ProductName = j.ProductName,
            ProductDetails = j.ProductDetails,
            JobType = j.JobType,
            Status = j.Status,
            ReceivedDate = j.ReceivedDate,
            ExpectedDate = j.ExpectedDate,
            CompletedDate = j.CompletedDate,
            DeliveredDate = j.DeliveredDate,
            Notes = j.Notes
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<RepairJobDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = await _db.RepairJobs.AsNoTracking().Include(j => j.History).ThenInclude(h => h.User)
            .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Repair / polish job not found.");
        _currentUser.Access().EnsureStoreAccess(job.StoreId);
        return Map(job);
    }

    public async Task<RepairJobDto> CreateAsync(CreateRepairJobRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.ProductName))
        {
            throw new ValidationAppException("Customer name, mobile number and product are required.");
        }

        if (request.BillId.HasValue)
        {
            var bill = await _db.Bills.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == request.BillId, cancellationToken)
                ?? throw new NotFoundAppException("Invoice not found.");
            _currentUser.Access().EnsureStoreAccess(bill.StoreId);
            request.InvoiceNumber ??= bill.BillNumber;
            if (request.BillItemId.HasValue)
            {
                var item = bill.Items.FirstOrDefault(i => i.Id == request.BillItemId)
                    ?? throw new ValidationAppException("Selected product does not belong to the invoice.");
                request.ProductId ??= item.ProductId;
                if (string.IsNullOrWhiteSpace(request.ProductName))
                {
                    request.ProductName = item.ProductName;
                }
            }
        }

        var job = new RepairJob
        {
            StoreId = storeId,
            CustomerId = request.CustomerId,
            BillId = request.BillId,
            BillItemId = request.BillItemId,
            ProductId = request.ProductId,
            JobNumber = $"RP-{DateTime.UtcNow:yyyyMMddHHmmss}-{storeId}",
            CustomerName = request.CustomerName.Trim(),
            MobileNumber = request.MobileNumber.Trim(),
            InvoiceNumber = request.InvoiceNumber,
            ProductName = request.ProductName.Trim(),
            ProductDetails = request.ProductDetails,
            JobType = request.JobType,
            Status = RepairJobStatus.Received,
            ReceivedDate = DateTime.UtcNow,
            ExpectedDate = request.ExpectedDate,
            Notes = request.Notes,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.RepairJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        AddHistory(job, RepairJobStatus.Received, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.RepairJobCreated, nameof(RepairJob), job.Id.ToString(), null, job, storeId, cancellationToken);
        return await GetByIdAsync(job.Id, cancellationToken);
    }

    public async Task<RepairJobDto> UpdateAsync(int id, UpdateRepairJobRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var job = await _db.RepairJobs.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Repair / polish job not found.");
        _currentUser.Access().EnsureStoreAccess(job.StoreId);
        job.Status = request.Status;
        job.ExpectedDate = request.ExpectedDate ?? job.ExpectedDate;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            job.Notes = request.Notes;
        }

        if (request.Status == RepairJobStatus.Ready || request.Status == RepairJobStatus.Delivered)
        {
            job.CompletedDate ??= DateTime.UtcNow;
        }

        if (request.Status == RepairJobStatus.Delivered)
        {
            job.DeliveredDate = DateTime.UtcNow;
        }

        job.UpdatedDate = DateTime.UtcNow;
        job.UpdatedBy = _currentUser.UserId;
        AddHistory(job, request.Status, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.RepairJobUpdated, nameof(RepairJob), id.ToString(), null, request, job.StoreId, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private void AddHistory(RepairJob job, RepairJobStatus status, string? notes) =>
        _db.RepairJobHistories.Add(new RepairJobHistory
        {
            RepairJobId = job.Id,
            Status = status,
            Notes = notes,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });

    private static RepairJobDto Map(RepairJob j) => new()
    {
        Id = j.Id,
        StoreId = j.StoreId,
        JobNumber = j.JobNumber,
        CustomerId = j.CustomerId,
        CustomerName = j.CustomerName,
        MobileNumber = j.MobileNumber,
        BillId = j.BillId,
        InvoiceNumber = j.InvoiceNumber,
        ProductId = j.ProductId,
        ProductName = j.ProductName,
        ProductDetails = j.ProductDetails,
        JobType = j.JobType,
        Status = j.Status,
        ReceivedDate = j.ReceivedDate,
        ExpectedDate = j.ExpectedDate,
        CompletedDate = j.CompletedDate,
        DeliveredDate = j.DeliveredDate,
        Notes = j.Notes,
        History = j.History?.OrderBy(h => h.CreatedDate).Select(h => new RepairJobHistoryDto
        {
            Status = h.Status,
            Notes = h.Notes,
            CreatedDate = h.CreatedDate,
            UserName = h.User?.UserName ?? string.Empty
        }).ToList() ?? []
    };
}
