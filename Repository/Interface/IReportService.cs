using HabitTracker.Models.DTOs;

namespace HabitTracker.Repository.Interface;

public interface IReportService
{
    Task<ReportsSummaryDto> GetSummaryAsync(int userId, DateTime from, DateTime to);
    Task<HabitReportDetailDto?> GetHabitDetailAsync(int userId, int habitId, DateTime from, DateTime to);
}
