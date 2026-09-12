using GramShopPOS.Application.Common;
using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Ecommerce;

public class ShopInfoDto
{
    public string ShopName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? Address { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
}

public class ShopCatalogRequest : PagedRequest
{
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
}

public class ShopProductDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string? ImagePath { get; set; }
    public string ImageUrl { get; set; } = "/images/default-jewellery.svg";
    public decimal? WeightGrams { get; set; }
    public string? Metal { get; set; }
    public decimal StockQuantity { get; set; }
    public bool InStock { get; set; }
}

public class ShopProductDetailDto : ShopProductDto
{
    public string? Description { get; set; }
    public decimal TaxPercent { get; set; }
    public IReadOnlyList<ShopProductDto> Related { get; set; } = [];
}

public class ShopOfferDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountKind DiscountKind { get; set; }
    public decimal Value { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class ShopRegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class ShopLoginRequest
{
    public string Mobile { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ShopAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public ShopCustomerDto Customer { get; set; } = new();
}

public class ShopCustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
}

public class ShopCheckoutItemRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class ShopCheckoutRequest
{
    public IReadOnlyList<ShopCheckoutItemRequest> Items { get; set; } = [];
    public int? OfferId { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Upi;
    public string? PaymentReference { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public class ShopOrderItemDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
}

public class ShopOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public BillStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public string? DiscountName { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string? PaymentMode { get; set; }
    public string? Notes { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerMobile { get; set; }
    public string? CustomerAddress { get; set; }
    public IReadOnlyList<ShopOrderItemDto> Items { get; set; } = [];
}

public class ShopCheckoutResponse
{
    public ShopOrderDto Order { get; set; } = new();
    public bool NotificationSent { get; set; }
    public string? NotificationMessage { get; set; }
}
