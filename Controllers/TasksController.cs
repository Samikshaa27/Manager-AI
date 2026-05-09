using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanAI.Data;
using PlanAI.Models;
using PlanAI.Helpers;
using Microsoft.AspNetCore.Http;

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
        [ProducesResponseType(typeof(ApiResponse<List<ProjectTask>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTasks()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var userProjectIds = await _db.ProjectPlans
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var tasks = await _db.Phases
                .Where(ph => userProjectIds.Contains(EF.Property<Guid>(ph, "ProjectPlanId")))
                .SelectMany(ph => ph.Tasks)
                .ToListAsync();

            return Ok(ApiResponse<List<ProjectTask>>.Ok(tasks));
        }

        [HttpGet("mytasks")]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectTask>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MyTasks()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var myTasks = await _db.Tasks.Where(t => t.AssignedUserId == userId).ToListAsync();
            return Ok(ApiResponse<List<ProjectTask>>.Ok(myTasks));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProjectTask>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

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
                _db.Phases.Add(phase);
                plan.Phases.Add(phase);
                await _db.SaveChangesAsync();
            }

            var task = new ProjectTask
            {
                Id = Guid.NewGuid(),
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

            _db.Tasks.Add(task);
            phase.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return StatusCode(201, ApiResponse<ProjectTask>.Ok(task, "Task created"));
        }

        [HttpPut("{id:guid}/complete")]
        [ProducesResponseType(typeof(ApiResponse<ProjectTask>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteTask(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            if (!await CanAccessTask(id, userId)) return Unauthorized(ApiResponse<object>.Fail("Cannot access this task"));

            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return NotFound(ApiResponse<object>.Fail("Task not found"));

            task.Status = task.Status == PlanAI.Models.TaskStatus.Completed 
                ? PlanAI.Models.TaskStatus.InProgress 
                : PlanAI.Models.TaskStatus.Completed;

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<ProjectTask>.Ok(task));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectTask>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTask(Guid id, [FromBody] TaskUpdateDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            if (!await CanAccessTask(id, userId)) return Unauthorized(ApiResponse<object>.Fail("Cannot access task"));

            var task = await _db.Tasks.FindAsync(id);
            if (task == null)
                return NotFound(ApiResponse<object>.Fail("Task not found"));

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
            return Ok(ApiResponse<ProjectTask>.Ok(task));
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
            [Required]
            public string Title { get; set; }
            public string Description { get; set; }
            public string Priority { get; set; }
            public string AssignedUser { get; set; }
            
            [FutureDate(ErrorMessage = "DueDate must be in the future")]
            public string DueDate { get; set; }
        }

        public class TaskUpdateDto
        {
            public string Status { get; set; }
        }
    }
}
