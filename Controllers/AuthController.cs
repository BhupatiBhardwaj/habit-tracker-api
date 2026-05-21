using HabitTracker.Repository;
using HabitTracker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HabitTracker.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using HabitTracker.Repository.Interface;
using Microsoft.AspNetCore.Authorization;

namespace HabitTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IAuthService _authService;

        public AuthController(AppDbContext context, JwtService jwtService, IAuthService authService)
        {
            _context = context;
            _jwtService = jwtService;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(RegisterRequest request)
        {
            bool exist = await _context.users.AnyAsync(x => x.email ==  request.email);
            if (exist)
            {
                return BadRequest("User already exist");
            }

            string token = await _authService.Register(request);

            return Ok(new AuthResponse
            {
                Token = token
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> LogIn(LoginRequest request) 
        {
            var user = await _context.users.FirstOrDefaultAsync(x => x.email == request.email);
            if(user == null)
            {
                return Unauthorized();
            }

            string token = await _authService.Login(user, request.password);

            return Ok(new AuthResponse
            {
                Token = token
            });
        }
    }
}
