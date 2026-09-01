using System.Data;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GramShopPOS.Application.Services;

public sealed class ProductUnitService : IProductUnitService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ProductUnitService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task CreateForStockIncreaseAsync(int productId, int storeId, decimal addedQuantity, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");
        if (!PieceCount.TryGet(product.Unit, addedQuantity, out var count) || count == 0)
        {
            return;
        }

        var hasUnits = await _db.ProductUnits.AnyAsync(u => u.ProductId == productId && !u.IsDeleted, cancellationToken);
        if (!hasUnits)
        {
            var inventoryQty = await _db.Inventories.AsNoTracking()
                .Where(i => i.ProductId == productId && i.StoreId == storeId && !i.IsDeleted)
                .Select(i => i.Quantity)
                .FirstOrDefaultAsync(cancellationToken);
            if (inventoryQty != addedQuantity)
            {
                return;
            }
        }

        var prefix = await EnsurePrefixAsync(product.Category, cancellationToken);
        var start = await AllocateRangeAsync(prefix, count, cancellationToken);
        var now = DateTime.UtcNow;
        var units = new List<ProductUnit>(count);
        for (var i = 0; i < count; i++)
        {
            units.Add(new ProductUnit
            {
                ProductId = productId,
                StoreId = storeId,
                UniqueNumber = $"{prefix}-{(start + i):000000}",
                Status = ProductUnitStatus.Available,
                PurchasePrice = product.PurchasePrice,
                SellingPrice = product.SellingPrice,
                MRP = product.MRP,
                CreatedDate = now,
                CreatedBy = _currentUser.UserId,
                IsActive = true
            });
        }

        _db.ProductUnits.AddRange(units);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkSoldAsync(
        int storeId,
        int productId,
        decimal quantity,
        int billItemId,
        IReadOnlyList<int>? productUnitIds,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == productId, cancellationToken);
        if (!PieceCount.TryGet(product.Unit, quantity, out var count))
        {
            return;
        }

        var hasUnits = await _db.ProductUnits.AnyAsync(u => u.ProductId == productId && !u.IsDeleted, cancellationToken);
        if (!hasUnits)
        {
            return;
        }

        List<ProductUnit> units;
        if (productUnitIds is { Count: > 0 })
        {
            if (productUnitIds.Count != count)
            {
                throw new ValidationAppException("Scanned piece count must match the billed quantity.");
            }

            units = await _db.ProductUnits.Where(u => productUnitIds.Contains(u.Id) && !u.IsDeleted).ToListAsync(cancellationToken);
            if (units.Count != count)
            {
                throw new NotFoundAppException("One or more scanned pieces were not found.");
            }

            foreach (var unit in units)
            {
                if (unit.ProductId != productId)
                {
                    throw new ValidationAppException($"Piece {unit.UniqueNumber} does not belong to this product.");
                }

                if (unit.StoreId != storeId)
                {
                    throw new BusinessAppException($"Piece {unit.UniqueNumber} belongs to another store.");
                }

                if (!IsSellable(unit.Status))
                {
                    throw new BusinessAppException($"Piece {unit.UniqueNumber} is {unit.Status.ToString().ToLowerInvariant()} and cannot be sold again.");
                }
            }
        }
        else
        {
            units = await _db.ProductUnits
                .Where(u => u.ProductId == productId && u.StoreId == storeId && !u.IsDeleted &&
                            (u.Status == ProductUnitStatus.Available || u.Status == ProductUnitStatus.Returned || u.Status == ProductUnitStatus.Exchanged))
                .OrderBy(u => u.UniqueNumber)
                .Take(count)
                .ToListAsync(cancellationToken);
            if (units.Count < count)
            {
                throw new InsufficientStockException($"Only {units.Count} tagged piece(s) are available for this product. Scan specific QR codes or add more stock.");
            }
        }

        foreach (var unit in units)
        {
            unit.Status = ProductUnitStatus.Sold;
            unit.BillItemId = billItemId;
            unit.UpdatedDate = DateTime.UtcNow;
            unit.UpdatedBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreForBillItemAsync(int billItemId, decimal quantity, ProductUnitStatus restoredStatus, CancellationToken cancellationToken = default)
    {
        if (!PieceCount.TryGet("PCS", quantity, out var count) || count == 0)
        {
            return;
        }

        var units = await _db.ProductUnits
            .Where(u => u.BillItemId == billItemId && u.Status == ProductUnitStatus.Sold && !u.IsDeleted)
            .OrderBy(u => u.UniqueNumber)
            .Take(count)
            .ToListAsync(cancellationToken);

        foreach (var unit in units)
        {
            unit.Status = restoredStatus;
            unit.BillItemId = null;
            unit.UpdatedDate = DateTime.UtcNow;
            unit.UpdatedBy = _currentUser.UserId;
        }

        if (units.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RestoreForBillAsync(int billId, CancellationToken cancellationToken = default)
    {
        var itemIds = await _db.BillItems.Where(i => i.BillId == billId).Select(i => i.Id).ToListAsync(cancellationToken);
        var units = await _db.ProductUnits
            .Where(u => u.BillItemId.HasValue && itemIds.Contains(u.BillItemId.Value) && u.Status == ProductUnitStatus.Sold && !u.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var unit in units)
        {
            unit.Status = ProductUnitStatus.Available;
            unit.BillItemId = null;
            unit.UpdatedDate = DateTime.UtcNow;
            unit.UpdatedBy = _currentUser.UserId;
        }

        if (units.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task TransferAsync(int productId, int fromStoreId, int toStoreId, decimal quantity, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == productId, cancellationToken);
        if (!PieceCount.TryGet(product.Unit, quantity, out var count))
        {
            return;
        }

        var hasUnits = await _db.ProductUnits.AnyAsync(u => u.ProductId == productId && !u.IsDeleted, cancellationToken);
        if (!hasUnits)
        {
            return;
        }

        var units = await _db.ProductUnits
            .Where(u => u.ProductId == productId && u.StoreId == fromStoreId && !u.IsDeleted &&
                        (u.Status == ProductUnitStatus.Available || u.Status == ProductUnitStatus.Returned || u.Status == ProductUnitStatus.Exchanged))
            .OrderBy(u => u.UniqueNumber)
            .Take(count)
            .ToListAsync(cancellationToken);
        if (units.Count < count)
        {
            throw new InsufficientStockException($"Only {units.Count} tagged piece(s) can be transferred for this product.");
        }

        foreach (var unit in units)
        {
            unit.StoreId = toStoreId;
            unit.UpdatedDate = DateTime.UtcNow;
            unit.UpdatedBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveForStockDecreaseAsync(int productId, int storeId, decimal quantity, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == productId, cancellationToken);
        if (!PieceCount.TryGet(product.Unit, quantity, out var count))
        {
            return;
        }

        var hasUnits = await _db.ProductUnits.AnyAsync(u => u.ProductId == productId && !u.IsDeleted, cancellationToken);
        if (!hasUnits)
        {
            return;
        }

        var units = await _db.ProductUnits
            .Where(u => u.ProductId == productId && u.StoreId == storeId && !u.IsDeleted &&
                        (u.Status == ProductUnitStatus.Available || u.Status == ProductUnitStatus.Returned || u.Status == ProductUnitStatus.Exchanged))
            .OrderByDescending(u => u.UniqueNumber)
            .Take(count)
            .ToListAsync(cancellationToken);
        if (units.Count < count)
        {
            throw new InsufficientStockException($"Only {units.Count} tagged piece(s) can be removed for this product.");
        }

        foreach (var unit in units)
        {
            unit.Status = ProductUnitStatus.Removed;
            unit.UpdatedDate = DateTime.UtcNow;
            unit.UpdatedBy = _currentUser.UserId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductDto> LookupAsync(string uniqueNumber, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var code = uniqueNumber.Trim().ToUpperInvariant();
        var unit = await _db.ProductUnits.AsNoTracking()
            .Include(u => u.Product).ThenInclude(p => p.Category)
            .Include(u => u.Product).ThenInclude(p => p.Inventories)
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.UniqueNumber == code && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("No product was found for this unique number.");

        int? resolved = null;
        if (storeId.HasValue || !_currentUser.IsAdmin)
        {
            resolved = _currentUser.Access().ResolveStoreId(storeId);
        }

        if (resolved.HasValue && unit.StoreId != resolved.Value)
        {
            throw new BusinessAppException($"Piece {unit.UniqueNumber} belongs to store {unit.Store.StoreCode}.");
        }

        if (!IsSellable(unit.Status))
        {
            var reason = unit.Status == ProductUnitStatus.Sold
                ? "already been sold"
                : $"not available ({unit.Status.ToString().ToLowerInvariant()})";
            throw new BusinessAppException($"Piece {unit.UniqueNumber} has {reason} and cannot be added to the bill.");
        }

        var dto = MapProduct(unit.Product, resolved ?? unit.StoreId);
        dto.ProductUnitId = unit.Id;
        dto.UniqueNumber = unit.UniqueNumber;
        dto.ProductUnitStatus = unit.Status;
        dto.SellingPrice = unit.SellingPrice ?? unit.Product.SellingPrice;
        dto.MRP = unit.MRP ?? unit.Product.MRP;
        if (_currentUser.IsAdmin)
        {
            dto.PurchasePrice = unit.PurchasePrice ?? unit.Product.PurchasePrice;
        }

        return dto;
    }

    public async Task<PagedResponse<ProductUnitDto>> GetAsync(ProductUnitListRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        int? storeId = null;
        if (request.StoreId.HasValue || !_currentUser.IsAdmin)
        {
            storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        }

        var query = _db.ProductUnits.AsNoTracking().Where(u => !u.IsDeleted);
        if (request.ProductId.HasValue)
        {
            query = query.Where(u => u.ProductId == request.ProductId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(u => u.StoreId == storeId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(u => u.UniqueNumber.Contains(s) || u.Product.ProductName.Contains(s) || u.Product.ProductCode.Contains(s));
        }

        var showPurchase = _currentUser.IsAdmin;
        var projected = query.OrderBy(u => u.UniqueNumber).Select(u => new ProductUnitDto
        {
            Id = u.Id,
            ProductId = u.ProductId,
            StoreId = u.StoreId,
            StoreCode = u.Store.StoreCode,
            UniqueNumber = u.UniqueNumber,
            Status = u.Status,
            BillItemId = u.BillItemId,
            CreatedDate = u.CreatedDate,
            ProductName = u.Product.ProductName,
            CategoryName = u.Product.Category.Name,
            PurchasePrice = showPurchase ? (u.PurchasePrice ?? u.Product.PurchasePrice) : 0,
            MRP = u.MRP ?? u.Product.MRP,
            SellingPrice = u.SellingPrice ?? u.Product.SellingPrice,
            WeightGrams = u.Product.WeightGrams,
            Metal = u.Product.Metal
        });
        var page = await projected.ToPagedAsync(request, cancellationToken);
        foreach (var item in page.Items)
        {
            item.StatusName = item.Status.ToString();
        }

        return page;
    }

    public async Task<IReadOnlyList<ProductUnitLabelDto>> GetLabelDataAsync(ProductUnitIdsRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var query = _db.ProductUnits.AsNoTracking().Where(u => !u.IsDeleted);
        if (request.Ids.Count > 0)
        {
            query = query.Where(u => request.Ids.Contains(u.Id));
        }
        else if (request.ProductId.HasValue)
        {
            query = query.Where(u => u.ProductId == request.ProductId.Value);
            if (request.StoreId.HasValue || !_currentUser.IsAdmin)
            {
                var storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
                query = query.Where(u => u.StoreId == storeId);
            }
        }
        else
        {
            throw new ValidationAppException("Select pieces or a product to print.");
        }

        var units = await query
            .OrderBy(u => u.UniqueNumber)
            .Select(u => new ProductUnitLabelDto
            {
                Id = u.Id,
                UniqueNumber = u.UniqueNumber,
                ProductName = u.Product.ProductName,
                CategoryName = u.Product.Category.Name,
                MRP = u.MRP ?? u.Product.MRP,
                SellingPrice = u.SellingPrice ?? u.Product.SellingPrice
            })
            .ToListAsync(cancellationToken);

        units = units
            .GroupBy(u => u.UniqueNumber, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(u => u.UniqueNumber)
            .ToList();

        if (units.Count == 0)
        {
            throw new NotFoundAppException("No jewellery pieces were found for printing.");
        }

        return units;
    }

    public async Task<ProductUnitDto> UpdatePricesAsync(int id, UpdateProductUnitRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var unit = await _db.ProductUnits
            .Include(u => u.Product).ThenInclude(p => p.Category)
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Jewellery piece not found.");

        unit.SellingPrice = Money.Round(request.SellingPrice);
        unit.MRP = Money.Round(request.MRP);
        unit.PurchasePrice = Money.Round(request.PurchasePrice ?? unit.PurchasePrice ?? unit.Product.PurchasePrice);
        unit.UpdatedDate = DateTime.UtcNow;
        unit.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return MapDto(unit);
    }

    private async Task<string> EnsurePrefixAsync(Category category, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(category.CodePrefix))
        {
            return category.CodePrefix.Trim().ToUpperInvariant();
        }

        var suggested = CategoryPrefixes.Suggest(category.Name);
        var prefix = suggested;
        var n = 2;
        while (await _db.Categories.AnyAsync(c => c.Id != category.Id && c.CodePrefix == prefix, cancellationToken))
        {
            prefix = $"{suggested}{n}";
            n++;
        }

        var tracked = await _db.Categories.FirstAsync(c => c.Id == category.Id, cancellationToken);
        tracked.CodePrefix = prefix;
        tracked.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        category.CodePrefix = prefix;
        return prefix;
    }

    private async Task<int> AllocateRangeAsync(string prefix, int count, CancellationToken cancellationToken)
    {
        await EnsureSequenceRowAsync(prefix, cancellationToken);
        var provider = _db.Database.ProviderName ?? string.Empty;
        if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var last = await IncrementSqlServerAsync(prefix, count, cancellationToken);
            return last - count + 1;
        }

        var seq = await _db.ProductUnitSequences.FirstAsync(s => s.Prefix == prefix, cancellationToken);
        var start = seq.LastNumber + 1;
        seq.LastNumber += count;
        seq.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return start;
    }

    private async Task EnsureSequenceRowAsync(string prefix, CancellationToken cancellationToken)
    {
        if (await _db.ProductUnitSequences.AnyAsync(s => s.Prefix == prefix, cancellationToken))
        {
            return;
        }

        _db.ProductUnitSequences.Add(new ProductUnitSequence
        {
            Prefix = prefix,
            LastNumber = 0,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent insert of the same prefix row is expected.
        }
    }

    private async Task<int> IncrementSqlServerAsync(string prefix, int count, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            UPDATE [ProductUnitSequences]
            SET [LastNumber] = [LastNumber] + @count, [UpdatedDate] = SYSUTCDATETIME()
            OUTPUT INSERTED.[LastNumber]
            WHERE [Prefix] = @prefix
            """;

        var countParam = command.CreateParameter();
        countParam.ParameterName = "@count";
        countParam.Value = count;
        command.Parameters.Add(countParam);

        var prefixParam = command.CreateParameter();
        prefixParam.ParameterName = "@prefix";
        prefixParam.Value = prefix;
        command.Parameters.Add(prefixParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw new InvalidOperationException("Failed to allocate unique product numbers.");
        }

        var local = _db.ProductUnitSequences.Local.FirstOrDefault(x => x.Prefix == prefix);
        if (local is not null)
        {
            await _db.ReloadTrackedAsync(local, cancellationToken);
        }

        return Convert.ToInt32(result);
    }

    private ProductDto MapProduct(Product p, int? storeId) => new()
    {
        Id = p.Id,
        ProductCode = p.ProductCode,
        Barcode = p.Barcode,
        ProductName = p.ProductName,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        Unit = p.Unit,
        PurchasePrice = _currentUser.IsAdmin ? p.PurchasePrice : 0,
        SellingPrice = p.SellingPrice,
        MRP = p.MRP,
        TaxPercent = p.TaxPercent,
        MinimumStockLevel = p.MinimumStockLevel,
        ImagePath = p.ImagePath,
        ImageUrl = string.IsNullOrWhiteSpace(p.ImagePath) ? "/images/default-jewellery.svg" : p.ImagePath,
        WeightGrams = p.WeightGrams,
        Metal = p.Metal,
        IsActive = p.IsActive,
        StockQuantity = storeId == null
            ? p.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity)
            : p.Inventories.Where(i => !i.IsDeleted && i.StoreId == storeId).Select(i => i.Quantity).FirstOrDefault(),
        IsLowStock = false
    };

    private ProductUnitDto MapDto(ProductUnit u) => new()
    {
        Id = u.Id,
        ProductId = u.ProductId,
        StoreId = u.StoreId,
        StoreCode = u.Store.StoreCode,
        UniqueNumber = u.UniqueNumber,
        Status = u.Status,
        StatusName = u.Status.ToString(),
        BillItemId = u.BillItemId,
        CreatedDate = u.CreatedDate,
        ProductName = u.Product.ProductName,
        CategoryName = u.Product.Category.Name,
        PurchasePrice = _currentUser.IsAdmin ? (u.PurchasePrice ?? u.Product.PurchasePrice) : 0,
        MRP = u.MRP ?? u.Product.MRP,
        SellingPrice = u.SellingPrice ?? u.Product.SellingPrice,
        WeightGrams = u.Product.WeightGrams,
        Metal = u.Product.Metal
    };

    private static bool IsSellable(ProductUnitStatus status) =>
        status is ProductUnitStatus.Available or ProductUnitStatus.Returned or ProductUnitStatus.Exchanged;
}
