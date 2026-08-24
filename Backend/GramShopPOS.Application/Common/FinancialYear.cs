namespace GramShopPOS.Application.Common;

public static class FinancialYear
{
    public static string GetCode(DateTime utcNow, int startMonth = 4)
    {
        var local = utcNow.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(utcNow, IndiaTime())
            : utcNow;

        var year = local.Month >= startMonth ? local.Year : local.Year - 1;
        var next = year + 1;
        return $"{year % 100:00}{next % 100:00}";
    }

    public static TimeZoneInfo IndiaTime()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }
}
