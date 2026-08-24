using System.Security.Claims;
using GramShopPOS.Application.Interfaces;

namespace GramShopPOS.API.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http)
    {
        _http = http;
    }

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int UserId => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub"), out var id) ? id : 0;

    public string UserName => Principal?.Identity?.Name ?? string.Empty;

    public string Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAdmin => string.Equals(Role, Domain.Constants.Roles.Admin, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<int> AssignedStoreIds
    {
        get
        {
            var raw = Principal?.FindFirstValue("stores");
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();
        }
    }

    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? JwtId => Principal?.FindFirstValue("jti");
}

public sealed class AppEnvironment : IAppEnvironment
{
    private readonly IHostEnvironment _env;
    public AppEnvironment(IHostEnvironment env) => _env = env;
    public bool IsDevelopment => _env.IsDevelopment();
}
