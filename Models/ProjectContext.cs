using System.Collections.Generic;

namespace PlanAI.Models
{
    /// <summary>
    /// Shared context that agents operate on during plan generation.
    /// </summary>
    public class ProjectContext
    {
        /// <summary>
        /// User provided project description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Detected project category.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Working project plan that agents progressively populate.
        /// </summary>
        public ProjectPlan Plan { get; set; }

        /// <summary>
        /// Log messages added by agents during execution.
        /// </summary>
        public List<string> AgentLog { get; set; }

        /// <summary>
        /// Creates a new ProjectContext with initialized plan and log.
        /// </summary>
        public ProjectContext()
        {
            Plan = new ProjectPlan();
            AgentLog = new List<string>();
        }

        /// <summary>
        /// Optional team members supplied with the request.
        /// </summary>
        public List<TeamMemberRequest> TeamMembers { get; set; } = new();

        /// <summary>
        /// Optional budget constraints provided by the user.
        /// </summary>
        public decimal? BudgetMin { get; set; }

        public decimal? BudgetMax { get; set; }

        public string Currency { get; set; } = "INR";
    }
}
