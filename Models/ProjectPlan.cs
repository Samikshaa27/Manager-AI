using System;
using System.Collections.Generic;

namespace PlanAI.Models
{
    /// <summary>
    /// High level structured project plan produced by the orchestrator.
    /// </summary>
    public class ProjectPlan
    {
        /// <summary>
        /// Unique identifier for the plan.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }

        /// <summary>
        /// Project name.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Category detected for the project.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Original description provided by the user.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total duration estimate in days.
        /// </summary>
        public int TotalDurationDays { get; set; }

        /// <summary>
        /// Minimum estimated cost.
        /// </summary>
        public decimal EstimatedCostMin { get; set; }

        /// <summary>
        /// Maximum estimated cost.
        /// </summary>
        public decimal EstimatedCostMax { get; set; }

        /// <summary>
        /// Ordered list of phases.
        /// </summary>
        public List<Phase> Phases { get; set; } = new();

        /// <summary>
        /// Identified risks.
        /// </summary>
        public List<Risk> Risks { get; set; } = new();

        /// <summary>
        /// List of team member identifiers or names.
        /// </summary>
        public List<string> TeamMembers { get; set; } = new();

        /// <summary>
        /// Task ids that form the critical path.
        /// </summary>
        public List<Guid> CriticalPathTaskIds { get; set; } = new();

        /// <summary>
        /// Log lines from agents during generation for debugging/trace.
        /// </summary>
        public List<string> AgentLog { get; set; } = new();

        /// <summary>
        /// Resource summary produced by ResourceAgent.
        /// </summary>
        public ResourceSummary ResourceSummary { get; set; }
    }
}
