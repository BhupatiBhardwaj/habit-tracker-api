using HabitTracker.Models.DTOs;

using HabitTracker.Models.Entities;

using HabitTracker.Repository.Interface;

using Microsoft.EntityFrameworkCore;



namespace HabitTracker.Repository

{

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



            var entry = await UpsertEntryAsync(habit, userId, request.EntryDate, request.TimeLog, request.IsDone, request.QuantityLog);

            await _context.SaveChangesAsync();



            return MapToDto(habit, entry);

        }



        public async Task<List<TodayHabitDto>> BulkLogAsync(BulkLogEntriesRequest request, int userId)

        {

            await using var transaction = await _context.Database.BeginTransactionAsync();



            try

            {

                var (dayStart, dayEnd) = GetUtcDayRange(request.EntryDate);

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



                    await UpsertEntryAsync(habit, userId, request.EntryDate, item.TimeLog, item.IsDone, item.QuantityLog);

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



        public async Task<List<TodayHabitDto>> GetTodayAsync(DateTime? date, int userId)

        {

            var (dayStart, dayEnd) = GetUtcDayRange(date);



            var entries = await _context.entries

                .Where(e =>

                    e.userid == userId &&

                    e.entrydate >= dayStart &&

                    e.entrydate < dayEnd)

                .ToListAsync();



            var habitIdsWithEntries = entries.Select(e => e.habitid).Distinct().ToList();



            var activeHabits = await _context.habits

                .Where(h => h.userid == userId && !h.isdeleted)

                .ToListAsync();



            var deletedHabitsWithEntries = await _context.habits

                .Where(h =>

                    h.userid == userId &&

                    h.isdeleted &&

                    habitIdsWithEntries.Contains(h.id))

                .ToListAsync();



            var habits = activeHabits

                .Concat(deletedHabitsWithEntries)

                .OrderBy(h => h.isdeleted)

                .ThenBy(h => h.name)

                .ToList();



            return habits

                .Select(h =>

                {

                    var entry = entries.FirstOrDefault(e => e.habitid == h.id);

                    return MapToDto(h, entry);

                })

                .ToList();

        }



        private async Task<Entry> UpsertEntryAsync(

            Habit habit,

            int userId,

            DateTime? entryDate,

            decimal? timeLog,

            bool? isDone,

            decimal? quantityLog)

        {

            var (dayStart, dayEnd) = GetUtcDayRange(entryDate);



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



            entry.points = CalculatePoints(habit.typeid, entry);

            return entry;

        }



        private static (DateTime start, DateTime end) GetUtcDayRange(DateTime? date)

        {

            var value = date ?? DateTime.UtcNow;



            if (value.Kind == DateTimeKind.Unspecified)

                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);

            else

                value = value.ToUniversalTime();



            var start = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);

            return (start, start.AddDays(1));

        }



        private static decimal CalculatePoints(int typeId, Entry entry)

        {

            return typeId switch

            {

                1 => entry.timelog ?? 0,

                2 => entry.isdone == true ? 1 : 0,

                3 => entry.quantitylog ?? 0,

                _ => 0

            };

        }



        private static TodayHabitDto MapToDto(Habit habit, Entry? entry)

        {

            return new TodayHabitDto

            {

                HabitId = habit.id,

                Name = habit.name,

                CategoryId = habit.categoryid,

                TypeId = habit.typeid,

                EntryId = entry?.id,

                EntryDate = entry?.entrydate,

                TimeLog = entry?.timelog,

                IsDone = entry?.isdone,

                QuantityLog = entry?.quantitylog,

                Points = entry?.points,

                IsDeleted = habit.isdeleted

            };

        }

    }

}


