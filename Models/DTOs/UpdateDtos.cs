namespace HabitTracker.Models.DTOs
{
    public class UpdateHabit
    {
        public int Id { get; set; }
        public string name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int TypeId { get; set; }
    }

    public class UpdateCategory
    {
        public int Id { get; set; }
        public string name { get; set; } = string.Empty;
    }

    public class DeleteHabitRequest
    {
        public int Id { get; set; }
    }

    public class DeleteCategoryRequest
    {
        public int Id { get; set; }
    }
}
