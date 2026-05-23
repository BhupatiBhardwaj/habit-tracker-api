using HabitTracker.Models;

namespace HabitTracker.Helpers;

public static class PeriodHelper
{
    public static (DateTime start, DateTime end) GetUtcDayRange(DateTime? date)
    {
        var value = NormalizeUtc(date ?? DateTime.UtcNow);
        var start = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddDays(1));
    }

    public static (DateTime start, DateTime end) GetUtcWeekRange(DateTime? date)
    {
        var value = NormalizeUtc(date ?? DateTime.UtcNow).Date;
        var diff = (7 + (value.DayOfWeek - DayOfWeek.Monday)) % 7;
        var start = value.AddDays(-diff);
        start = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddDays(7));
    }

    public static (DateTime start, DateTime end) GetUtcMonthRange(DateTime? date)
    {
        var value = NormalizeUtc(date ?? DateTime.UtcNow);
        var start = new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }

    public static int CountDaysInRange(DateTime from, DateTime to)
    {
        return (int)(to.Date - from.Date).TotalDays;
    }

    public static int CountIsoWeeksOverlapping(DateTime from, DateTime to)
    {
        var count = 0;
        var cursor = from;
        while (cursor < to)
        {
            var (_, weekEnd) = GetUtcWeekRange(cursor);
            count++;
            cursor = weekEnd;
        }
        return Math.Max(count, 1);
    }

    public static int CountMonthsOverlapping(DateTime from, DateTime to)
    {
        var count = 0;
        var cursor = from;
        while (cursor < to)
        {
            var (_, monthEnd) = GetUtcMonthRange(cursor);
            count++;
            cursor = monthEnd;
        }
        return Math.Max(count, 1);
    }

    public static int CalculateExpectedTarget(int frequencyType, int targetCount, DateTime from, DateTime to)
    {
        var days = CountDaysInRange(from, to);
        return frequencyType switch
        {
            FrequencyType.Daily => days * targetCount,
            FrequencyType.Weekly => CountIsoWeeksOverlapping(from, to) * targetCount,
            FrequencyType.Monthly => CountMonthsOverlapping(from, to) * targetCount,
            _ => days * targetCount
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return value.ToUniversalTime();
    }
}
