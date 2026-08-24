using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Common;

public static class QueryablePaging
{
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(
        this IQueryable<T> query,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<T>.Create(items, request.PageNumber, request.PageSize, total);
    }

    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortColumn,
        string? sortDirection,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> map,
        string defaultKey)
    {
        var key = string.IsNullOrWhiteSpace(sortColumn) ? defaultKey : sortColumn.Trim().ToLowerInvariant();
        if (!map.TryGetValue(key, out var expr))
        {
            expr = map[defaultKey];
        }

        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return desc ? query.OrderByDescending(expr) : query.OrderBy(expr);
    }
}
