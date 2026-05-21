using BCrypt.Net;
using HabitTracker.Models.DTOs;
using HabitTracker.Repository.Interface;
using HabitTracker.Models.Entities;

namespace HabitTracker.Repository;

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthService(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<string> Register(RegisterRequest rquest)
        {
            var user = new User
            {
                Name= rquest.name,
                email = rquest.email,
                password = BCrypt.Net.BCrypt.HashPassword(rquest.password)
            };

            _context.users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return token;
        }

        public async Task<string> Login(User user, string password)
        {
            bool validPassword =
                BCrypt.Net.BCrypt.Verify(password,user.password);

            if (!validPassword)
            {
                return "UnAuthorized";
            }

            var token = _jwtService.GenerateToken(user);

            return token;
        }

       
    }
