using HabitTracker.Models.DTOs;

namespace HabitTracker.Repository.Interface;

public interface IEntryService
{
    Task<LogEntryResult> LogEntryAsync(LogEntryRequest request, int userId);
    Task<List<TodayHabitDto>> GetTodayAsync(DateTime? date, int userId);
    Task<TodayDashboardDto> GetTodayDashboardAsync(int userId, DateTime? date);
    Task<List<TodayHabitDto>> BulkLogAsync(BulkLogEntriesRequest request, int userId);
    Task<bool> DeleteEntryAsync(int entryId, int userId);
}
