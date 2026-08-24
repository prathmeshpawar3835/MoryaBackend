using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Constants;

namespace GramShopPOS.Application.Interfaces;

public static class CurrentUserExtensions
{
    public static StoreAccessService Access(this ICurrentUser user) =>
        new(user.AssignedStoreIds, user.Role);

    public static void EnsureAdmin(this ICurrentUser user)
    {
        if (!user.IsAdmin)
        {
            throw new Exceptions.ForbiddenAppException("Administrator access is required.");
        }
    }

    public static void EnsureAuthenticated(this ICurrentUser user)
    {
        if (!user.IsAuthenticated)
        {
            throw new Exceptions.UnauthorizedAppException();
        }
    }
}
