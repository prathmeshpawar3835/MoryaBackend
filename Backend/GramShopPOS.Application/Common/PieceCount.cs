namespace GramShopPOS.Application.Common;

public static class PieceCount
{
    public static bool TryGet(string? unit, decimal quantity, out int count)
    {
        count = 0;
        var u = (unit ?? "PCS").Trim().ToUpperInvariant();
        if (u is not ("PCS" or "PC" or "PIECE" or "PIECES" or "NOS" or "NO"))
        {
            return false;
        }

        if (quantity <= 0 || quantity != Math.Truncate(quantity) || quantity > 100_000)
        {
            return false;
        }

        count = (int)quantity;
        return true;
    }
}
