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

        public const string DuplicateEntryMessage =
            "You already logged today's entry, either update it or first remove it to add again.";

        [HttpPost("Log")]
        public async Task<ActionResult<TodayHabitDto>> Log(LogEntryRequest request)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _entryService.LogEntryAsync(request, userId);

            if (result.IsConflict)
                return Conflict(new { message = result.Message ?? DuplicateEntryMessage });

            if (result.NotFound)
                return NotFound();

            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var deleted = await _entryService.DeleteEntryAsync(id, userId);
            if (!deleted)
                return NotFound();
            return NoContent();
        }

        [HttpGet("Today")]
        public async Task<ActionResult<TodayDashboardDto>> Today([FromQuery] DateTime? date)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _entryService.GetTodayDashboardAsync(userId, date);
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
