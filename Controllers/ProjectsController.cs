using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PlanAI.Data;
using PlanAI.Models;
using PlanAI.Services;
using PlanAI.Helpers;
using Microsoft.AspNetCore.Http;

namespace PlanAI.Controllers
{
    /// <summary>
    /// API surface for generating and retrieving project plans.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectOrchestrator _orchestrator;
        private readonly AppDbContext _db;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(ProjectOrchestrator orchestrator, AppDbContext db, ILogger<ProjectsController> logger)
        {
            _orchestrator = orchestrator;
            _db = db;
            _logger = logger;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var id)) return id;
            return null;
        }

        [HttpPost]
        [HttpPost("generate")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProjectPlan>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<ProjectPlan>>> GeneratePlan([FromBody] GenerateProjectRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
                return BadRequest(ApiResponse<object>.Fail("Description is required."));

            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var plan = await _orchestrator.GeneratePlanAsync(request);
            plan.UserId = userId;

            _db.ProjectPlans.Add(plan);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = plan.Id }, ApiResponse<ProjectPlan>.Ok(plan, "Project generated successfully"));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ProjectSummaryDto[]>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<ProjectSummaryDto[]>>> GetProjects()
        {
            var userId = GetCurrentUserId();
            var list = await _db.ProjectPlans
                .Where(p => p.UserId == userId)
                .Select(p => new ProjectSummaryDto
                {
                    Id = p.Id,
                    ProjectName = p.ProjectName,
                    Category = p.Category,
                    CreatedAt = p.CreatedAt,
                    TotalDurationDays = p.TotalDurationDays
                })
                .ToArrayAsync();

            return Ok(ApiResponse<ProjectSummaryDto[]>.Ok(list));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectPlan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProjectPlan>>> GetProject(Guid id)
        {
            var userId = GetCurrentUserId();
            var plan = await _db.ProjectPlans
                .Include(p => p.Phases)
                    .ThenInclude(ph => ph.Tasks)
                .Include(p => p.Risks)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (plan == null)
                return NotFound(ApiResponse<object>.Fail("Project not found."));

            return Ok(ApiResponse<ProjectPlan>.Ok(plan));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProjectPlan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProjectPlan>>> UpdateProject(Guid id, [FromBody] ProjectPlan dto)
        {
            var userId = GetCurrentUserId();
            var plan = await _db.ProjectPlans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (plan == null) return NotFound(ApiResponse<object>.Fail("Project not found."));
            plan.ProjectName = dto.ProjectName ?? plan.ProjectName;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<ProjectPlan>.Ok(plan));
        }

        [HttpPut("{id:guid}/tasks/{taskId:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<ProjectTask>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProjectTask>>> UpdateTaskStatus(Guid id, Guid taskId, [FromBody] UpdateTaskStatusDto dto)
        {
            var userId = GetCurrentUserId();
            var plan = await _db.ProjectPlans
                .Include(p => p.Phases)
                    .ThenInclude(ph => ph.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (plan == null)
                return NotFound(ApiResponse<object>.Fail("Project not found."));

            var phase = plan.Phases.FirstOrDefault(ph => ph.Tasks.Any(t => t.Id == taskId));
            if (phase == null)
                return NotFound(ApiResponse<object>.Fail("Phase not found."));

            var task = phase.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (!Enum.TryParse<PlanAI.Models.TaskStatus>(dto?.Status ?? string.Empty, true, out var status))
                return BadRequest(ApiResponse<object>.Fail("Invalid status value."));

            task.Status = status;

            // Recalculate Phase progress
            var totalTasks = phase.Tasks.Count;
            if (totalTasks > 0)
            {
                var completedTasks = phase.Tasks.Count(t => t.Status == PlanAI.Models.TaskStatus.Completed);
                phase.ProgressPercent = (int)Math.Round((double)completedTasks / totalTasks * 100);
            }

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<ProjectTask>.Ok(task));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var userId = GetCurrentUserId();
            var plan = await _db.ProjectPlans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (plan == null)
                return NotFound(ApiResponse<object>.Fail("Project not found."));

            _db.ProjectPlans.Remove(plan);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(null, "Project deleted."));
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProjectTask>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignTask(Guid id, [FromBody] AssignTaskRequest req)
        {
            var userId = GetCurrentUserId();
            var plan = await _db.ProjectPlans
                .Include(p => p.Phases)
                    .ThenInclude(ph => ph.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (plan == null)
                return NotFound(ApiResponse<object>.Fail("Project not found."));

            var task = plan.Phases.SelectMany(ph => ph.Tasks).FirstOrDefault(t => t.Id == req.TaskId);
            if (task == null)
                return NotFound(ApiResponse<object>.Fail("Task not found."));

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("User not found."));

            task.AssignedUserId = user.Id;
            task.AssignedTo = user.Name;

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<ProjectTask>.Ok(task));
        }

        [HttpGet("{id:guid}/dashboard")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDashboard(Guid id)
        {
            var userId = GetCurrentUserId();
            var plan = await _db.ProjectPlans
                .Include(p => p.Phases)
                    .ThenInclude(ph => ph.Tasks)
                .Include(p => p.Risks)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (plan == null)
                return NotFound(ApiResponse<object>.Fail("Project not found."));

            var allTasks = plan.Phases.SelectMany(ph => ph.Tasks).ToList();
            var completedCount = allTasks.Count(t => t.Status == PlanAI.Models.TaskStatus.Completed);
            
            // Team progress details
            var assignedUserIds = allTasks.Where(t => t.AssignedUserId.HasValue).Select(t => t.AssignedUserId.Value).Distinct().ToList();
            var teamMembers = await _db.Users.Where(u => assignedUserIds.Contains(u.Id)).ToListAsync();
            
            var teamProgress = teamMembers.Select(u => {
                var userTasks = allTasks.Where(t => t.AssignedUserId == u.Id).ToList();
                var userCompleted = userTasks.Count(t => t.Status == PlanAI.Models.TaskStatus.Completed);
                return new {
                    MemberName = u.Name,
                    AssignedTasks = userTasks.Count,
                    CompletedTasks = userCompleted,
                    ProgressPercent = userTasks.Count == 0 ? 0 : (int)Math.Round((double)userCompleted / userTasks.Count * 100)
                };
            }).ToList();

            var summary = new
            {
                ProjectName = plan.ProjectName,
                ProjectCategory = plan.Category,
                TotalTasks = allTasks.Count,
                CompletedTasks = completedCount,
                InProgressTasks = allTasks.Count(t => t.Status == PlanAI.Models.TaskStatus.InProgress),
                PendingTasks = allTasks.Count(t => t.Status == PlanAI.Models.TaskStatus.NotStarted),
                OverallProgressPercent = allTasks.Count == 0 ? 0 : (int)Math.Round((double)completedCount / allTasks.Count * 100),
                TotalDurationDays = plan.TotalDurationDays,
                EstimatedCostMin = plan.EstimatedCostMin,
                EstimatedCostMax = plan.EstimatedCostMax,
                CriticalPathTasks = allTasks.Where(t => t.IsOnCriticalPath).Select(t => t.Name).ToList(),
                TeamProgress = teamProgress,
                Risks = plan.Risks.Select(r => new { r.Title, Severity = r.Severity.ToString() }).ToList(),
                AgentLog = plan.AgentLog
            };

            return Ok(ApiResponse<object>.Ok(summary));
        }

        [HttpPost("{id:guid}/orchestrate")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProjectPlan>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ProjectPlan>>> Orchestrate(Guid id, [FromBody] GenerateProjectRequest request)
        {
            return await GeneratePlan(request);
        }

        public class AssignTaskRequest
        {
            public Guid TaskId { get; set; }
            public Guid UserId { get; set; }
        }
    }
}
