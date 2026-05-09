using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PlanAI.Agents;
using PlanAI.Models;
using PlanAI.Services;
using Xunit;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace PlanAI.Tests
{
    public class AgentTests
    {
        [Fact]
        public async Task CategoryDetectorAgent_WithDescription_ReturnsNonEmptyCategory()
        {
            var agent = new CategoryDetectorAgent();
            var ctx = new ProjectContext { Description = "A mobile app" };
            await agent.ExecuteAsync(ctx);
            Assert.False(string.IsNullOrEmpty(ctx.Category));
        }

        [Fact]
        public async Task CategoryDetectorAgent_EmptyDescription_ThrowsArgumentException()
        {
            var agent = new CategoryDetectorAgent();
            var ctx = new ProjectContext { Description = "" };
            await Assert.ThrowsAsync<ArgumentException>(() => agent.ExecuteAsync(ctx));
        }

        [Fact]
        public async Task TaskPlannerAgent_EmptyDescription_ThrowsArgumentException()
        {
            var httpClient = new HttpClient();
            var mockConfig = new Mock<IConfiguration>();
            var llm = new LlmService(new Mock<IHttpClientFactory>().Object, mockConfig.Object, new Mock<ILogger<LlmService>>().Object);
            var agent = new TaskPlannerAgent(llm, new Mock<ILogger<TaskPlannerAgent>>().Object);
            var ctx = new ProjectContext { Description = "" };
            await Assert.ThrowsAsync<ArgumentException>(() => agent.ExecuteAsync(ctx));
        }
        
        [Fact]
        public async Task TaskPlannerAgent_ValidDescription_ReturnsTasks()
        {
            var httpClient = new HttpClient();
            var mockConfig = new Mock<IConfiguration>();
            var llm = new LlmService(new Mock<IHttpClientFactory>().Object, mockConfig.Object, new Mock<ILogger<LlmService>>().Object);
            var agent = new TaskPlannerAgent(llm, new Mock<ILogger<TaskPlannerAgent>>().Object);
            var ctx = new ProjectContext { Description = "Create a login page", Category = "Software" };
            await agent.ExecuteAsync(ctx);
            Assert.True(ctx.Plan.Phases.Count > 0);
            Assert.True(ctx.Plan.Phases[0].Tasks.Count > 0);
            var task = ctx.Plan.Phases[0].Tasks[0];
            Assert.NotNull(task.Name);
            Assert.True(task.Priority == PriorityLevel.Low || task.Priority == PriorityLevel.Medium || task.Priority == PriorityLevel.High || task.Priority == PriorityLevel.Critical);
        }

        [Fact]
        public async Task RiskAgent_ReturnsRisks()
        {
            var agent = new RiskAgent();
            var ctx = new ProjectContext { Description = "A generic project" };
            await agent.ExecuteAsync(ctx);
            Assert.True(ctx.Plan.Risks.Count > 0);
            Assert.NotNull(ctx.Plan.Risks[0].Title);
        }

        [Fact]
        public async Task OptimizerAgent_ReordersTasksWithoutDropping()
        {
            var agent = new OptimizerAgent(new Mock<ILogger<OptimizerAgent>>().Object);
            var ctx = new ProjectContext { Plan = new ProjectPlan { Phases = new List<Phase> { new Phase { Tasks = new List<ProjectTask> { new ProjectTask { Name = "Task1" }, new ProjectTask { Name = "Task2" } } } } } };
            await agent.ExecuteAsync(ctx);
            Assert.Equal(2, ctx.Plan.Phases[0].Tasks.Count);
        }

        [Fact]
        public async Task ResourceAgent_ReturnsResources()
        {
            var agent = new ResourceAgent(new Mock<ILogger<ResourceAgent>>().Object);
            var ctx = new ProjectContext { Description = "Web App" };
            await agent.ExecuteAsync(ctx);
            Assert.NotNull(ctx.AgentLog);
            Assert.True(ctx.AgentLog.Count > 0);
        }

        [Fact]
        public async Task TeamAssignmentAgent_AssignsTasks()
        {
            var agent = new TeamAssignmentAgent();
            var ctx = new ProjectContext { Plan = new ProjectPlan { Phases = new List<Phase> { new Phase { Tasks = new List<ProjectTask> { new ProjectTask { Name = "Test" } } } } }, TeamMembers = new List<TeamMemberRequest> { new TeamMemberRequest { Name = "John", Role = "Developer" } } };
            await agent.ExecuteAsync(ctx);
            Assert.NotNull(ctx.Plan.Phases[0].Tasks[0].AssignedTo);
        }
    }
}
