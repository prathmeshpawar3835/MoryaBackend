using GramShopPOS.Application.Exceptions;
using GramShopPOS.Domain.Constants;

namespace GramShopPOS.Application.Services;

public class StoreAccessService
{
    private readonly IReadOnlyCollection<int> _storeIds;
    private readonly bool _isAdmin;

    public StoreAccessService(IReadOnlyCollection<int> storeIds, string role)
    {
        _storeIds = storeIds;
        _isAdmin = string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        Role = role;
    }

    public string Role { get; }
    public bool IsAdmin => _isAdmin;
    public IReadOnlyList<int> AssignedStoreIds => _storeIds.ToList();

    public void EnsureStoreAccess(int storeId)
    {
        if (_isAdmin)
        {
            return;
        }

        if (!_storeIds.Contains(storeId))
        {
            throw new ForbiddenAppException("You do not have access to the requested store.");
        }
    }

    public int ResolveStoreId(int? requestedStoreId)
    {
        if (_isAdmin)
        {
            if (requestedStoreId.HasValue)
            {
                return requestedStoreId.Value;
            }

            throw new ValidationAppException("StoreId is required.");
        }

        if (requestedStoreId.HasValue)
        {
            EnsureStoreAccess(requestedStoreId.Value);
            return requestedStoreId.Value;
        }

        if (_storeIds.Count == 1)
        {
            return _storeIds.First();
        }

        throw new ValidationAppException("StoreId is required.");
    }

    public IQueryable<T> FilterByStore<T>(IQueryable<T> query, Func<T, int> storeIdSelector)
    {
        if (_isAdmin)
        {
            return query;
        }

        return query.Where(x => _storeIds.Contains(storeIdSelector(x)));
    }

    public bool CanAccessStore(int storeId) => _isAdmin || _storeIds.Contains(storeId);
}
