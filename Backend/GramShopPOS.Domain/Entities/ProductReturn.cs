using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Domain.Entities;

public class ProductReturn : BaseEntity
{
    public int StoreId { get; set; }
    public int OriginalBillId { get; set; }
    public string OriginalBillNumber { get; set; } = string.Empty;
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int? CustomerId { get; set; }
    public decimal ReturnAmount { get; set; }
    public string? Reason { get; set; }
    public ReturnKind ReturnKind { get; set; } = ReturnKind.Return;
    public int UserId { get; set; }
    public int? SalesPersonId { get; set; }
    public int? ExchangeBillId { get; set; }
    public int? AppliedToBillId { get; set; }

    public Store Store { get; set; } = null!;
    public Bill OriginalBill { get; set; } = null!;
    public Bill? ExchangeBill { get; set; }
    public Bill? AppliedToBill { get; set; }
    public Customer? Customer { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? SalesPerson { get; set; }
    public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
}
