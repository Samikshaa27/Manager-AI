using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanAI.Data;

namespace PlanAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            // Get all project IDs owned by the user
            var userProjectIds = await _db.ProjectPlans
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            // Get all tasks associated with these projects
            var allTasks = await _db.Phases
                .Where(ph => userProjectIds.Contains(EF.Property<Guid>(ph, "ProjectPlanId")))
                .SelectMany(ph => ph.Tasks)
                .ToListAsync();
            
            var totalTasks = allTasks.Count;
            var completedTasks = allTasks.Count(t => t.Status == PlanAI.Models.TaskStatus.Completed);
            var inProgressTasks = allTasks.Count(t => t.Status == PlanAI.Models.TaskStatus.InProgress);
            
            var overdueTasks = allTasks.Count(t => t.Status == PlanAI.Models.TaskStatus.Blocked);

            return Ok(new
            {
                totalTasks,
                completedTasks,
                inProgressTasks,
                overdueTasks
            });
        }
    }
}
