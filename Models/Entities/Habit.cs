using System.ComponentModel.DataAnnotations.Schema;

namespace HabitTracker.Models.Entities;
    public class Habit
    {
        public int id { get; set; }
        public int userid { get; set; }
        public string name{ get; set; } = string.Empty;
        public int categoryid { get; set; }
        public int typeid { get; set; }

        [Column("is_deleted")]
        public bool isdeleted { get; set; }
    }

