namespace GramShopPOS.Domain.Entities;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsUsed { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
