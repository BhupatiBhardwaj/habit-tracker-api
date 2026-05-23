using HabitTracker.Helpers;
using HabitTracker.Models;
using HabitTracker.Models.DTOs;
using HabitTracker.Models.Entities;
using HabitTracker.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Repository;

public class EntryService : IEntryService
{
    private readonly AppDbContext _context;

    public EntryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TodayHabitDto?> LogEntryAsync(LogEntryRequest request, int userId)
    {
        var habit = await _context.habits
            .FirstOrDefaultAsync(h => h.id == request.HabitId && h.userid == userId && !h.isdeleted);

        if (habit == null)
            return null;

        var entry = await SaveEntryAsync(habit, userId, request.EntryDate, request.TimeLog, request.IsDone, request.QuantityLog);
        await _context.SaveChangesAsync();

        return MapToLegacyDto(habit, entry);
    }

    public async Task<List<TodayHabitDto>> BulkLogAsync(BulkLogEntriesRequest request, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(request.EntryDate);

            foreach (var item in request.Items)
            {
                var habit = await _context.habits
                    .FirstOrDefaultAsync(h => h.id == item.HabitId && h.userid == userId);

                if (habit == null)
                    continue;

                if (habit.isdeleted)
                {
                    var hasEntry = await _context.entries.AnyAsync(e =>
                        e.userid == userId &&
                        e.habitid == habit.id &&
                        e.entrydate >= dayStart &&
                        e.entrydate < dayEnd);
                    if (!hasEntry)
                        continue;
                }

                await SaveEntryAsync(habit, userId, request.EntryDate, item.TimeLog, item.IsDone, item.QuantityLog);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await GetTodayAsync(request.EntryDate, userId);
    }

    public async Task<TodayDashboardDto> GetTodayDashboardAsync(int userId)
    {
        var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(null);
        var (weekStart, weekEnd) = PeriodHelper.GetUtcWeekRange(null);
        var (monthStart, monthEnd) = PeriodHelper.GetUtcMonthRange(null);

        var habits = await _context.habits
            .Where(h => h.userid == userId && !h.isdeleted)
            .ToListAsync();

        var todayEntries = await _context.entries
            .Where(e => e.userid == userId && e.entrydate >= dayStart && e.entrydate < dayEnd)
            .ToListAsync();

        var weekCounts = await _context.entries
            .Where(e => e.userid == userId && e.entrydate >= weekStart && e.entrydate < weekEnd)
            .GroupBy(e => e.habitid)
            .Select(g => new { HabitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HabitId, x => x.Count);

        var monthCounts = await _context.entries
            .Where(e => e.userid == userId && e.entrydate >= monthStart && e.entrydate < monthEnd)
            .GroupBy(e => e.habitid)
            .Select(g => new { HabitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HabitId, x => x.Count);

        var dashboard = new TodayDashboardDto();

        foreach (var habit in habits)
        {
            var todayEntry = todayEntries.FirstOrDefault(e => e.habitid == habit.id);
            var card = BuildCard(habit, todayEntry, weekCounts, monthCounts);

            if (card.IsCompletedToday)
                dashboard.CompletedToday.Add(card);

            if (habit.frequencytype == FrequencyType.Daily && !card.IsCompletedToday)
                dashboard.DailyPending.Add(card);
            else if (habit.frequencytype == FrequencyType.Weekly && !card.IsPeriodMet)
                dashboard.WeeklyProgress.Add(card);
            else if (habit.frequencytype == FrequencyType.Monthly && !card.IsPeriodMet)
                dashboard.MonthlyProgress.Add(card);
        }

        SortCards(dashboard);
        return dashboard;
    }

    public async Task<List<TodayHabitDto>> GetTodayAsync(DateTime? date, int userId)
    {
        var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(date);
        var (weekStart, weekEnd) = PeriodHelper.GetUtcWeekRange(date);
        var (monthStart, monthEnd) = PeriodHelper.GetUtcMonthRange(date);

        var entries = await _context.entries
            .Where(e => e.userid == userId && e.entrydate >= dayStart && e.entrydate < dayEnd)
            .ToListAsync();

        var habitIdsWithEntries = entries.Select(e => e.habitid).Distinct().ToList();

        var activeHabits = await _context.habits
            .Where(h => h.userid == userId && !h.isdeleted)
            .ToListAsync();

        var deletedHabitsWithEntries = await _context.habits
            .Where(h => h.userid == userId && h.isdeleted && habitIdsWithEntries.Contains(h.id))
            .ToListAsync();

        var habits = activeHabits
            .Concat(deletedHabitsWithEntries)
            .OrderBy(h => h.isdeleted)
            .ThenBy(h => h.name)
            .ToList();

        var weekCounts = await _context.entries
            .Where(e => e.userid == userId && e.entrydate >= weekStart && e.entrydate < weekEnd)
            .GroupBy(e => e.habitid)
            .Select(g => new { HabitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HabitId, x => x.Count);

        var monthCounts = await _context.entries
            .Where(e => e.userid == userId && e.entrydate >= monthStart && e.entrydate < monthEnd)
            .GroupBy(e => e.habitid)
            .Select(g => new { HabitId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HabitId, x => x.Count);

        return habits.Select(h =>
        {
            var entry = entries.FirstOrDefault(e => e.habitid == h.id);
            var card = BuildCard(h, entry, weekCounts, monthCounts);
            return MapCardToLegacyDto(card, h.isdeleted);
        }).ToList();
    }

    private async Task<Entry> SaveEntryAsync(
        Habit habit,
        int userId,
        DateTime? entryDate,
        decimal? timeLog,
        bool? isDone,
        decimal? quantityLog)
    {
        if (habit.frequencytype == FrequencyType.Daily)
            return await UpsertDailyEntryAsync(habit, userId, entryDate, timeLog, isDone, quantityLog);

        return await InsertPeriodEntryAsync(habit, userId, entryDate, timeLog, isDone, quantityLog);
    }

    private async Task<Entry> UpsertDailyEntryAsync(
        Habit habit,
        int userId,
        DateTime? entryDate,
        decimal? timeLog,
        bool? isDone,
        decimal? quantityLog)
    {
        var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(entryDate);

        var entry = await _context.entries
            .FirstOrDefaultAsync(e =>
                e.userid == userId &&
                e.habitid == habit.id &&
                e.entrydate >= dayStart &&
                e.entrydate < dayEnd);

        if (entry == null)
        {
            entry = new Entry
            {
                userid = userId,
                habitid = habit.id,
                entrydate = dayStart,
                points = 0
            };
            _context.entries.Add(entry);
        }

        ApplyEntryValues(habit, entry, timeLog, isDone, quantityLog);
        entry.points = CalculatePoints(habit, entry);
        return entry;
    }

    private async Task<Entry> InsertPeriodEntryAsync(
        Habit habit,
        int userId,
        DateTime? entryDate,
        decimal? timeLog,
        bool? isDone,
        decimal? quantityLog)
    {
        var when = entryDate.HasValue
            ? PeriodHelper.GetUtcDayRange(entryDate).start
            : DateTime.UtcNow;

        var entry = new Entry
        {
            userid = userId,
            habitid = habit.id,
            entrydate = when,
            points = 0
        };

        ApplyEntryValues(habit, entry, timeLog, isDone, quantityLog);
        entry.points = CalculatePoints(habit, entry);
        _context.entries.Add(entry);
        return entry;
    }

    private static void ApplyEntryValues(Habit habit, Entry entry, decimal? timeLog, bool? isDone, decimal? quantityLog)
    {
        switch (habit.typeid)
        {
            case 1:
                entry.timelog = timeLog;
                break;
            case 2:
                entry.isdone = isDone;
                break;
            case 3:
                entry.quantitylog = quantityLog;
                break;
        }
    }

    private static TodayHabitCardDto BuildCard(
        Habit habit,
        Entry? todayEntry,
        Dictionary<int, int> weekCounts,
        Dictionary<int, int> monthCounts)
    {
        var isCompletedToday = todayEntry != null;
        var currentProgress = habit.frequencytype switch
        {
            FrequencyType.Weekly => weekCounts.GetValueOrDefault(habit.id, 0),
            FrequencyType.Monthly => monthCounts.GetValueOrDefault(habit.id, 0),
            _ => isCompletedToday ? 1 : 0
        };

        var isPeriodMet = currentProgress >= habit.targetcount;

        return new TodayHabitCardDto
        {
            HabitId = habit.id,
            Name = habit.name,
            TypeId = habit.typeid,
            FrequencyType = habit.frequencytype,
            TargetCount = habit.targetcount,
            CurrentProgress = currentProgress,
            IsCompletedToday = isCompletedToday,
            IsPeriodMet = isPeriodMet,
            PointsPerUnit = habit.pointsperunit,
            TodayEntryId = todayEntry?.id,
            TimeLog = todayEntry?.timelog,
            IsDone = todayEntry?.isdone,
            QuantityLog = todayEntry?.quantitylog,
            Points = todayEntry?.points
        };
    }

    private static void SortCards(TodayDashboardDto dashboard)
    {
        dashboard.DailyPending = dashboard.DailyPending.OrderBy(c => c.Name).ToList();
        dashboard.WeeklyProgress = dashboard.WeeklyProgress.OrderBy(c => c.Name).ToList();
        dashboard.MonthlyProgress = dashboard.MonthlyProgress.OrderBy(c => c.Name).ToList();
        dashboard.CompletedToday = dashboard.CompletedToday.OrderBy(c => c.Name).ToList();
    }

    private static decimal CalculatePoints(Habit habit, Entry entry)
    {
        var rate = habit.pointsperunit;
        return habit.typeid switch
        {
            1 => (entry.timelog ?? 0) * rate,
            2 => entry.isdone == true ? rate : 0,
            3 => (entry.quantitylog ?? 0) * rate,
            _ => 0
        };
    }

    private static TodayHabitDto MapToLegacyDto(Habit habit, Entry entry)
    {
        return new TodayHabitDto
        {
            HabitId = habit.id,
            Name = habit.name,
            CategoryId = habit.categoryid,
            TypeId = habit.typeid,
            EntryId = entry.id,
            EntryDate = entry.entrydate,
            TimeLog = entry.timelog,
            IsDone = entry.isdone,
            QuantityLog = entry.quantitylog,
            Points = entry.points,
            IsDeleted = habit.isdeleted,
            PointsPerUnit = habit.pointsperunit,
            FrequencyType = habit.frequencytype,
            TargetCount = habit.targetcount,
            CurrentProgress = 1,
            IsCompletedToday = true,
            IsPeriodMet = true
        };
    }

    private static TodayHabitDto MapCardToLegacyDto(TodayHabitCardDto card, bool isDeleted)
    {
        return new TodayHabitDto
        {
            HabitId = card.HabitId,
            Name = card.Name,
            TypeId = card.TypeId,
            EntryId = card.TodayEntryId,
            EntryDate = card.IsCompletedToday ? DateTime.UtcNow : null,
            TimeLog = card.TimeLog,
            IsDone = card.IsDone,
            QuantityLog = card.QuantityLog,
            Points = card.Points,
            IsDeleted = isDeleted,
            PointsPerUnit = card.PointsPerUnit,
            FrequencyType = card.FrequencyType,
            TargetCount = card.TargetCount,
            CurrentProgress = card.CurrentProgress,
            IsCompletedToday = card.IsCompletedToday,
            IsPeriodMet = card.IsPeriodMet
        };
    }
}
