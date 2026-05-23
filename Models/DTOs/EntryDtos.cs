namespace HabitTracker.Models.DTOs
{
    public class LogEntryRequest
    {
        public int HabitId { get; set; }
        public DateTime? EntryDate { get; set; }
        public decimal? TimeLog { get; set; }
        public bool? IsDone { get; set; }
        public decimal? QuantityLog { get; set; }
    }

    public class TodayHabitDto
    {
        public int HabitId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int TypeId { get; set; }
        public int? EntryId { get; set; }
        public DateTime? EntryDate { get; set; }
        public decimal? TimeLog { get; set; }
        public bool? IsDone { get; set; }
        public decimal? QuantityLog { get; set; }
        public decimal? Points { get; set; }
        public bool IsDeleted { get; set; }
        public decimal PointsPerUnit { get; set; }
        public int FrequencyType { get; set; }
        public int TargetCount { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsCompletedToday { get; set; }
        public bool IsPeriodMet { get; set; }
    }

    public class BulkLogEntryItem
    {
        public int HabitId { get; set; }
        public decimal? TimeLog { get; set; }
        public bool? IsDone { get; set; }
        public decimal? QuantityLog { get; set; }
    }

    public class BulkLogEntriesRequest
    {
        public DateTime? EntryDate { get; set; }
        public List<BulkLogEntryItem> Items { get; set; } = new();
    }
}
