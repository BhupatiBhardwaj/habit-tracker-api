using System.ComponentModel.DataAnnotations.Schema;

namespace HabitTracker.Models.Entities;
    public class Habit
    {
        public int id { get; set; }
        public int userid { get; set; }
        public string name{ get; set; } = string.Empty;
        public int? categoryid { get; set; }
        public int typeid { get; set; }

        [Column("is_deleted")]
        public bool isdeleted { get; set; }

        [Column("pointsperunit")]
        public decimal pointsperunit { get; set; } = 1;

        [Column("frequencytype")]
        public int frequencytype { get; set; } = 1;

        [Column("targetcount")]
        public int targetcount { get; set; } = 1;
    }

