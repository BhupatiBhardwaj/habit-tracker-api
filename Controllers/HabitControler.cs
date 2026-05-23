using HabitTracker.Models.DTOs;

using HabitTracker.Models.Entities;

using HabitTracker.Repository.Interface;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;



namespace HabitTracker.Controllers

{

    [Route("api/Habit")]

    [ApiController]

    [Authorize]

    public class HabitControler : ControllerBase

    {

        private readonly IHabitService _habitService;



        public HabitControler(IHabitService habitService)

        {

            _habitService = habitService;

        }



        [HttpPost("CreateCategory")]

        public async Task<ActionResult<int>> CreateCategory(CreateCategory request)

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            int id = await _habitService.CreateCategory(request.name, userId);

            return Ok(id);

        }



        [HttpPost("CreateHabit")]

        public async Task<ActionResult<int>> CreateHabit(CreateHabit habit)

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            int id = await _habitService.CreateHabit(habit, userId);

            return Ok(id);

        }



        [HttpGet("GetAllHabits")]

        public async Task<ActionResult<List<Habit>>> GetAllHabits()

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            List<Habit> allHabits = await _habitService.GetAllHabitsAsync(userId);

            return Ok(allHabits);

        }



        [HttpGet("GetAllCategories")]

        public async Task<ActionResult<List<Category>>> GetAllCategories()

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            List<Category> allcategories = await _habitService.GetAllCategoriesAsync(userId);

            return Ok(allcategories);

        }



        [HttpPost("UpdateHabit")]

        public async Task<IActionResult> UpdateHabit(UpdateHabit request)

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var ok = await _habitService.UpdateHabitAsync(request, userId);

            if (!ok) return NotFound();

            return Ok();

        }



        [HttpPost("SoftDeleteHabit")]

        public async Task<IActionResult> SoftDeleteHabit(DeleteHabitRequest request)

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var ok = await _habitService.SoftDeleteHabitAsync(request.Id, userId);

            if (!ok) return NotFound();

            return Ok();

        }



        [HttpPost("UpdateCategory")]

        public async Task<IActionResult> UpdateCategory(UpdateCategory request)

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var ok = await _habitService.UpdateCategoryAsync(request, userId);

            if (!ok) return NotFound();

            return Ok();

        }



        [HttpPost("DeleteCategory")]

        public async Task<IActionResult> DeleteCategory(DeleteCategoryRequest request)

        {

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var (success, error) = await _habitService.DeleteCategoryAsync(request.Id, userId);

            if (!success && error == "Category not found") return NotFound();

            if (!success) return Conflict(new { message = error });

            return Ok();

        }

    }

}


