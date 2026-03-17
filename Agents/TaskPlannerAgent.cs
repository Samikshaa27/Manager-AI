using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanAI.Models;
using PlanAI.Services;
using Microsoft.Extensions.Logging;

namespace PlanAI.Agents
{
    /// <summary>
    /// Agent that breaks a project description into tasks and phases.
    /// </summary>
    public class TaskPlannerAgent : IAgent
    {
        private readonly LlmService _llm;
        private readonly ILogger<TaskPlannerAgent> _logger;

        /// <inheritdoc />
        public string AgentName => "TaskPlanner";

        private static readonly Dictionary<string, List<(string Name, int Duration, PriorityLevel Priority, string Phase)>> CategoryTaskMap =
            new()
            {
                ["Solar"] = new()
                {
                    ("Site survey", 5, PriorityLevel.High, "Initiation"),
                    ("Permitting and approvals", 14, PriorityLevel.Medium, "Planning"),
                    ("Procure panels and inverters", 10, PriorityLevel.High, "Execution"),
                    ("Electrical installation", 7, PriorityLevel.Critical, "Execution"),
                    ("Commissioning and testing", 3, PriorityLevel.Medium, "Closure")
                },
                ["Software"] = new()
                {
                    ("Requirements gathering", 7, PriorityLevel.High, "Planning"),
                    ("Design architecture", 10, PriorityLevel.High, "Design"),
                    ("Implement features", 14, PriorityLevel.Critical, "Execution"),
                    ("Integration testing", 7, PriorityLevel.Medium, "Testing"),
                    ("Deployment", 3, PriorityLevel.Medium, "Release")
                },
                ["Construction"] = new()
                {
                    ("Site preparation", 10, PriorityLevel.High, "Initiation"),
                    ("Foundation works", 14, PriorityLevel.Critical, "Execution"),
                    ("Structural framing", 21, PriorityLevel.Critical, "Execution"),
                    ("Finishes", 14, PriorityLevel.Medium, "Execution"),
                    ("Handover", 5, PriorityLevel.Medium, "Closure")
                },
                ["Healthcare"] = new()
                {
                    ("Stakeholder interviews", 5, PriorityLevel.Medium, "Planning"),
                    ("Regulatory review", 14, PriorityLevel.High, "Planning"),
                    ("Procure medical equipment", 14, PriorityLevel.High, "Execution"),
                    ("Staff training", 7, PriorityLevel.Medium, "Execution"),
                    ("Clinical validation", 10, PriorityLevel.Critical, "Testing")
                },
                ["Event"] = new()
                {
                    ("Venue booking", 3, PriorityLevel.High, "Planning"),
                    ("Sponsorship outreach", 14, PriorityLevel.Medium, "Planning"),
                    ("Program scheduling", 7, PriorityLevel.High, "Execution"),
                    ("Logistics and setup", 5, PriorityLevel.Critical, "Execution"),
                    ("Event day operations", 1, PriorityLevel.Critical, "Execution")
                },
                ["Manufacturing"] = new()
                {
                    ("Process design", 10, PriorityLevel.High, "Design"),
                    ("Procure machinery", 21, PriorityLevel.High, "Planning"),
                    ("Line setup", 14, PriorityLevel.Critical, "Execution"),
                    ("Pilot production", 7, PriorityLevel.Medium, "Execution"),
                    ("Scale-up", 14, PriorityLevel.High, "Execution")
                }
            };

        public TaskPlannerAgent(LlmService llm, ILogger<TaskPlannerAgent> logger)
        {
            _llm = llm;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(ProjectContext context)
        {
            string description = context.Description ?? string.Empty;
            string category = context.Category ?? "Other";

            var tasks = new List<ProjectTask>();

            if (CategoryTaskMap.TryGetValue(category, out var template))
            {
                foreach (var t in template)
                {
                    tasks.Add(new ProjectTask
                    {
                        Name = t.Name,
                        Description = t.Name,
                        DurationDays = t.Duration,
                        Priority = t.Priority,
                        Phase = t.Phase,
                        Dependencies = new List<string>()
                    });
                }
            }
            else
            {
                tasks.Add(new ProjectTask { Name = "Kickoff", Description = "Project kickoff", DurationDays = 2, Priority = PriorityLevel.High, Phase = "Initiation" });
                tasks.Add(new ProjectTask { Name = "Delivery", Description = "Deliver solution", DurationDays = 7, Priority = PriorityLevel.Medium, Phase = "Execution" });
            }

            try
            {
                var llmTasks = await _llm.GenerateTasksAsync(description, category);
                if (llmTasks != null && llmTasks.Any())
                {
                    _logger?.LogInformation("LLM returned {count} tasks for project planning", llmTasks.Count);
                    context.AgentLog.Add($"{AgentName} → LLM successfully generated {llmTasks.Count} tailored tasks.");

                    if (llmTasks.Count >= 8)
                    {
                        // Use LLM output exclusively if it's substantial
                        tasks = llmTasks;
                    }
                    else
                    {
                        // Merge LLM tasks with template tasks, favoring LLM tasks for duplicates
                        var merged = new Dictionary<string, ProjectTask>(StringComparer.OrdinalIgnoreCase);
                        
                        // Add template tasks first
                        foreach (var t in tasks)
                        {
                            if (!string.IsNullOrWhiteSpace(t.Name))
                                merged[t.Name] = t;
                        }

                        // Overwrite or add LLM tasks
                        foreach (var lt in llmTasks)
                        {
                            if (!string.IsNullOrWhiteSpace(lt.Name))
                                merged[lt.Name] = lt;
                        }

                        tasks = merged.Values.ToList();
                        context.AgentLog.Add($"{AgentName} → Merged LLM tasks with industry templates.");
                    }
                }
                else
                {
                    _logger?.LogWarning("LLM returned no tasks, staying with template fallback for category {cat}", category);
                    context.AgentLog.Add($"{AgentName} → LLM provided no data; using optimized industry template for {category}.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LLM Task Generation failed in agent");
                context.AgentLog.Add($"{AgentName} → LLM encountered an error: {ex.Message}. Falling back to templates.");
            }

            // Group tasks into phases on the context.Plan
            context.Plan.Phases = tasks.GroupBy(t => t.Phase ?? "Uncategorized")
                .Select((g, idx) => new Phase
                {
                    Name = g.Key,
                    Order = idx + 1,
                    Tasks = g.ToList(),
                    ProgressPercent = 0
                }).OrderBy(p => p.Order).ToList();

            context.AgentLog.Add($"{AgentName} → Generated {tasks.Count} tasks");
        }
    }
}
