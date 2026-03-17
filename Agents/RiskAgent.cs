using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanAI.Models;

namespace PlanAI.Agents
{
    /// <summary>
    /// Agent that identifies risks and suggests mitigations.
    /// </summary>
    public class RiskAgent : IAgent
    {
        /// <inheritdoc />
        public string AgentName => "RiskAgent";

        private static readonly Dictionary<string, List<Risk>> DefaultRisksByCategory = new()
        {
            ["Solar"] = new()
            {
                new Risk { Title = "Supply chain delay", Description = "Delays in panel/inverter delivery", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Procurement", MitigationNote = "Identify alternate suppliers" },
                new Risk { Title = "Weather impact", Description = "Monsoon or storms delaying installation", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.Medium, Owner = "PM", MitigationNote = "Plan around weather windows" }
            },
            ["Software"] = new()
            {
                new Risk { Title = "Scope creep", Description = "Requirements grow over time", Probability = ProbabilityLevel.High, Severity = SeverityLevel.Medium, Owner = "Product", MitigationNote = "Strict change control" },
                new Risk { Title = "Integration bugs", Description = "Third-party APIs change unexpectedly", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Engineering", MitigationNote = "Pin dependency versions" }
            },
            ["Construction"] = new()
            {
                new Risk { Title = "Regulatory delays", Description = "Permitting or inspections delayed", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Site Manager", MitigationNote = "Engage early with authorities" },
                new Risk { Title = "Material cost increases", Description = "Price volatility in materials", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.Medium, Owner = "Procurement", MitigationNote = "Lock prices where possible" }
            },
            ["Healthcare"] = new()
            {
                new Risk { Title = "Regulatory compliance", Description = "Health regulations cause rework", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Compliance", MitigationNote = "Early regulatory review" },
                new Risk { Title = "Training shortfall", Description = "Staff not trained in time", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.Medium, Owner = "Operations", MitigationNote = "Schedule training early" }
            },
            ["Event"] = new()
            {
                new Risk { Title = "Vendor no-show", Description = "Caterer or AV vendor cancels", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Logistics", MitigationNote = "Keep backups ready" },
                new Risk { Title = "Weather on event day", Description = "Outdoor event impacted by weather", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.Medium, Owner = "Event Ops", MitigationNote = "Plan indoor backup" }
            },
            ["Manufacturing"] = new()
            {
                new Risk { Title = "Equipment failure", Description = "Critical machinery downtime", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Maintenance", MitigationNote = "Preventive maintenance" },
                new Risk { Title = "Quality issues", Description = "Product quality not meeting standards", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.Medium, Owner = "QA", MitigationNote = "Pilot runs and QA checks" }
            }
        };

        /// <inheritdoc />
        public Task ExecuteAsync(ProjectContext context)
        {
            var description = context.Description ?? string.Empty;
            var category = context.Category ?? "Other";

            var risks = new List<Risk>();

            var keywordMap = new Dictionary<string, Risk>(StringComparer.OrdinalIgnoreCase)
            {
                ["kseb"] = new Risk { Title = "Grid approval delay", Description = "Local grid authority approvals (KSEB) may delay interconnection", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Compliance", MitigationNote = "Engage with utility early" },
                ["monsoon"] = new Risk { Title = "Weather delay", Description = "Monsoon season could delay outdoor works", Probability = ProbabilityLevel.High, Severity = SeverityLevel.Medium, Owner = "Site", MitigationNote = "Adjust schedule for seasonality" },
                ["import"] = new Risk { Title = "Import delays", Description = "Import restrictions or shipping delays", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Procurement", MitigationNote = "Source local alternatives" },
                ["regulatory"] = new Risk { Title = "Regulatory change", Description = "Regulatory changes could require rework", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.High, Owner = "Compliance", MitigationNote = "Monitor regulations" },
                ["deadline"] = new Risk { Title = "Aggressive deadline", Description = "Tight deadlines may force overtime or scope cuts", Probability = ProbabilityLevel.High, Severity = SeverityLevel.High, Owner = "PM", MitigationNote = "Negotiate realistic deadlines" }
            };

            foreach (var kv in keywordMap)
            {
                if (description.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    risks.Add(kv.Value);
                }
            }

            if (DefaultRisksByCategory.TryGetValue(category, out var defaults))
            {
                risks.AddRange(defaults.Select(r => new Risk
                {
                    Title = r.Title,
                    Description = r.Description,
                    Probability = r.Probability,
                    Severity = r.Severity,
                    Owner = r.Owner,
                    MitigationNote = r.MitigationNote
                }));
            }

            if (!risks.Any())
            {
                risks.Add(new Risk { Title = "Unknowns", Description = "General unknowns during execution", Probability = ProbabilityLevel.Medium, Severity = SeverityLevel.Medium, Owner = "PM", MitigationNote = "Reserve contingency" });
            }

            context.Plan.Risks = risks;
            context.AgentLog.Add($"{AgentName} → Identified {risks.Count} risks");
            return Task.CompletedTask;
        }
    }
}
