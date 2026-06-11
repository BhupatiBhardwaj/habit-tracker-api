using HabitTracker.Repository.Interface;
using HabitTracker.Models.Entities;
using HabitTracker;
using HabitTracker.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Repository
{
    public class HabitService : IHabitService
    {
        private readonly AppDbContext _context;
        public HabitService(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        public async Task<int> CreateHabit(CreateHabit request, int userId)
        {
            var habit = new Habit
            {
                name = request.name,
                typeid = request.TypeId,
                userid = userId,
                isdeleted = false,
                pointsperunit = request.PointsPerUnit,
                frequencytype = request.FrequencyType,
                targetcount = request.TargetCount,
                isgood = request.IsGood,
            };

            _context.habits.Add(habit);

            await _context.SaveChangesAsync();

            return habit.id;
        }

        public async Task<int> CreateCategory(string name, int userId)
        {
            var category = new Category
            {
                name = name,
                userid = userId
            };

            _context.categories.Add(category);

            await _context.SaveChangesAsync();

            return category.id;
        }

        public async Task<List<Habit>> GetAllHabitsAsync(int userId)
        {
            return await _context.habits
                .Where(x => x.userid == userId && !x.isdeleted)
                .ToListAsync();
        }

        public async Task<List<Category>> GetAllCategoriesAsync(int userId)
        {
            return await _context.categories
                .Where(x => x.userid == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdateHabitAsync(UpdateHabit request, int userId)
        {
            var habit = await _context.habits
                .FirstOrDefaultAsync(h => h.id == request.Id && h.userid == userId && !h.isdeleted);

            if (habit == null)
                return false;

            habit.name = request.name;
            habit.categoryid = request.CategoryId;
            habit.typeid = request.TypeId;
            habit.pointsperunit = request.PointsPerUnit;
            habit.frequencytype = request.FrequencyType;
            habit.targetcount = request.TargetCount;
            habit.isgood = request.isGood;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteHabitAsync(int habitId, int userId)
        {
            var habit = await _context.habits
                .FirstOrDefaultAsync(h => h.id == habitId && h.userid == userId && !h.isdeleted);

            if (habit == null)
                return false;

            habit.isdeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCategoryAsync(UpdateCategory request, int userId)
        {
            var category = await _context.categories
                .FirstOrDefaultAsync(c => c.id == request.Id && c.userid == userId);

            if (category == null)
                return false;

            category.name = request.name;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool success, string? error)> DeleteCategoryAsync(int categoryId, int userId)
        {
            var category = await _context.categories
                .FirstOrDefaultAsync(c => c.id == categoryId && c.userid == userId);

            if (category == null)
                return (false, "Category not found");

            var hasActiveHabits = await _context.habits
                .AnyAsync(h => h.userid == userId && h.categoryid == categoryId && !h.isdeleted);

            if (hasActiveHabits)
                return (false, "Cannot delete category while habits are assigned to it");

            _context.categories.Remove(category);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
