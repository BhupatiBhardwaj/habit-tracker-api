using HabitTracker.Models.DTOs;
using HabitTracker.Models.Entities;

namespace HabitTracker.Repository.Interface
{
    public interface IAuthService
    {
        Task<string> Register(RegisterRequest request);
        Task<string> Login(User user, string password);
    }
}
