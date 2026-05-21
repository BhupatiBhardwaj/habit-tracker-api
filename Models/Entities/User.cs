namespace HabitTracker.Models.Entities;
    public class User
    {
        public int id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
    }
