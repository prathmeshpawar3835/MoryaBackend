namespace GramShopPOS.Domain.Entities;

public class RevokedToken
{
    public int Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
    public int UserId { get; set; }
}
