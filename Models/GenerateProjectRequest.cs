namespace PlanAI.Models
{
    /// <summary>
    /// Request body for project generation.
    /// </summary>
    public class GenerateProjectRequest
    {
        public string Description { get; set; }
        public decimal? BudgetMin { get; set; }
        public decimal? BudgetMax { get; set; }
        public string Currency { get; set; } = "INR";
        /// <summary>
        /// Optional list of team members provided by the requester.
        /// </summary>
        public List<TeamMemberRequest> TeamMembers { get; set; } = new();
    }

    /// <summary>
    /// Lightweight team member specification supplied in the generate request.
    /// </summary>
    public class TeamMemberRequest
    {
        public string Name { get; set; }
        public string Role { get; set; }
    }
}
