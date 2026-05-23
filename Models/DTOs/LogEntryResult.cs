namespace HabitTracker.Models.DTOs;

public class LogEntryResult
{
    public TodayHabitDto? Data { get; init; }
    public bool IsConflict { get; init; }
    public bool NotFound { get; init; }
    public string? Message { get; init; }

    public static LogEntryResult Success(TodayHabitDto data) => new() { Data = data };
    public static LogEntryResult Conflict(string message) => new() { IsConflict = true, Message = message };
    public static LogEntryResult NotFoundResult() => new() { NotFound = true };
}
