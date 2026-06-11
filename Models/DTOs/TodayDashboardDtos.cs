namespace HabitTracker.Models.DTOs;

public class TodayDashboardDto
{
    public List<TodayHabitCardDto> DailyPending { get; set; } = new();
    public List<TodayHabitCardDto> WeeklyProgress { get; set; } = new();
    public List<TodayHabitCardDto> MonthlyProgress { get; set; } = new();
    public List<TodayHabitCardDto> CompletedToday { get; set; } = new();
}

public class TodayHabitCardDto
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TypeId { get; set; }
    public int FrequencyType { get; set; }
    public int TargetCount { get; set; }
    public decimal CurrentProgress { get; set; }
    public bool IsCompletedToday { get; set; }
    public bool IsPeriodMet { get; set; }
    public decimal PointsPerUnit { get; set; }
    public int? TodayEntryId { get; set; }
    public decimal? TimeLog { get; set; }
    public bool? IsDone { get; set; }
    public decimal? QuantityLog { get; set; }
    public decimal? Points { get; set; }
    public bool isHabitDeleted { get; set; } = false;
    public bool IsGood { get; set; }
}
