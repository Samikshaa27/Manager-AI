using System.Collections.Generic;

namespace PlanAI.Models
{
    /// <summary>
    /// Summary of resource estimates returned by ResourceAgent.
    /// </summary>
    public class ResourceSummary
    {
        public int? Headcount { get; set; }
        public decimal? CostMin { get; set; }
        public decimal? CostMax { get; set; }
        public List<string> Equipment { get; set; } = new();
    }
}
