using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Catalog;

public class ProductUnitDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string UniqueNumber { get; set; } = string.Empty;
    public ProductUnitStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int? BillItemId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal MRP { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? WeightGrams { get; set; }
    public string? Metal { get; set; }
}

public class ProductUnitListRequest : Common.PagedRequest
{
    public int? ProductId { get; set; }
    public ProductUnitStatus? Status { get; set; }
}

public class ProductUnitIdsRequest
{
    public int? ProductId { get; set; }
    public int? StoreId { get; set; }
    public IReadOnlyList<int> Ids { get; set; } = [];
    public decimal WidthMm { get; set; } = 50;
    public decimal HeightMm { get; set; } = 30;
}

public class ProductUnitLabelDto
{
    public int Id { get; set; }
    public string UniqueNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal MRP { get; set; }
    public decimal SellingPrice { get; set; }
}
