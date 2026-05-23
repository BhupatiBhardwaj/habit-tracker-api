using HabitTracker.Models.DTOs;

namespace HabitTracker.Repository.Interface;

public interface IEntryService
{
    Task<TodayHabitDto?> LogEntryAsync(LogEntryRequest request, int userId);
    Task<List<TodayHabitDto>> GetTodayAsync(DateTime? date, int userId);
    Task<TodayDashboardDto> GetTodayDashboardAsync(int userId);
    Task<List<TodayHabitDto>> BulkLogAsync(BulkLogEntriesRequest request, int userId);
}
