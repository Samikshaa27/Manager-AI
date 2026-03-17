using System;

namespace PlanAI.Models
{
    /// <summary>
    /// Represents a project risk and suggested mitigation.
    /// </summary>
    public class Risk
    {
        /// <summary>
        /// Unique identifier for the risk.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Short title for the risk.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Detailed description of the risk.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Likelihood of the risk occurring.
        /// </summary>
        public ProbabilityLevel Probability { get; set; } = ProbabilityLevel.Medium;

        /// <summary>
        /// Severity of the impact if the risk occurs.
        /// </summary>
        public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;

        /// <summary>
        /// Owner responsible for the risk mitigation.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Note describing mitigation actions.
        /// </summary>
        public string MitigationNote { get; set; }
    }

    /// <summary>
    /// Probability levels for risks.
    /// </summary>
    public enum ProbabilityLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Severity levels for risks.
    /// </summary>
    public enum SeverityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}
