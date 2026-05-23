using HabitTracker.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HabitTracker.Controllers;

[Route("api/Reports")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("Summary")]
    public async Task<IActionResult> Summary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _reportService.GetSummaryAsync(userId, from, to);
        return Ok(result);
    }

    [HttpGet("Habit/{habitId}")]
    public async Task<IActionResult> HabitDetail(int habitId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _reportService.GetHabitDetailAsync(userId, habitId, from, to);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
