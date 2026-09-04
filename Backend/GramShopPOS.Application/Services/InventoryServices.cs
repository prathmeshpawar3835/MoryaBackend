using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockEngine _stock;
    private readonly IAuditService _audit;
    private readonly IProductUnitService _units;

    public InventoryService(IAppDbContext db, ICurrentUser currentUser, IStockEngine stock, IAuditService audit, IProductUnitService? units = null)
    {
        _db = db;
        _currentUser = currentUser;
        _stock = stock;
        _audit = audit;
        _units = units ?? new ProductUnitService(_db, _currentUser);
    }

    public async Task<PagedResponse<InventoryDto>> GetAsync(InventoryListRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var storeId = request.StoreId;
        if (!_currentUser.IsAdmin || storeId.HasValue)
        {
            storeId = _currentUser.Access().ResolveStoreId(storeId);
        }

        var query = _db.Inventories.AsNoTracking().Where(i => !i.IsDeleted);
        if (storeId.HasValue)
        {
            query = query.Where(i => i.StoreId == storeId);
        }
        else if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(i => ids.Contains(i.StoreId));
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(i => i.ProductId == request.ProductId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(i => i.Product.ProductCode.Contains(s) || i.Product.ProductName.Contains(s) || (i.Product.Barcode != null && i.Product.Barcode.Contains(s)));
        }

        if (request.LowStockOnly == true)
        {
            query = query.Where(i => i.Quantity <= i.Product.MinimumStockLevel);
        }

        var projected = query.Select(i => new InventoryDto
        {
            Id = i.Id,
            StoreId = i.StoreId,
            StoreCode = i.Store.StoreCode,
            ProductId = i.ProductId,
            ProductCode = i.Product.ProductCode,
            ProductName = i.Product.ProductName,
            Barcode = i.Product.Barcode,
            Quantity = i.Quantity,
            MinimumStockLevel = i.Product.MinimumStockLevel,
            IsLowStock = i.Quantity <= i.Product.MinimumStockLevel,
            PurchasePrice = _currentUser.IsAdmin ? i.Product.PurchasePrice : 0,
            SellingPrice = i.Product.SellingPrice
        }).OrderBy(i => i.ProductName);

        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<InventoryDto> GetByProductAsync(int productId, int storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.Access().EnsureStoreAccess(storeId);
        var item = await _db.Inventories.AsNoTracking()
            .Where(i => i.ProductId == productId && i.StoreId == storeId && !i.IsDeleted)
            .Select(i => new InventoryDto
            {
                Id = i.Id,
                StoreId = i.StoreId,
                StoreCode = i.Store.StoreCode,
                ProductId = i.ProductId,
                ProductCode = i.Product.ProductCode,
                ProductName = i.Product.ProductName,
                Barcode = i.Product.Barcode,
                Quantity = i.Quantity,
                MinimumStockLevel = i.Product.MinimumStockLevel,
                IsLowStock = i.Quantity <= i.Product.MinimumStockLevel,
                PurchasePrice = _currentUser.IsAdmin ? i.Product.PurchasePrice : 0,
                SellingPrice = i.Product.SellingPrice
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundAppException("Inventory not found.");
        return item;
    }

    public async Task<PagedResponse<StockMovementDto>> GetLedgerAsync(InventoryListRequest request, int? productId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.StockMovements.AsNoTracking().AsQueryable();
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(m => ids.Contains(m.StoreId));
        }

        if (request.StoreId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(request.StoreId.Value);
            query = query.Where(m => m.StoreId == request.StoreId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(m => m.ProductId == productId.Value);
        }

        var projected = query.OrderByDescending(m => m.CreatedDate).Select(m => new StockMovementDto
        {
            Id = m.Id,
            ProductId = m.ProductId,
            ProductCode = m.Product.ProductCode,
            ProductName = m.Product.ProductName,
            StoreId = m.StoreId,
            Quantity = m.Quantity,
            PreviousQuantity = m.PreviousQuantity,
            NewQuantity = m.NewQuantity,
            MovementType = m.MovementType,
            ReferenceNumber = m.ReferenceNumber,
            Reason = m.Reason,
            CreatedDate = m.CreatedDate
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task StockInAsync(StockInRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        _currentUser.Access().EnsureStoreAccess(request.StoreId);
        if (request.Quantity <= 0)
        {
            throw new ValidationAppException("Quantity must be greater than zero.");
        }

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _stock.ChangeAsync(request.StoreId, request.ProductId, request.Quantity, StockMovementType.Purchase, null, request.InvoiceNumber, request.Reason ?? "Stock in", false, _currentUser.UserId, cancellationToken);
        await _units.CreateForStockIncreaseAsync(request.ProductId, request.StoreId, request.Quantity, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.StockIn, nameof(Inventory), request.ProductId.ToString(), null, request, request.StoreId, cancellationToken);
    }

    public async Task AdjustAsync(StockAdjustRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenAppException("Only administrators can adjust stock.");
        }

        _currentUser.Access().EnsureStoreAccess(request.StoreId);
        if (request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationAppException("Quantity and reason are required.");
        }

        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var delta = request.IsIncrease ? request.Quantity : -request.Quantity;
        var type = request.IsIncrease ? StockMovementType.AdjustmentIn : StockMovementType.AdjustmentOut;
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        await _stock.ChangeAsync(request.StoreId, request.ProductId, delta, type, null, null, request.Reason, settings.AllowNegativeStock, _currentUser.UserId, cancellationToken);
        if (request.IsIncrease)
        {
            await _units.CreateForStockIncreaseAsync(request.ProductId, request.StoreId, request.Quantity, cancellationToken);
        }
        else
        {
            await _units.RemoveForStockDecreaseAsync(request.ProductId, request.StoreId, request.Quantity, cancellationToken);
        }
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.StockAdjusted, nameof(Inventory), request.ProductId.ToString(), null, request, request.StoreId, cancellationToken);
    }

    public async Task TransferAsync(StockTransferRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        if (request.FromStoreId == request.ToStoreId)
        {
            throw new ValidationAppException("Source and destination stores must be different.");
        }

        if (request.Items.Count == 0 || request.Items.Any(i => i.Quantity <= 0))
        {
            throw new ValidationAppException("Transfer items are invalid.");
        }

        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var transfer = new StockTransfer
        {
            TransferNumber = $"TR-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.FromStoreId}",
            FromStoreId = request.FromStoreId,
            ToStoreId = request.ToStoreId,
            TransferDate = DateTime.UtcNow,
            Status = StockTransferStatus.Completed,
            Reason = request.Reason,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.StockTransfers.Add(transfer);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var item in request.Items)
        {
            _db.StockTransferItems.Add(new StockTransferItem
            {
                StockTransferId = transfer.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
            await _stock.ChangeAsync(request.FromStoreId, item.ProductId, -item.Quantity, StockMovementType.TransferOut, transfer.Id, transfer.TransferNumber, request.Reason, settings.AllowNegativeStock, _currentUser.UserId, cancellationToken);
            await _stock.ChangeAsync(request.ToStoreId, item.ProductId, item.Quantity, StockMovementType.TransferIn, transfer.Id, transfer.TransferNumber, request.Reason, false, _currentUser.UserId, cancellationToken);
            await _units.TransferAsync(item.ProductId, request.FromStoreId, request.ToStoreId, item.Quantity, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.StockTransfer, nameof(StockTransfer), transfer.Id.ToString(), null, request, request.FromStoreId, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryDto>> GetLowStockAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        return (await GetAsync(new InventoryListRequest { StoreId = storeId, LowStockOnly = true, PageSize = 100 }, cancellationToken)).Items;
    }
}

public sealed class PurchaseService : IPurchaseService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStockEngine _stock;
    private readonly IAuditService _audit;
    private readonly IProductUnitService _units;

    public PurchaseService(IAppDbContext db, ICurrentUser currentUser, IStockEngine stock, IAuditService audit, IProductUnitService? units = null)
    {
        _db = db;
        _currentUser = currentUser;
        _stock = stock;
        _audit = audit;
        _units = units ?? new ProductUnitService(_db, _currentUser);
    }

    public async Task<PagedResponse<PurchaseDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.Purchases.AsNoTracking().Where(p => !p.IsDeleted);
        if (!_currentUser.IsAdmin)
        {
            var ids = _currentUser.AssignedStoreIds;
            query = query.Where(p => ids.Contains(p.StoreId));
        }

        if (request.StoreId.HasValue)
        {
            _currentUser.Access().EnsureStoreAccess(request.StoreId.Value);
            query = query.Where(p => p.StoreId == request.StoreId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p => p.InvoiceNumber.Contains(s) || p.SupplierName.Contains(s));
        }

        ApplyDate(ref query, request);
        var projected = query.OrderByDescending(p => p.PurchaseDate).Select(p => new PurchaseDto
        {
            Id = p.Id,
            StoreId = p.StoreId,
            StoreCode = p.Store.StoreCode,
            SupplierId = p.SupplierId,
            SupplierName = p.SupplierName,
            InvoiceNumber = p.InvoiceNumber,
            PurchaseDate = p.PurchaseDate,
            Total = p.Total,
            Notes = p.Notes
        });
        return await projected.ToPagedAsync(request, cancellationToken);
    }

    public async Task<PurchaseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var purchase = await _db.Purchases.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Purchase not found.");
        _currentUser.Access().EnsureStoreAccess(purchase.StoreId);
        return Map(purchase, true);
    }

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        _currentUser.Access().EnsureStoreAccess(request.StoreId);
        if (request.Items.Count == 0)
        {
            throw new ValidationAppException("Purchase must contain items.");
        }

        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var supplierName = request.SupplierName?.Trim() ?? string.Empty;
        int? supplierId = request.SupplierId;
        if (supplierId.HasValue)
        {
            var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId && !s.IsDeleted && s.IsActive, cancellationToken)
                ?? throw new NotFoundAppException("Supplier not found.");
            supplierName = supplier.Name;
        }
        else if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new ValidationAppException("Supplier name is required.");
        }

        var purchase = new Purchase
        {
            StoreId = request.StoreId,
            SupplierId = supplierId,
            SupplierName = supplierName,
            InvoiceNumber = request.InvoiceNumber.Trim(),
            PurchaseDate = request.Date ?? DateTime.UtcNow,
            Notes = request.Notes,
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            IsActive = true
        };
        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync(cancellationToken);

        decimal total = 0;
        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0 || item.PurchasePrice < 0)
            {
                throw new ValidationAppException("Invalid purchase item.");
            }

            if (!await _db.Products.AnyAsync(p => p.Id == item.ProductId && p.IsActive && !p.IsDeleted, cancellationToken))
            {
                throw new ValidationAppException($"Invalid product {item.ProductId}.");
            }

            var lineTotal = Money.Round(item.Quantity * item.PurchasePrice);
            total += lineTotal;
            _db.PurchaseItems.Add(new PurchaseItem
            {
                PurchaseId = purchase.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PurchasePrice = Money.Round(item.PurchasePrice),
                Total = lineTotal,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
            await _stock.ChangeAsync(request.StoreId, item.ProductId, item.Quantity, StockMovementType.Purchase, purchase.Id, purchase.InvoiceNumber, "Purchase stock-in", false, _currentUser.UserId, cancellationToken);
            await _units.CreateForStockIncreaseAsync(item.ProductId, request.StoreId, item.Quantity, cancellationToken);
        }

        purchase.Total = Money.Round(total);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.PurchaseCreated, nameof(Purchase), purchase.Id.ToString(), null, purchase, purchase.StoreId, cancellationToken);
        return await GetByIdAsync(purchase.Id, cancellationToken);
    }

    private static void ApplyDate(ref IQueryable<Purchase> query, PagedRequest request)
    {
        if (request.FromDate.HasValue)
        {
            query = query.Where(p => p.PurchaseDate >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(p => p.PurchaseDate < to);
        }
    }

    private static PurchaseDto Map(Purchase p, bool includeItems = false) => new()
    {
        Id = p.Id,
        StoreId = p.StoreId,
        StoreCode = p.Store?.StoreCode ?? string.Empty,
        SupplierId = p.SupplierId,
        SupplierName = p.SupplierName,
        InvoiceNumber = p.InvoiceNumber,
        PurchaseDate = p.PurchaseDate,
        Total = p.Total,
        Notes = p.Notes,
        Items = includeItems
            ? p.Items.Select(i => new PurchaseItemDto
            {
                ProductId = i.ProductId,
                ProductCode = i.Product.ProductCode,
                ProductName = i.Product.ProductName,
                Quantity = i.Quantity,
                PurchasePrice = i.PurchasePrice,
                Total = i.Total
            }).ToList()
            : []
    };
}
