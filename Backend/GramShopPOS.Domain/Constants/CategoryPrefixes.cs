namespace GramShopPOS.Domain.Constants;

public static class CategoryPrefixes
{
    public static string Suggest(string? name)
    {
        var n = (name ?? string.Empty).Trim().ToUpperInvariant();
        if (n.Contains("MANGALSUTRA") || n.Contains("MANGAL")) return "MGS";
        if (n.Contains("NOSE")) return "NSP";
        if (n.Contains("BRACELET")) return "BRC";
        if (n.Contains("NECK")) return "NCK";
        if (n.Contains("EARRING") || n.Contains("EAR RING")) return "ERG";
        if (n.Contains("CHAIN")) return "CHN";
        if (n.Contains("BANG")) return "BRC";
        if (n.Contains("RING")) return "RNG";

        var letters = new string(n.Where(char.IsLetter).Take(3).ToArray());
        if (letters.Length >= 2)
        {
            return letters.PadRight(3, 'X');
        }

        return "GEN";
    }

    public static string Normalize(string? prefix)
    {
        var value = new string((prefix ?? string.Empty).Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (value.Length is < 2 or > 8)
        {
            throw new ArgumentException("Category prefix must be 2 to 8 letters or digits.");
        }

        return value;
    }
}
