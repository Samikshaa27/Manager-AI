using System;

namespace PlanAI.Models
{
    /// <summary>
    /// Represents a team member who can be assigned tasks.
    /// </summary>
    public class TeamMember
    {
        /// <summary>
        /// Unique identifier for the team member.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Full name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Primary role or skill.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Hourly rate for cost estimation.
        /// </summary>
        public decimal HourlyRate { get; set; }

        /// <summary>
        /// Maximum working hours per day for the team member (e.g. 8).
        /// </summary>
        public int MaxDailyHours { get; set; } = 8;
    }
}
