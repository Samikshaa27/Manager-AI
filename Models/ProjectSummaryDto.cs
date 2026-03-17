using System;

namespace PlanAI.Models
{
    /// <summary>
    /// Lightweight summary returned by GET /api/projects
    /// </summary>
    public class ProjectSummaryDto
    {
        public Guid Id { get; set; }
        public string ProjectName { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalDurationDays { get; set; }
    }
}
