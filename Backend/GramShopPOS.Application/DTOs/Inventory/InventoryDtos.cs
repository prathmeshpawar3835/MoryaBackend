using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Inventory;

public class InventoryDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public bool IsLowStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
}

public class InventoryListRequest : Common.PagedRequest
{
    public int? ProductId { get; set; }
    public bool? LowStockOnly { get; set; }
}

public class StockInRequest
{
    public int StoreId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Reason { get; set; }
    public string? SupplierName { get; set; }
    public string? InvoiceNumber { get; set; }
}

public class StockAdjustRequest
{
    public int StoreId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public bool IsIncrease { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class StockTransferRequest
{
    public int FromStoreId { get; set; }
    public int ToStoreId { get; set; }
    public string? Reason { get; set; }
    public IReadOnlyList<StockTransferItemRequest> Items { get; set; } = [];
}

public class StockTransferItemRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class StockMovementDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class PurchaseDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<PurchaseItemDto> Items { get; set; } = [];
}

public class PurchaseItemDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Total { get; set; }
}

public class CreatePurchaseRequest
{
    public int StoreId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<CreatePurchaseItemRequest> Items { get; set; } = [];
}

public class CreatePurchaseItemRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
}
