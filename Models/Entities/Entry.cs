namespace HabitTracker.Models.Entities;
    public class Entry
    {
        public int id { get; set; }
        public int userid { get; set; }
        public int habitid { get; set; }
        public DateTime entrydate { get; set; }
        public decimal? timelog { get; set; }
        public bool? isdone { get; set; }
        public decimal? quantitylog { get; set; }
        public decimal points { get; set; }
    }
