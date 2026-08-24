namespace GramShopPOS.Application.DTOs.Stores;

public class StoreDto
{
    public int Id { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? GSTNumber { get; set; }
    public string? InvoicePrefix { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateStoreRequest
{
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? GSTNumber { get; set; }
    public string? InvoicePrefix { get; set; }
}

public class UpdateStoreRequest : CreateStoreRequest
{
    public bool IsActive { get; set; } = true;
}
