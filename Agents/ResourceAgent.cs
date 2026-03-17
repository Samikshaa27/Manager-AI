using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanAI.Models;

namespace PlanAI.Agents
{
    /// <summary>
    /// Agent that estimates resource requirements and costs.
    /// </summary>
    public class ResourceAgent : IAgent
    {
        public string AgentName => "ResourceAgent";
        private readonly ILogger<ResourceAgent> _logger;

        private static readonly Dictionary<string, (decimal Min, decimal Max, string[] Equipment)> CategoryCostMap = new()
        {
            ["Solar"] = (4200000m, 5800000m, new[] { "Solar panels", "Inverters", "Mounting hardware" }),
            ["Software"] = (800000m, 2000000m, new[] { "Cloud hosting", "CI/CD tools", "Testing devices" }),
            ["Construction"] = (10000000m, 25000000m, new[] { "Excavators", "Cranes", "Scaffolding" }),
            ["Healthcare"] = (5000000m, 12000000m, new[] { "Medical equipment", "Clinical IT systems" }),
            ["Event"] = (500000m, 1500000m, new[] { "AV equipment", "Staging", "Catering" }),
            ["Manufacturing"] = (1000000m, 3000000m, new[] { "Assembly line equipment", "Conveyors" }),
            ["Other"] = (1000000m, 3000000m, new[] { "General equipment" })
        };

        public ResourceAgent(ILogger<ResourceAgent> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task ExecuteAsync(ProjectContext context)
        {
            var plan = context.Plan;
            _logger.LogInformation("ResourceAgent sees budget: {min} - {max}", context.BudgetMin, context.BudgetMax);
            var tasks = plan.Phases?.SelectMany(ph => ph.Tasks).ToList() ?? new List<ProjectTask>();
            var category = context.Category ?? "Other";

            int headcount = tasks.Count < 5 ? 2 : tasks.Count < 10 ? 4 : 6;

            decimal costMin;
            decimal costMax;
            List<string> equipment;

            // Prefer user-provided budget if present in the context
            if (context.BudgetMin.HasValue || context.BudgetMax.HasValue)
            {
                costMin = context.BudgetMin ?? 0m;
                costMax = context.BudgetMax ?? costMin;
                equipment = new List<string>();
            }
            else
            {
                if (!CategoryCostMap.TryGetValue(category, out var costEntry))
                {
                    costEntry = CategoryCostMap["Other"];
                }

                costMin = costEntry.Min;
                costMax = costEntry.Max;
                equipment = new List<string>(costEntry.Equipment);
            }

            plan.ResourceSummary = new ResourceSummary
            {
                Headcount = headcount,
                CostMin = costMin,
                CostMax = costMax,
                Equipment = equipment
            };

            var dailyRate = 8m * 50m;
            var laborCost = headcount * dailyRate * Math.Max(1, plan.TotalDurationDays);

            // Explicitly set plan-level estimated cost fields from context or fallback
            plan.EstimatedCostMin = context.BudgetMin ?? costMin;
            plan.EstimatedCostMax = context.BudgetMax ?? costMax;

            context.AgentLog.Add($"{AgentName} → Headcount={headcount}, EstimatedCostMin={plan.EstimatedCostMin:C}, EstimatedCostMax={plan.EstimatedCostMax:C}, LaborEst={laborCost:C}");
            return Task.CompletedTask;
        }
    }
}
