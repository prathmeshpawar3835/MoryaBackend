namespace GramShopPOS.Application.Common;

public class PagedRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 20;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 200 ? 20 : value;
    }

    public string? Search { get; set; }
    public string? SortColumn { get; set; }
    public string SortDirection { get; set; } = "desc";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? StoreId { get; set; }
}
