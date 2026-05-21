using HabitTracker.Models.Entities;
using HabitTracker.Models.DTOs;

namespace HabitTracker.Repository.Interface
{
    public interface IHabitService
    {
        Task<int> CreateHabit(CreateHabit request, int userId);
        Task<int> CreateCategory(string name, int userId);
        Task<List<Habit>> GetAllHabitsAsync(int userId);
        Task<List<Category>> GetAllCategoriesAsync(int userId);
        Task<bool> UpdateHabitAsync(UpdateHabit request, int userId);
        Task<bool> SoftDeleteHabitAsync(int habitId, int userId);
        Task<bool> UpdateCategoryAsync(UpdateCategory request, int userId);
        Task<(bool success, string? error)> DeleteCategoryAsync(int categoryId, int userId);
    }
}
