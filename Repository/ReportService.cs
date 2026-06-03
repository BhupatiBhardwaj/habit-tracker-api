using HabitTracker.Helpers;
using HabitTracker.Models;
using HabitTracker.Models.DTOs;
using HabitTracker.Models.Entities;
using HabitTracker.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Repository;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReportsSummaryDto> GetSummaryAsync(int userId, DateTime from, DateTime to)
    {
        var range = NormalizeRange(from, to);

        var habits = await _context.habits
            .Where(h => h.userid == userId && !h.isdeleted)
            .ToListAsync();

        var entryStats = await _context.entries
            .Where(e => e.userid == userId && e.entrydate >= range.from && e.entrydate < range.to)
            .GroupBy(e => e.habitid)
            .Select(g => new
            {
                HabitId = g.Key,
                Count = g.Count(),
                Points = g.Sum(x => x.points)
            })
            .ToListAsync();

        var statsByHabit = entryStats.ToDictionary(x => x.HabitId);

        var result = new ReportsSummaryDto
        {
            From = range.from,
            To = range.to,
            Habits = habits.Select(h =>
            {
                statsByHabit.TryGetValue(h.id, out var stat);
                var actual = stat?.Count ?? 0;
                var expected = PeriodHelper.CalculateExpectedTarget(h.frequencytype, h.targetcount, range.from, range.to);
                var percent = expected > 0 ? Math.Round((decimal)actual / expected * 100, 1) : 0;

                return new HabitReportSummaryDto
                {
                    HabitId = h.id,
                    Name = h.name,
                    FrequencyType = h.frequencytype,
                    TargetCount = h.targetcount,
                    ActualCount = actual,
                    ExpectedCount = expected,
                    CompletionPercent = percent,
                    TotalPoints = stat?.Points ?? 0,
                    CurrentStreak = 0
                };
            }).OrderBy(h => h.Name).ToList()
        };

        foreach (var item in result.Habits)
        {
            var habit = habits.First(h => h.id == item.HabitId);
            if (habit.isgood)
            {
                item.CurrentStreak = await CalculateGoodHabitDailyStreakAsync(userId, habit, from, to);
            }
            else
            {
                item.CurrentStreak = await CalculateBadHabitDailyStreakAsync(userId, habit, from, to);
            }
        }

        return result;
    }

    public async Task<HabitReportDetailDto?> GetHabitDetailAsync(int userId, int habitId, DateTime from, DateTime to)
    {
        var habit = await _context.habits
            .FirstOrDefaultAsync(h => h.id == habitId && h.userid == userId && !h.isdeleted);

        if (habit == null)
            return null;

        var range = NormalizeRange(from, to);

        var entries = await _context.entries
            .Where(e => e.userid == userId && e.habitid == habitId && e.entrydate >= range.from && e.entrydate < range.to)
            .OrderByDescending(e => e.entrydate)
            .ToListAsync();

        var actual = entries.Count;
        var expected = PeriodHelper.CalculateExpectedTarget(habit.frequencytype, habit.targetcount, range.from, range.to);
        var percent = expected > 0 ? Math.Round((decimal)actual / expected * 100, 1) : 0;
        var currentStreak = 0;
        if (habit.isgood)
        {
            currentStreak = await CalculateGoodHabitDailyStreakAsync(userId, habit, from, to);
        }
        else
        {
            currentStreak = await CalculateBadHabitDailyStreakAsync(userId, habit, from, to);
        }


        return new HabitReportDetailDto
        {
            HabitId = habit.id,
            Name = habit.name,
            FrequencyType = habit.frequencytype,
            TargetCount = habit.targetcount,
            ActualCount = actual,
            ExpectedCount = expected,
            CompletionPercent = percent,
            TotalPoints = entries.Sum(e => e.points),
            CurrentStreak = currentStreak,
            Entries = entries.Select(e => new ReportEntryDto
            {
                Id = e.id,
                EntryDate = e.entrydate,
                TimeLog = e.timelog,
                IsDone = e.isdone,
                QuantityLog = e.quantitylog,
                Points = Math.Abs(e.points),
            }).ToList()
        };
    }

    private async Task<int> CalculateGoodHabitDailyStreakAsync(int userId, Habit habit, DateTime from, DateTime to)
    {
        var streak = 0;
        var it = to;

        while (true)
        {
            var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(it);
            var count = await _context.entries.CountAsync(e =>
                e.userid == userId &&
                e.habitid == habit.id &&
                e.entrydate >= dayStart &&
                e.entrydate < dayEnd);

            if (count < habit.targetcount || it < from)
                break;

            streak++;
            it = it.AddDays(-1);
        }

        return streak;
    }

    private async Task<int> CalculateBadHabitDailyStreakAsync(int userId, Habit habit, DateTime from, DateTime to)
    {
        var streak = 0;
        var it = to;

        while (true)
        {
            var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(it);
            var count = await _context.entries.CountAsync(e =>
                e.userid == userId &&
                e.habitid == habit.id &&
                e.entrydate >= dayStart &&
                e.entrydate < dayEnd);

            if (count > habit.targetcount || it < from)
                break;

            streak++;
            it = it.AddDays(-1);
        }

        return streak;
    }

    private async Task<int> CalculateWeeklyStreakAsync(int userId, Habit habit)
    {
        var streak = 0;
        var cursor = DateTime.UtcNow;

        while (true)
        {
            var (weekStart, weekEnd) = PeriodHelper.GetUtcWeekRange(cursor);
            var count = await _context.entries.CountAsync(e =>
                e.userid == userId &&
                e.habitid == habit.id &&
                e.entrydate >= weekStart &&
                e.entrydate < weekEnd);

            if (count < habit.targetcount)
                break;

            streak++;
            cursor = weekStart.AddDays(-1);
        }

        return streak;
    }

    private async Task<int> CalculateMonthlyStreakAsync(int userId, Habit habit)
    {
        var streak = 0;
        var cursor = DateTime.UtcNow;

        while (true)
        {
            var (monthStart, monthEnd) = PeriodHelper.GetUtcMonthRange(cursor);
            var count = await _context.entries.CountAsync(e =>
                e.userid == userId &&
                e.habitid == habit.id &&
                e.entrydate >= monthStart &&
                e.entrydate < monthEnd);

            if (count < habit.targetcount)
                break;

            streak++;
            cursor = monthStart.AddDays(-1);
        }

        return streak;
    }

    private static (DateTime from, DateTime to) NormalizeRange(DateTime? from, DateTime? to)
    {
        var end = to.HasValue ? PeriodHelper.GetUtcDayRange(to).end : PeriodHelper.GetUtcDayRange(null).end;
        var start = from.HasValue
            ? PeriodHelper.GetUtcDayRange(from).start
            : end.AddDays(-30);

        return (start, end);
    }
}
