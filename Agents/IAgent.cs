using System.Threading.Tasks;
using PlanAI.Models;

namespace PlanAI.Agents
{
    /// <summary>
    /// Interface for pipeline agents that operate on a shared ProjectContext.
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Execute the agent logic and modify the provided context.
        /// </summary>
        /// <param name="context">Shared project context for the pipeline.</param>
        Task ExecuteAsync(ProjectContext context);

        /// <summary>
        /// Human-readable name of the agent.
        /// </summary>
        string AgentName { get; }
    }
}
