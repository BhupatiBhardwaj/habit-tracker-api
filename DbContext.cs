using Microsoft.EntityFrameworkCore;
using HabitTracker.Models.Entities;

namespace HabitTracker;

    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<User> users { get; set; }
        public DbSet<Category> categories { get; set; }
        public DbSet<Habit> habits { get; set; }
        public DbSet<Entry> entries { get; set; }
    }

