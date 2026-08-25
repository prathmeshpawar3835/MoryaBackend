using GramShopPOS.Application.DTOs.Operations;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public static class StaffResolver
{
    public static async Task<int> ResolveSalesPersonIdAsync(
        IAppDbContext db,
        ICurrentUser currentUser,
        int storeId,
        int? requestedId,
        CancellationToken cancellationToken)
    {
        var id = requestedId ?? currentUser.UserId;
        var user = await db.Users.Include(u => u.UserRoles).ThenInclude(r => r.Role).Include(u => u.StoreUsers)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken)
            ?? throw new ValidationAppException("Sales person not found.");
        if (!user.IsActive)
        {
            throw new BusinessAppException("The selected sales person is inactive.");
        }

        var role = user.UserRoles.Select(r => r.Role.Name).FirstOrDefault();
        if (role != Roles.Admin && role != Roles.SalesPerson)
        {
            throw new ValidationAppException("Selected user is not a sales person.");
        }

        if (role == Roles.SalesPerson && user.StoreUsers.All(s => s.StoreId != storeId))
        {
            throw new ForbiddenAppException("Sales person is not assigned to this store.");
        }

        return id;
    }

    public static async Task<IReadOnlyList<SalesPersonOptionDto>> ListAsync(
        IAppDbContext db,
        ICurrentUser currentUser,
        int storeId,
        CancellationToken cancellationToken)
    {
        currentUser.Access().EnsureStoreAccess(storeId);
        return await db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive &&
                u.UserRoles.Any(r => r.Role.Name == Roles.Admin || r.Role.Name == Roles.SalesPerson) &&
                (u.UserRoles.Any(r => r.Role.Name == Roles.Admin) || u.StoreUsers.Any(s => s.StoreId == storeId)))
            .OrderBy(u => u.FullName)
            .Select(u => new SalesPersonOptionDto
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
