using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanAI.Models;

namespace PlanAI.Agents
{
    /// <summary>
    /// Agent responsible for optimizing the plan (critical path, cost tradeoffs).
    /// Performs critical path analysis and adds buffers based on risks.
    /// </summary>
    public class OptimizerAgent : IAgent
    {
        private readonly ILogger<OptimizerAgent> _logger;

        /// <inheritdoc />
        public string AgentName => "Optimizer";

        public OptimizerAgent(ILogger<OptimizerAgent> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task ExecuteAsync(ProjectContext context)
        {
            var plan = context.Plan;
            var tasks = plan.Phases?.SelectMany(ph => ph.Tasks).ToList() ?? new List<ProjectTask>();

            // Reset IsOnCriticalPath flags
            foreach (var t in tasks)
                t.IsOnCriticalPath = false;

            if (!tasks.Any())
            {
                context.AgentLog.Add($"{AgentName} → No tasks to optimize");
                return Task.CompletedTask;
            }

            var ordered = TopologicalSort(tasks);

            var earliestStart = new Dictionary<ProjectTask, int>();
            var earliestFinish = new Dictionary<ProjectTask, int>();

            foreach (var t in ordered)
            {
                int start = 0;
                var deps = ResolveDependencies(t, tasks);
                if (deps.Any())
                    start = deps.Max(d => earliestFinish.ContainsKey(d) ? earliestFinish[d] : 0);

                earliestStart[t] = start;
                earliestFinish[t] = start + Math.Max(0, t.DurationDays);
            }

            int projectDuration = earliestFinish.Values.DefaultIfEmpty(0).Max();

            var latestFinish = new Dictionary<ProjectTask, int>();
            var latestStart = new Dictionary<ProjectTask, int>();

            foreach (var t in ordered.AsEnumerable().Reverse())
            {
                var dependents = tasks.Where(x => ResolveDependencies(x, tasks).Contains(t)).ToList();
                if (!dependents.Any())
                    latestFinish[t] = projectDuration;
                else
                    latestFinish[t] = dependents.Min(d => latestStart.ContainsKey(d) ? latestStart[d] : projectDuration);

                latestStart[t] = latestFinish[t] - Math.Max(0, t.DurationDays);
            }

            foreach (var t in tasks)
            {
                if (earliestStart.TryGetValue(t, out var es) && latestStart.TryGetValue(t, out var ls) && es == ls)
                    t.IsOnCriticalPath = true;
                else
                    t.IsOnCriticalPath = false;
            }

            int bufferAddition = 0;
            if (plan?.Risks != null && plan.Risks.Any())
            {
                var highest = plan.Risks.Max(r => r.Severity);
                bufferAddition = highest switch
                {
                    SeverityLevel.Critical => 5,
                    SeverityLevel.High => 3,
                    SeverityLevel.Medium => 1,
                    _ => 0
                };
            }

            if (bufferAddition > 0)
            {
                foreach (var t in tasks.Where(x => x.IsOnCriticalPath))
                    t.BufferDays += bufferAddition;
            }

            // Recalculate with buffers
            earliestStart.Clear();
            earliestFinish.Clear();
            foreach (var t in ordered)
            {
                int start = 0;
                var deps = ResolveDependencies(t, tasks);
                if (deps.Any())
                    start = deps.Max(d => earliestFinish.ContainsKey(d) ? earliestFinish[d] : 0);

                earliestStart[t] = start;
                var dur = Math.Max(0, t.DurationDays + t.BufferDays);
                earliestFinish[t] = start + dur;
            }

            int finalDuration = earliestFinish.Values.DefaultIfEmpty(0).Max();
            plan.TotalDurationDays = finalDuration;
            context.AgentLog.Add($"{AgentName} → Calculated duration {finalDuration} days; buffer {bufferAddition} days applied to critical tasks");

            // Fallback: if no tasks are marked as critical, mark top-3 longest tasks
            var allTasks = context.Plan.Phases?.SelectMany(p => p.Tasks).ToList() ?? new List<ProjectTask>();
            bool anyMarked = allTasks.Any(t => t.IsOnCriticalPath);
            _logger.LogInformation("Tasks marked as critical: {count}", allTasks.Count(t => t.IsOnCriticalPath));

            if (!anyMarked)
            {
                var top3 = allTasks.OrderByDescending(t => t.DurationDays).Take(3).ToList();
                top3.ForEach(t => t.IsOnCriticalPath = true);
                _logger.LogInformation("Fallback: marked top 3 longest tasks as critical path");
                context.AgentLog.Add($"{AgentName} → Fallback: marked top 3 longest tasks as critical path");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs a topological sort of the provided tasks using Kahn's algorithm.
        /// Unrecognized dependencies (not matching any task Id or Name) are ignored.
        /// Throws InvalidOperationException if a cycle is detected.
        /// </summary>
        private static List<ProjectTask> TopologicalSort(List<ProjectTask> tasks)
        {
            var mapByIdOrName = tasks.ToDictionary(
                t => t.Id.ToString(),
                t => t,
                StringComparer.OrdinalIgnoreCase);

            // Also allow lookup by name
            foreach (var t in tasks)
            {
                if (!string.IsNullOrWhiteSpace(t.Name) && !mapByIdOrName.ContainsKey(t.Name))
                {
                    mapByIdOrName[t.Name] = t;
                }
            }

            // Build adjacency and indegree
            var indegree = tasks.ToDictionary(t => t, t => 0);
            var adj = tasks.ToDictionary(t => t, t => new List<ProjectTask>());

            foreach (var t in tasks)
            {
                var deps = ResolveDependencies(t, tasks);
                foreach (var d in deps)
                {
                    // edge from d -> t
                    adj[d].Add(t);
                    indegree[t] = indegree[t] + 1;
                }
            }

            var q = new Queue<ProjectTask>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var result = new List<ProjectTask>();

            while (q.Count > 0)
            {
                var n = q.Dequeue();
                result.Add(n);

                foreach (var m in adj[n])
                {
                    indegree[m]--;
                    if (indegree[m] == 0)
                        q.Enqueue(m);
                }
            }

            if (result.Count != tasks.Count)
            {
                throw new InvalidOperationException("Cycle detected in task dependencies; topological sort failed.");
            }

            return result;
        }

        /// <summary>
        /// Resolves dependencies for a task into actual ProjectTask references that exist in the provided set.
        /// Matches dependency strings against task.Id.ToString() and task.Name (case-insensitive).
        /// </summary>
        private static List<ProjectTask> ResolveDependencies(ProjectTask task, List<ProjectTask> allTasks)
        {
            var result = new List<ProjectTask>();
            if (task.Dependencies == null || !task.Dependencies.Any())
                return result;

            var lookupById = allTasks.ToDictionary(t => t.Id.ToString(), t => t, StringComparer.OrdinalIgnoreCase);
            var lookupByName = allTasks.Where(t => !string.IsNullOrWhiteSpace(t.Name)).ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

            foreach (var dep in task.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dep))
                    continue;

                if (lookupById.TryGetValue(dep, out var byId))
                {
                    result.Add(byId);
                    continue;
                }

                if (lookupByName.TryGetValue(dep, out var byName))
                {
                    result.Add(byName);
                    continue;
                }

                // Try parsing as GUID
                if (Guid.TryParse(dep, out var guid) && lookupById.TryGetValue(guid.ToString(), out var byParsedId))
                {
                    result.Add(byParsedId);
                }
            }

            return result;
        }
    }
}
