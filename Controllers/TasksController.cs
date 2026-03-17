using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanAI.Data;
using PlanAI.Models;

namespace PlanAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TasksController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var userProjectIds = await _db.ProjectPlans
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var tasks = await _db.Phases
                .Where(ph => userProjectIds.Contains(EF.Property<Guid>(ph, "ProjectPlanId")))
                .SelectMany(ph => ph.Tasks)
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("mytasks")]
        public async Task<IActionResult> MyTasks()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var myTasks = await _db.Tasks.Where(t => t.AssignedUserId == userId).ToListAsync();
            return Ok(myTasks);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            // Try to find the specified project owned by the user
            var plan = await _db.ProjectPlans.Include(p => p.Phases).FirstOrDefaultAsync(p => p.UserId == userId);
            if (plan == null)
            {
                plan = new ProjectPlan { ProjectName = "General Project", UserId = userId };
                _db.ProjectPlans.Add(plan);
                await _db.SaveChangesAsync();
            }

            var phase = plan.Phases.FirstOrDefault();
            if (phase == null)
            {
                phase = new Phase { Name = "General" };
                plan.Phases.Add(phase);
                await _db.SaveChangesAsync();
            }

            var task = new ProjectTask
            {
                Name = dto.Title,
                Description = dto.Description,
                Priority = Enum.TryParse<PriorityLevel>(dto.Priority, true, out var p) ? p : PriorityLevel.Medium,
                Status = PlanAI.Models.TaskStatus.NotStarted,
                AssignedTo = dto.AssignedUser,
                Phase = phase.Name
            };

            // If a specific user is assigned by name, try to find their ID
            if (!string.IsNullOrEmpty(dto.AssignedUser))
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == dto.AssignedUser);
                if (user != null)
                {
                    task.AssignedUserId = user.Id;
                }
            }

            phase.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return StatusCode(201, task);
        }

        [HttpPut("{id:guid}/complete")]
        public async Task<IActionResult> CompleteTask(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            if (!await CanAccessTask(id, userId)) return Unauthorized();

            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            task.Status = task.Status == PlanAI.Models.TaskStatus.Completed 
                ? PlanAI.Models.TaskStatus.InProgress 
                : PlanAI.Models.TaskStatus.Completed;

            await _db.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTask(Guid id, [FromBody] TaskUpdateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            if (!await CanAccessTask(id, userId)) return Unauthorized();

            var task = await _db.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            if (!string.IsNullOrEmpty(dto.Status))
            {
                // Simple mapping for frontend values
                var statusStr = dto.Status.ToLower().Replace(" ", "");
                if (statusStr == "todo" || statusStr == "notstarted") task.Status = PlanAI.Models.TaskStatus.NotStarted;
                else if (statusStr == "inprogress") task.Status = PlanAI.Models.TaskStatus.InProgress;
                else if (statusStr == "review") task.Status = PlanAI.Models.TaskStatus.InProgress; 
                else if (statusStr == "done" || statusStr == "completed") task.Status = PlanAI.Models.TaskStatus.Completed;
                else if (statusStr == "blocked") task.Status = PlanAI.Models.TaskStatus.Blocked;
            }

            await _db.SaveChangesAsync();
            return Ok(task);
        }

        private async Task<bool> CanAccessTask(Guid taskId, Guid userId)
        {
            return await _db.ProjectPlans
                .Where(p => p.UserId == userId)
                .AnyAsync(p => p.Phases.Any(ph => ph.Tasks.Any(t => t.Id == taskId)));
        }

        // Legacy/Alias
        [HttpGet("my-tasks")]
        public async Task<IActionResult> MyTasksLegacy() => await MyTasks();

        public class TaskCreateDto
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Priority { get; set; }
            public string AssignedUser { get; set; }
            public string DueDate { get; set; }
        }

        public class TaskUpdateDto
        {
            public string Status { get; set; }
        }
    }
}
