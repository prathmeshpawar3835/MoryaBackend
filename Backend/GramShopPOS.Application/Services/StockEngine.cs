using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Application.Services;

public sealed class StockEngine : IStockEngine
{
    private readonly IAppDbContext _db;

    public StockEngine(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<(decimal Previous, decimal New)> ChangeAsync(
        int storeId,
        int productId,
        decimal delta,
        StockMovementType type,
        int? referenceId,
        string? referenceNumber,
        string? reason,
        bool allowNegative,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (delta == 0)
        {
            throw new ValidationAppException("Stock change quantity cannot be zero.");
        }

        var inventory = await _db.Inventories
            .FirstOrDefaultAsync(i => i.StoreId == storeId && i.ProductId == productId && !i.IsDeleted, cancellationToken);

        if (inventory is null)
        {
            if (delta < 0)
            {
                throw new InsufficientStockException("No inventory record exists for this product in the store.");
            }

            inventory = new Inventory
            {
                StoreId = storeId,
                ProductId = productId,
                Quantity = 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId
            };
            _db.Inventories.Add(inventory);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var rows = await _db.Inventories
            .Where(i => i.Id == inventory.Id && (allowNegative || i.Quantity + delta >= 0))
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Quantity, i => i.Quantity + delta)
                .SetProperty(i => i.UpdatedDate, DateTime.UtcNow)
                .SetProperty(i => i.UpdatedBy, userId), cancellationToken);

        if (rows == 0)
        {
            throw new InsufficientStockException("Insufficient stock or the stock was updated by another transaction.");
        }

        var updated = await _db.Inventories.AsNoTracking().FirstAsync(i => i.Id == inventory.Id, cancellationToken);
        var previous = updated.Quantity - delta;

        _db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            StoreId = storeId,
            Quantity = Math.Abs(delta),
            PreviousQuantity = previous,
            NewQuantity = updated.Quantity,
            MovementType = type,
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            Reason = reason,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userId,
            IsActive = true
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (previous, updated.Quantity);
    }
}
