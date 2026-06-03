using HabitTracker.Helpers;
using HabitTracker.Models;
using HabitTracker.Models.DTOs;
using HabitTracker.Models.Entities;
using HabitTracker.Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Repository;

public class EntryService : IEntryService
{
    public const string DuplicateEntryMessage =
        "You already logged today's entry, either update it or first remove it to add again.";

    private readonly AppDbContext _context;

    public EntryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LogEntryResult> LogEntryAsync(LogEntryRequest request, int userId)
    {
        var habit = await _context.habits
            .FirstOrDefaultAsync(h => h.id == request.HabitId && h.userid == userId && !h.isdeleted);

        if (habit == null)
            return LogEntryResult.NotFoundResult();

        var (entry, conflict) = await SaveEntryAsync(
            habit,
            userId,
            request.EntryDate,
            request.TimeLog,
            request.IsDone,
            request.QuantityLog,
            request.EntryId);

        if (conflict)
            return LogEntryResult.Conflict(DuplicateEntryMessage);

        if (entry == null)
            return LogEntryResult.NotFoundResult();

        await _context.SaveChangesAsync();
        return LogEntryResult.Success(MapToLegacyDto(habit, entry));
    }

    public async Task<bool> DeleteEntryAsync(int entryId, int userId)
    {
        var entry = await _context.entries
            .FirstOrDefaultAsync(e => e.id == entryId && e.userid == userId);

        if (entry == null)
            return false;

        _context.entries.Remove(entry);
        await _context.SaveChangesAsync();
        return true;
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

                await SaveEntryAsync(habit, userId, request.EntryDate, item.TimeLog, item.IsDone, item.QuantityLog, null, allowOverwrite: true);
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

    public async Task<TodayDashboardDto> GetTodayDashboardAsync(int userId, DateTime? date)
    {
        var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(date);
        var (weekStart, weekEnd) = PeriodHelper.GetUtcWeekRange(date);
        var (monthStart, monthEnd) = PeriodHelper.GetUtcMonthRange(date);

        var habits = await _context.habits
            .Where(h => h.userid == userId)
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

            if (habit.frequencytype == FrequencyType.Daily && !card.IsCompletedToday && !habit.isdeleted)
                dashboard.DailyPending.Add(card);
            else if (habit.frequencytype == FrequencyType.Weekly && !habit.isdeleted)
                dashboard.WeeklyProgress.Add(card);
            else if (habit.frequencytype == FrequencyType.Monthly && !habit.isdeleted)
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

    private async Task<(Entry? entry, bool conflict)> SaveEntryAsync(
        Habit habit,
        int userId,
        DateTime? entryDate,
        decimal? timeLog,
        bool? isDone,
        decimal? quantityLog,
        int? entryId,
        bool allowOverwrite = false)
    {
        if (entryId.HasValue)
        {
            var existing = await _context.entries
                .FirstOrDefaultAsync(e => e.id == entryId.Value && e.userid == userId && e.habitid == habit.id);

            if (existing == null)
                return (null, false);

            ApplyEntryValues(habit, existing, timeLog, isDone, quantityLog);
            existing.points = CalculatePoints(habit, existing);
            return (existing, false);
        }

        var (dayStart, dayEnd) = PeriodHelper.GetUtcDayRange(entryDate);

        var entry = await _context.entries
            .FirstOrDefaultAsync(e =>
                e.userid == userId &&
                e.habitid == habit.id &&
                e.entrydate >= dayStart &&
                e.entrydate < dayEnd);

        if (entry != null)
        {
            if (!allowOverwrite)
                return (null, true);

            ApplyEntryValues(habit, entry, timeLog, isDone, quantityLog);
            entry.points = CalculatePoints(habit, entry);
            return (entry, false);
        }

        entry = new Entry
        {
            userid = userId,
            habitid = habit.id,
            entrydate = dayStart,
            points = 0
        };
        _context.entries.Add(entry);

        ApplyEntryValues(habit, entry, timeLog, isDone, quantityLog);
        entry.points = CalculatePoints(habit, entry);
        return (entry, false);
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
            Points = todayEntry?.points,
            isHabitDeleted = habit.isdeleted,
            IsGood = habit.isgood,
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
