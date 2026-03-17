using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanAI.Data;
using PlanAI.Models;

namespace PlanAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SeedController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            // Create Demo Manager
            var managerEmail = "manager@managerai.com";
            AppUser manager = await _db.Users.FirstOrDefaultAsync(u => u.Email == managerEmail);
            if (manager == null)
            {
                manager = new AppUser
                {
                    Name = "Demo Manager",
                    Email = managerEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "ProjectManager"
                };
                _db.Users.Add(manager);
                await _db.SaveChangesAsync();
            }

            // Create Demo Member
            var memberEmail = "member@managerai.com";
            if (!await _db.Users.AnyAsync(u => u.Email == memberEmail))
            {
                _db.Users.Add(new AppUser
                {
                    Name = "Demo Member",
                    Email = memberEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "TeamMember"
                });
            }

            // Create a default project and phase if none exists
            if (!await _db.ProjectPlans.AnyAsync())
            {
                var plan = new ProjectPlan 
                { 
                    ProjectName = "Enterprise CRM",
                    UserId = manager.Id
                };
                var phase = new Phase { Name = "Execution" };
                phase.Tasks.Add(new ProjectTask { Name = "Initial Setup", Description = "Setup the core infrastructure", Priority = PriorityLevel.High, Status = PlanAI.Models.TaskStatus.Completed });
                phase.Tasks.Add(new ProjectTask { Name = "Develop UI", Description = "Build the React components", Priority = PriorityLevel.Medium, Status = PlanAI.Models.TaskStatus.InProgress });
                phase.Tasks.Add(new ProjectTask { Name = "API Integration", Description = "Connect with backend", Priority = PriorityLevel.High, Status = PlanAI.Models.TaskStatus.NotStarted });
                plan.Phases.Add(phase);
                _db.ProjectPlans.Add(plan);
            }

            await _db.SaveChangesAsync();
            return Ok("Database seeded with demo accounts: manager@managerai.com and member@managerai.com (password: password123)");
        }
    }
}
