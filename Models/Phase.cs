using System;
using System.Collections.Generic;

namespace PlanAI.Models
{
    /// <summary>
    /// Represents a phase within a project plan.
    /// </summary>
    public class Phase
    {
        /// <summary>
        /// Unique identifier for the phase.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Phase name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Ordering index for the phase.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Tasks that belong to this phase.
        /// </summary>
        public List<ProjectTask> Tasks { get; set; } = new();

        /// <summary>
        /// Progress percent for the phase (0-100).
        /// </summary>
        public int ProgressPercent { get; set; }
    }
}
