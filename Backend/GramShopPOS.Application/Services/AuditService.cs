using System.Text.Json;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Settings;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class AuditService : IAuditService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public AuditService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId,
        object? oldValue,
        object? newValue,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.IsAuthenticated ? _currentUser.UserId : null,
            StoreId = storeId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValue = SerializeSafe(oldValue),
            NewValue = SerializeSafe(newValue),
            IpAddress = _currentUser.IpAddress,
            CreatedDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<AuditLogDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        _currentUser.EnsureAdmin();
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (request.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == request.StoreId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(x => x.Action.Contains(s) || x.EntityName.Contains(s));
        }

        query = query.OrderByDescending(x => x.CreatedDate);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User != null ? x.User.UserName : null,
                StoreId = x.StoreId,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                IpAddress = x.IpAddress,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<AuditLogDto>.Create(items, request.PageNumber, request.PageSize, total);
    }

    private static string? SerializeSafe(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(value, JsonOptions);
        if (json.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("AccessToken", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            return "{\"redacted\":true}";
        }

        return json;
    }
}
