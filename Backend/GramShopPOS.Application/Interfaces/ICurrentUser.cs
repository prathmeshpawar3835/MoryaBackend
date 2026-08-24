namespace GramShopPOS.Application.Interfaces;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    string UserName { get; }
    string Role { get; }
    bool IsAdmin { get; }
    IReadOnlyList<int> AssignedStoreIds { get; }
    string? IpAddress { get; }
    string? JwtId { get; }
}
