namespace GramShopPOS.Application.Common;

public static class BusinessCalendar
{
    public static DateOnly Today()
    {
        var utc = DateTime.Now;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, tz));
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(utc.AddHours(5.5));
        }
    }

    public static bool IsBirthdayToday(DateOnly? dateOfBirth) => IsBirthdayOn(dateOfBirth, Today());

    public static bool IsBirthdayOn(DateOnly? dateOfBirth, DateOnly onDate)
    {
        if (dateOfBirth is null)
        {
            return false;
        }

        var dob = dateOfBirth.Value;
        if (dob.Month == onDate.Month && dob.Day == onDate.Day)
        {
            return true;
        }

        return dob is { Month: 2, Day: 29 }
            && onDate is { Month: 2, Day: 28 }
            && !DateTime.IsLeapYear(onDate.Year);
    }

    public static void EnsureValidDateOfBirth(DateOnly? dateOfBirth)
    {
        if (dateOfBirth is null)
        {
            return;
        }

        var today = Today();
        if (dateOfBirth.Value > today)
        {
            throw new Exceptions.ValidationAppException("Date of birth cannot be in the future.");
        }

        if (dateOfBirth.Value < today.AddYears(-120))
        {
            throw new Exceptions.ValidationAppException("Date of birth is not valid.");
        }
    }
}
