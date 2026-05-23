namespace HabitTracker.Models.DTOs
{
    public class CreateHabit
    {
        public int CategoryId {  get; set; }
        public int TypeId { get; set; }
        public string name{ get; set; } = string.Empty;
        public decimal PointsPerUnit { get; set; } = 1;
        public int FrequencyType { get; set; } = 1;
        public int TargetCount { get; set; } = 1;

    }

    public class CreateCategory
    {
        public string name { get; set; } = string.Empty;
    }
}
