using System.Linq.Expressions;
using System.Text.Json;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly IStockEngine _stock;
    private readonly IExcelWorkbookService _excel;
    private readonly IProductUnitService _units;

    public ProductService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IAuditService audit,
        IStockEngine stock,
        IExcelWorkbookService excel,
        IProductUnitService? units = null)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
        _stock = stock;
        _excel = excel;
        _units = units ?? new ProductUnitService(_db, _currentUser);
    }

    public async Task<PagedResponse<ProductDto>> GetAsync(ProductListRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        int? storeId = null;
        if (request.StoreId.HasValue || !_currentUser.IsAdmin)
        {
            storeId = _currentUser.Access().ResolveStoreId(request.StoreId);
        }

        var query = _db.Products.AsNoTracking().Where(p => !p.IsDeleted);
        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p => p.ProductCode.Contains(s) || p.ProductName.Contains(s) || (p.Barcode != null && p.Barcode.Contains(s))
                || p.Units.Any(u => !u.IsDeleted && u.UniqueNumber.Contains(s)));
        }

        var projected = query.Select(p => new ProductDto
        {
            Id = p.Id,
            ProductCode = p.ProductCode,
            Barcode = p.Barcode,
            ProductName = p.ProductName,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            Unit = p.Unit,
            PurchasePrice = _currentUser.IsAdmin ? p.PurchasePrice : 0,
            SellingPrice = p.SellingPrice,
            MRP = p.MRP,
            TaxPercent = p.TaxPercent,
            MinimumStockLevel = p.MinimumStockLevel,
            ImagePath = p.ImagePath,
            ImageUrl = p.ImagePath ?? "/images/default-jewellery.svg",
            WeightGrams = p.WeightGrams,
            Metal = p.Metal,
            IsActive = p.IsActive,
            StockQuantity = storeId == null
                ? p.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity)
                : p.Inventories.Where(i => !i.IsDeleted && i.StoreId == storeId).Select(i => (decimal?)i.Quantity).FirstOrDefault() ?? 0,
            IsLowStock = false
        });

        var sortMap = new Dictionary<string, Expression<Func<ProductDto, object>>>
        {
            ["productcode"] = x => x.ProductCode,
            ["productname"] = x => x.ProductName,
            ["sellingprice"] = x => x.SellingPrice,
            ["createddate"] = x => x.Id,
            ["id"] = x => x.Id
        };
        projected = projected.ApplySort(request.SortColumn, request.SortDirection, sortMap, "id");
        var page = await projected.ToPagedAsync(request, cancellationToken);
        var items = page.Items.Select(p =>
        {
            p.IsLowStock = (p.StockQuantity ?? 0) <= p.MinimumStockLevel;
            return p;
        }).Where(p => request.LowStockOnly != true || p.IsLowStock).ToList();

        if (request.LowStockOnly == true)
        {
            return PagedResponse<ProductDto>.Create(items, request.PageNumber, request.PageSize, items.Count);
        }

        return PagedResponse<ProductDto>.Create(items, page.PageNumber, page.PageSize, page.TotalCount);
    }

    public async Task<ProductDto> GetByIdAsync(int id, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        int? resolved = null;
        if (storeId.HasValue || !_currentUser.IsAdmin)
        {
            resolved = _currentUser.Access().ResolveStoreId(storeId);
        }

        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");
        return Map(product, resolved);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        await ValidatePricesAsync(request.CategoryId, request.ProductCode, request.Barcode, null, request.PurchasePrice, request.SellingPrice, request.MRP, request.TaxPercent, cancellationToken);

        var product = new Product
        {
            ProductCode = request.ProductCode.Trim().ToUpperInvariant(),
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            ProductName = request.ProductName.Trim(),
            CategoryId = request.CategoryId,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "PCS" : request.Unit.Trim().ToUpperInvariant(),
            PurchasePrice = Money.Round(request.PurchasePrice),
            SellingPrice = Money.Round(request.SellingPrice),
            MRP = Money.Round(request.MRP),
            TaxPercent = request.TaxPercent,
            MinimumStockLevel = request.MinimumStockLevel,
            WeightGrams = request.WeightGrams,
            Metal = string.IsNullOrWhiteSpace(request.Metal) ? null : request.Metal.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        if (request.OpeningStock > 0 && request.OpeningStockStoreId.HasValue)
        {
            await _stock.ChangeAsync(request.OpeningStockStoreId.Value, product.Id, request.OpeningStock, StockMovementType.OpeningStock, product.Id, product.ProductCode, "Opening stock", false, _currentUser.UserId, cancellationToken);
            await _units.CreateForStockIncreaseAsync(product.Id, request.OpeningStockStoreId.Value, request.OpeningStock, cancellationToken);
        }

        await _audit.LogAsync(AuditActions.ProductCreated, nameof(Product), product.Id.ToString(), null, product, request.OpeningStockStoreId, cancellationToken);
        return await GetByIdAsync(product.Id, request.OpeningStockStoreId, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");

        await ValidatePricesAsync(request.CategoryId, product.ProductCode, request.Barcode, id, request.PurchasePrice, request.SellingPrice, request.MRP, request.TaxPercent, cancellationToken);
        var priceChanged = product.SellingPrice != request.SellingPrice || product.PurchasePrice != request.PurchasePrice;
        var old = new { product.SellingPrice, product.PurchasePrice, product.TaxPercent };

        product.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        product.ProductName = request.ProductName.Trim();
        product.CategoryId = request.CategoryId;
        product.Unit = request.Unit.Trim().ToUpperInvariant();
        product.PurchasePrice = Money.Round(request.PurchasePrice);
        product.SellingPrice = Money.Round(request.SellingPrice);
        product.MRP = Money.Round(request.MRP);
        product.TaxPercent = request.TaxPercent;
        product.MinimumStockLevel = request.MinimumStockLevel;
        product.WeightGrams = request.WeightGrams;
        product.Metal = string.IsNullOrWhiteSpace(request.Metal) ? null : request.Metal.Trim();
        product.IsActive = request.IsActive;
        product.UpdatedDate = DateTime.UtcNow;
        product.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(priceChanged ? AuditActions.PriceChanged : AuditActions.ProductUpdated, nameof(Product), id.ToString(), old, product, null, cancellationToken);
        return await GetByIdAsync(id, null, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");
        product.IsDeleted = true;
        product.IsActive = false;
        product.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.ProductDeleted, nameof(Product), id.ToString(), null, null, null, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string query, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        int? resolved = storeId.HasValue || !_currentUser.IsAdmin ? _currentUser.Access().ResolveStoreId(storeId) : null;
        var s = query.Trim();
        var unique = s.ToUpperInvariant();
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .Where(p => !p.IsDeleted && p.IsActive && (p.ProductCode.Contains(s) || p.ProductName.Contains(s) || (p.Barcode != null && p.Barcode.Contains(s))
                || p.Units.Any(u => !u.IsDeleted && u.UniqueNumber == unique)))
            .OrderBy(p => p.ProductName)
            .Take(50)
            .ToListAsync(cancellationToken);
        return products.Select(p => Map(p, resolved)).ToList();
    }

    public async Task<ProductDto> GetByBarcodeAsync(string barcode, int? storeId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAuthenticated();
        var code = barcode.Trim();
        try
        {
            return await _units.LookupAsync(code, storeId, cancellationToken);
        }
        catch (NotFoundAppException)
        {
            // Fall through to SKU barcode lookup.
        }

        int? resolved = storeId.HasValue || !_currentUser.IsAdmin ? _currentUser.Access().ResolveStoreId(storeId) : null;
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.Barcode == code && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Product not found for barcode.");
        return Map(product, resolved);
    }

    public async Task<ImportPreviewResponse> PreviewImportAsync(Stream file, string fileName, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var rows = _excel.ReadTable(file, fileName);
        var result = await ValidateImportRowsAsync(rows, cancellationToken);
        var valid = result.Where(r => r.Item1.IsValid).Select(r => r.Item2!).ToList();
        var batch = new ProductImportBatch
        {
            BatchId = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            CreatedDate = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            Status = valid.Count > 0 && result.All(r => r.Item1.IsValid) ? "Pending" : "Invalid",
            PayloadJson = JsonSerializer.Serialize(valid),
            ValidRowCount = valid.Count,
            ErrorRowCount = result.Count(r => !r.Item1.IsValid),
            IsActive = true
        };
        _db.ProductImportBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);
        return new ImportPreviewResponse
        {
            BatchId = batch.BatchId,
            ValidRowCount = batch.ValidRowCount,
            ErrorRowCount = batch.ErrorRowCount,
            Rows = result.Select(r => r.Item1).ToList()
        };
    }

    public async Task<ImportConfirmResponse> ConfirmImportAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var batch = await _db.ProductImportBatches.FirstOrDefaultAsync(b => b.BatchId == batchId, cancellationToken)
            ?? throw new NotFoundAppException("Import batch not found.");
        if (batch.ExpiresAtUtc < DateTime.UtcNow || batch.Status != "Pending")
        {
            throw new BusinessAppException("Import batch is invalid, expired, or contains errors.");
        }

        var rows = JsonSerializer.Deserialize<List<ValidatedImportRow>>(batch.PayloadJson) ?? [];
        await using var tx = await _db.BeginTransactionAsync(cancellationToken);
        var created = 0;
        var updated = 0;
        var inventoryUpdated = 0;
        foreach (var row in rows)
        {
            var category = await _db.Categories.FirstAsync(c => c.Name == row.CategoryName && !c.IsDeleted, cancellationToken);
            var store = await _db.Stores.FirstAsync(s => s.StoreCode == row.StoreCode && !s.IsDeleted, cancellationToken);
            var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductCode == row.ProductCode && !p.IsDeleted, cancellationToken);
            if (product is null)
            {
                product = new Product
                {
                    ProductCode = row.ProductCode,
                    ProductName = row.ProductName,
                    CategoryId = category.Id,
                    Unit = row.Unit,
                    PurchasePrice = row.PurchasePrice,
                    SellingPrice = row.SellingPrice,
                    MRP = row.MRP,
                    TaxPercent = row.TaxPercent,
                    Barcode = row.Barcode,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId,
                    IsActive = true
                };
                _db.Products.Add(product);
                await _db.SaveChangesAsync(cancellationToken);
                created++;
            }
            else
            {
                product.ProductName = row.ProductName;
                product.CategoryId = category.Id;
                product.Unit = row.Unit;
                product.PurchasePrice = row.PurchasePrice;
                product.SellingPrice = row.SellingPrice;
                product.MRP = row.MRP;
                product.TaxPercent = row.TaxPercent;
                product.Barcode = row.Barcode;
                product.UpdatedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                updated++;
            }

            if (row.OpeningStock != 0)
            {
                await _stock.ChangeAsync(store.Id, product.Id, row.OpeningStock, StockMovementType.OpeningStock, product.Id, product.ProductCode, "Import opening stock", false, _currentUser.UserId, cancellationToken);
                await _units.CreateForStockIncreaseAsync(product.Id, store.Id, row.OpeningStock, cancellationToken);
                inventoryUpdated++;
            }
        }

        batch.Status = "Confirmed";
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        await _audit.LogAsync(AuditActions.ProductImported, nameof(ProductImportBatch), batch.BatchId.ToString(), null, new { created, updated }, null, cancellationToken);
        return new ImportConfirmResponse { Created = created, Updated = updated, InventoryUpdated = inventoryUpdated };
    }

    public Task<FileDownload> GetImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        return Task.FromResult(_excel.CreateProductImportTemplate());
    }

    public async Task<ProductDto> SetImageAsync(int id, string relativePath, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");
        product.ImagePath = relativePath;
        product.UpdatedDate = DateTime.UtcNow;
        product.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, null, cancellationToken);
    }

    private async Task ValidatePricesAsync(int categoryId, string productCode, string? barcode, int? id, decimal purchase, decimal selling, decimal mrp, decimal tax, CancellationToken cancellationToken)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == categoryId && !c.IsDeleted && c.IsActive, cancellationToken))
        {
            throw new ValidationAppException("Invalid category.");
        }

        if (purchase < 0 || selling < 0 || mrp < 0 || tax < 0 || tax > 100)
        {
            throw new ValidationAppException("Invalid price or tax values.");
        }

        if (await _db.Products.AnyAsync(p => p.ProductCode == productCode.Trim().ToUpperInvariant() && p.Id != id && !p.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("Product code already exists.");
        }

        if (!string.IsNullOrWhiteSpace(barcode) &&
            await _db.Products.AnyAsync(p => p.Barcode == barcode.Trim() && p.Id != id && !p.IsDeleted, cancellationToken))
        {
            throw new ConflictAppException("Barcode already exists.");
        }
    }

    private async Task<List<(ImportRowResult, ValidatedImportRow?)>> ValidateImportRowsAsync(
        IReadOnlyList<Dictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.Where(c => !c.IsDeleted).ToListAsync(cancellationToken);
        var stores = await _db.Stores.Where(s => !s.IsDeleted).ToListAsync(cancellationToken);
        var products = await _db.Products.Where(p => !p.IsDeleted).ToListAsync(cancellationToken);
        var result = new List<(ImportRowResult, ValidatedImportRow?)>();
        var codesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var barcodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var errors = new List<string>();
            string Get(params string[] keys)
            {
                foreach (var key in keys)
                {
                    var match = row.FirstOrDefault(k => k.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(match.Value))
                    {
                        return match.Value.Trim();
                    }
                }

                return string.Empty;
            }

            var code = Get("Product Code", "ProductCode");
            var name = Get("Product Name", "ProductName");
            var category = Get("Category");
            var unit = Get("Unit");
            var storeCode = Get("Store Code", "StoreCode");
            var barcode = Get("Barcode");
            if (string.IsNullOrWhiteSpace(name)) errors.Add("Product Name is required.");
            Category? matchedCategory = null;
            if (string.IsNullOrWhiteSpace(category))
            {
                errors.Add("Invalid category.");
            }
            else
            {
                matchedCategory = categories.FirstOrDefault(c => c.Name.Equals(category, StringComparison.OrdinalIgnoreCase))
                    ?? categories.FirstOrDefault(c =>
                        c.Name.StartsWith(category, StringComparison.OrdinalIgnoreCase) ||
                        category.StartsWith(c.Name, StringComparison.OrdinalIgnoreCase));
                if (matchedCategory is null)
                {
                    errors.Add("Invalid category.");
                }
            }

            if (string.IsNullOrWhiteSpace(storeCode))
            {
                storeCode = stores.Count == 1 ? stores[0].StoreCode : stores.FirstOrDefault(s => s.StoreCode == "STORE01")?.StoreCode ?? stores.FirstOrDefault()?.StoreCode ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(storeCode) || stores.All(s => !s.StoreCode.Equals(storeCode, StringComparison.OrdinalIgnoreCase)))
                errors.Add("Invalid store code.");

            var purchaseText = Get("Purchase Price", "PurchasePrice");
            var sellingText = Get("Selling Price", "SellingPrice");
            var mrpText = Get("MRP");
            if (!decimal.TryParse(sellingText, out var selling) || selling < 0) errors.Add("Invalid selling price.");
            if (!decimal.TryParse(mrpText, out var mrp) || mrp < 0)
            {
                if (decimal.TryParse(sellingText, out var sellAsMrp) && sellAsMrp >= 0)
                {
                    mrp = sellAsMrp;
                }
                else
                {
                    errors.Add("Invalid MRP.");
                }
            }

            decimal purchase;
            if (string.IsNullOrWhiteSpace(purchaseText))
            {
                purchase = selling >= 0 ? selling : mrp;
            }
            else if (!decimal.TryParse(purchaseText, out purchase) || purchase < 0)
            {
                errors.Add("Invalid purchase price.");
            }

            var taxText = Get("Tax %", "Tax%", "Tax");
            decimal tax = 3;
            if (!string.IsNullOrWhiteSpace(taxText) && (!decimal.TryParse(taxText, out tax) || tax < 0 || tax > 100))
            {
                errors.Add("Invalid tax.");
            }

            var openingText = Get("Opening Stock", "OpeningStock", "Quantity", "Qty");
            decimal opening = 0;
            if (!string.IsNullOrWhiteSpace(openingText) && (!decimal.TryParse(openingText, out opening) || opening < 0))
            {
                errors.Add("Invalid opening stock.");
            }

            var generatedCode = false;
            if (string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name) && matchedCategory is not null)
            {
                var prefix = string.IsNullOrWhiteSpace(matchedCategory.CodePrefix)
                    ? CategoryPrefixes.Suggest(matchedCategory.Name)
                    : matchedCategory.CodePrefix;
                var slug = new string(name.Where(char.IsLetterOrDigit).Take(8).ToArray()).ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(slug)) slug = "ITEM";
                var generated = $"{prefix}-{slug}";
                var n = 1;
                var candidate = generated;
                while (codesInFile.Contains(candidate) || products.Any(p => p.ProductCode.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
                {
                    n++;
                    candidate = $"{generated}-{n}";
                }

                codesInFile.Add(candidate);
                code = candidate;
                generatedCode = true;
            }
            else if (string.IsNullOrWhiteSpace(code))
            {
                errors.Add("Product Code is required.");
            }

            if (!string.IsNullOrWhiteSpace(unit) && unit.Length > 20) errors.Add("Invalid unit.");
            if (!generatedCode && !string.IsNullOrWhiteSpace(code) && !codesInFile.Add(code)) errors.Add("Duplicate product code in file.");
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                if (!barcodesInFile.Add(barcode)) errors.Add("Duplicate barcode in file.");
                if (products.Any(p => p.Barcode == barcode && !p.ProductCode.Equals(code, StringComparison.OrdinalIgnoreCase)))
                    errors.Add("Barcode already exists on another product.");
            }

            var preview = new ImportRowResult
            {
                RowNumber = i + 2,
                IsValid = errors.Count == 0,
                ProductCode = code,
                ProductName = name,
                Errors = errors
            };

            ValidatedImportRow? valid = null;
            if (preview.IsValid)
            {
                valid = new ValidatedImportRow
                {
                    RowNumber = preview.RowNumber,
                    ProductCode = code.ToUpperInvariant(),
                    ProductName = name,
                    CategoryName = matchedCategory!.Name,
                    Unit = string.IsNullOrWhiteSpace(unit) ? "PCS" : unit.ToUpperInvariant(),
                    PurchasePrice = Money.Round(purchase),
                    SellingPrice = Money.Round(selling),
                    MRP = Money.Round(mrp),
                    TaxPercent = tax,
                    OpeningStock = opening,
                    StoreCode = stores.First(s => s.StoreCode.Equals(storeCode, StringComparison.OrdinalIgnoreCase)).StoreCode,
                    Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode
                };
            }

            result.Add((preview, valid));
        }

        return result;
    }

    private ProductDto Map(Product p, int? storeId) => new()
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
        IsLowStock = (storeId == null
            ? p.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity)
            : p.Inventories.Where(i => !i.IsDeleted && i.StoreId == storeId).Select(i => i.Quantity).FirstOrDefault()) <= p.MinimumStockLevel
    };
}
