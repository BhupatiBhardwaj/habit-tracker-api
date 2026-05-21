namespace HabitTracker.Models.DTOs
{
    public class CreateHabit
    {
        public int CategoryId {  get; set; }
        public int TypeId { get; set; }
        public string name{ get; set; } = string.Empty;

    }

    public class CreateCategory
    {
        public string name { get; set; } = string.Empty;
    }
}
