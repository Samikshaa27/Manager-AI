using System;

namespace PlanAI.Models
{
    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ProjectPlan> Projects { get; set; } = new();
    }
}
