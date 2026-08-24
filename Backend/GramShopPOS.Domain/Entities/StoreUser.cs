namespace GramShopPOS.Domain.Entities;

public class StoreUser
{
    public int StoreId { get; set; }
    public int UserId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedDate { get; set; }

    public Store Store { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
