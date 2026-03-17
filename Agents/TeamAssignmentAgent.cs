using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanAI.Models;

namespace PlanAI.Agents
{
    /// <summary>
    /// Agent that assigns tasks to team members based on skills and availability.
    /// </summary>
    public class TeamAssignmentAgent : IAgent
    {
        /// <inheritdoc />
        public string AgentName => "TeamAssignment";

        /// <inheritdoc />
        public Task ExecuteAsync(ProjectContext context)
        {
            var plan = context.Plan;
            var tasks = plan.Phases?.SelectMany(ph => ph.Tasks).ToList() ?? new List<ProjectTask>();

            // Use provided team members from the request if any
            var provided = context.TeamMembers ?? new List<TeamMemberRequest>();

            var roleKeywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Developer"] = new[] { "app", "api", "software", "implement", "deploy", "integration" },
                ["Engineer"] = new[] { "install", "installation", "site", "construction", "assembly", "electrical", "structural" },
                ["QA"] = new[] { "test", "testing", "validation", "qa", "integration" },
                ["PM"] = new[] { "kickoff", "handover", "plan", "schedule", "coordinator" }
            };

            if (provided.Any())
            {
                var assignedHours = provided.ToDictionary(m => m.Name, m => 0);

                foreach (var task in tasks)
                {
                    string bestMember = provided.First().Name;
                    int bestScore = -1;

                    foreach (var member in provided)
                    {
                        int score = 0;
                        if (roleKeywords.TryGetValue(member.Role, out var keys))
                        {
                            foreach (var kw in keys)
                            {
                                if (!string.IsNullOrWhiteSpace(task.Name) && task.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                                    score++;
                            }
                        }

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMember = member.Name;
                        }
                    }

                    task.AssignedTo = bestMember;
                    assignedHours[bestMember] += task.DurationDays * 8;
                }

                int totalProjectDays = tasks.Sum(t => Math.Max(0, t.DurationDays));
                if (totalProjectDays == 0) totalProjectDays = 1;

                plan.TeamMembers = provided.Select(m => m.Name).ToList();
                plan.AgentLog ??= new List<string>();

                foreach (var member in provided)
                {
                    var capacityHours = 8 * totalProjectDays; // assume 8h/day
                    var used = assignedHours.TryGetValue(member.Name, out var uh) ? uh : 0;
                    var utilization = capacityHours > 0 ? (double)used / capacityHours : 0.0;

                    if (utilization >= 0.8)
                    {
                        context.AgentLog.Add($"{AgentName} → Warning - member {member.Name} at {utilization:P0} capacity (assigned {used}h of {capacityHours}h).");
                    }
                }

                context.AgentLog.Add($"{AgentName} → Assigned {tasks.Count} tasks to {provided.Count} members");
            }
            else
            {
                // No team members provided; mark tasks as unassigned
                foreach (var task in tasks)
                    task.AssignedTo = "Unassigned";

                plan.TeamMembers = new List<string>();
                context.AgentLog.Add($"{AgentName} → No team members provided; all tasks marked Unassigned");
            }
            return Task.CompletedTask;
        }
    }
}
