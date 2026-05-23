using HabitTracker.Models.DTOs;
using HabitTracker.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HabitTracker.Controllers
{
    [Route("api/Entry")]
    [ApiController]
    [Authorize]
    public class EntryController : ControllerBase
    {
        private readonly IEntryService _entryService;

        public EntryController(IEntryService entryService)
        {
            _entryService = entryService;
        }

        [HttpPost("Log")]
        public async Task<ActionResult<TodayHabitDto>> Log(LogEntryRequest request)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _entryService.LogEntryAsync(request, userId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("Today")]
        public async Task<ActionResult<TodayDashboardDto>> Today()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _entryService.GetTodayDashboardAsync(userId);
            return Ok(result);
        }

        [HttpGet("GetByDate")]
        public async Task<ActionResult<List<TodayHabitDto>>> GetByDate([FromQuery] DateTime? date)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _entryService.GetTodayAsync(date, userId);
            return Ok(result);
        }

        [HttpPost("BulkLog")]
        public async Task<ActionResult<List<TodayHabitDto>>> BulkLog(BulkLogEntriesRequest request)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _entryService.BulkLogAsync(request, userId);
            return Ok(result);
        }
    }
}
