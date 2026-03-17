using System;
using System.Collections.Generic;

namespace PlanAI.Models
{
    /// <summary>
    /// Represents a single task in a project plan.
    /// </summary>
    public class ProjectTask
    {
        /// <summary>
        /// Unique identifier for the task.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Task name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Detailed description of the task.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Estimated duration in days.
        /// </summary>
        public int DurationDays { get; set; }

        /// <summary>
        /// Priority of the task.
        /// </summary>
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        /// <summary>
        /// Phase name this task belongs to.
        /// </summary>
        public string Phase { get; set; }

        /// <summary>
        /// Identifier of the team member assigned to the task (or name).
        /// </summary>
        public string AssignedTo { get; set; }
        public Guid? AssignedUserId { get; set; }

        /// <summary>
        /// List of dependency task ids (by Id or name) as strings.
        /// </summary>
        public List<string> Dependencies { get; set; } = new();

        /// <summary>
        /// Whether the task is on the critical path.
        /// </summary>
        public bool IsOnCriticalPath { get; set; }

        /// <summary>
        /// Current status of the task.
        /// </summary>
        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;

        /// <summary>
        /// Buffer time in days.
        /// </summary>
        public int BufferDays { get; set; }
    }

    /// <summary>
    /// Priority levels for tasks.
    /// </summary>
    public enum PriorityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Status of a task.
    /// </summary>
    public enum TaskStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Blocked
    }
}
