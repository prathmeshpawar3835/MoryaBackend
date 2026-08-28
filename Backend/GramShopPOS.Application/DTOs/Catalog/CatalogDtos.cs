using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Catalog;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CodePrefix { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? CodePrefix { get; set; }
    public string? Description { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest
{
    public bool IsActive { get; set; } = true;
}

public class ProductDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public string? ImagePath { get; set; }
    public string ImageUrl { get; set; } = "/images/default-jewellery.svg";
    public decimal? WeightGrams { get; set; }
    public string? Metal { get; set; }
    public bool IsActive { get; set; }
    public decimal? StockQuantity { get; set; }
    public bool IsLowStock { get; set; }
    public int? ProductUnitId { get; set; }
    public string? UniqueNumber { get; set; }
    public ProductUnitStatus? ProductUnitStatus { get; set; }
}

public class CreateProductRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Unit { get; set; } = "PCS";
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal? WeightGrams { get; set; }
    public string? Metal { get; set; }
    public int? OpeningStockStoreId { get; set; }
    public decimal OpeningStock { get; set; }
}

public class UpdateProductRequest
{
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Unit { get; set; } = "PCS";
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal? WeightGrams { get; set; }
    public string? Metal { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductListRequest : Common.PagedRequest
{
    public int? CategoryId { get; set; }
    public bool? LowStockOnly { get; set; }
}

public class ImportPreviewResponse
{
    public Guid BatchId { get; set; }
    public int ValidRowCount { get; set; }
    public int ErrorRowCount { get; set; }
    public IReadOnlyList<ImportRowResult> Rows { get; set; } = [];
}

public class ImportRowResult
{
    public int RowNumber { get; set; }
    public bool IsValid { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
}

public class ImportConfirmResponse
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int InventoryUpdated { get; set; }
}

public class ValidatedImportRow
{
    public int RowNumber { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = "PCS";
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal OpeningStock { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
}
