namespace HabitTracker.Models.DTOs;

public class HabitReportSummaryDto
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FrequencyType { get; set; }
    public int TargetCount { get; set; }
    public int ActualCount { get; set; }
    public int ExpectedCount { get; set; }
    public decimal CompletionPercent { get; set; }
    public decimal TotalPoints { get; set; }
    public int CurrentStreak { get; set; }
}

public class ReportsSummaryDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<HabitReportSummaryDto> Habits { get; set; } = new();
}

public class HabitReportDetailDto
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FrequencyType { get; set; }
    public int TargetCount { get; set; }
    public int ActualCount { get; set; }
    public int ExpectedCount { get; set; }
    public decimal CompletionPercent { get; set; }
    public decimal TotalPoints { get; set; }
    public int CurrentStreak { get; set; }
    public List<ReportEntryDto> Entries { get; set; } = new();
}

public class ReportEntryDto
{
    public int Id { get; set; }
    public DateTime EntryDate { get; set; }
    public decimal? TimeLog { get; set; }
    public bool? IsDone { get; set; }
    public decimal? QuantityLog { get; set; }
    public decimal Points { get; set; }
}
