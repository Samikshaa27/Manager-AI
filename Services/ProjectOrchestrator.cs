using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanAI.Agents;
using PlanAI.Models;

namespace PlanAI.Services
{
    /// <summary>
    /// Orchestrates the pipeline of agents to generate a ProjectPlan from a description.
    /// </summary>
    public class ProjectOrchestrator
    {
        private readonly IEnumerable<IAgent> _agents;
        private readonly ILogger<ProjectOrchestrator> _logger;

        /// <summary>
        /// Creates a new orchestrator with the agent pipeline.
        /// </summary>
        public ProjectOrchestrator(IEnumerable<IAgent> agents, ILogger<ProjectOrchestrator> logger)
        {
            _agents = agents;
            _logger = logger;
        }

        /// <summary>
        /// Generates a ProjectPlan for the provided request by running each agent in sequence.
        /// </summary>
        public async Task<ProjectPlan> GeneratePlanAsync(GenerateProjectRequest request)
        {
            var context = new ProjectContext
            {
                Description = request?.Description,
                BudgetMin = request?.BudgetMin,
                BudgetMax = request?.BudgetMax,
                Currency = request?.Currency ?? "INR",
                TeamMembers = request?.TeamMembers ?? new List<TeamMemberRequest>()
            };

            _logger.LogInformation("Budget in context: {min} - {max}", context.BudgetMin, context.BudgetMax);

            var description = request?.Description;
            context.Plan.ProjectName = description?.Length > 40 ? description[..40] + "..." : (description ?? "Untitled Project");
            context.Plan.Description = description;
            context.Plan.CreatedAt = DateTime.UtcNow;

            foreach (var agent in _agents)
            {
                await agent.ExecuteAsync(context);
            }

            // Attach agent log to plan for persistence and return
            context.Plan.AgentLog = context.AgentLog;
            return context.Plan;
        }
    }
}
