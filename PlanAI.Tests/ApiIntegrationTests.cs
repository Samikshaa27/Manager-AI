using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlanAI.Data;
using PlanAI.Models;
using Xunit;

namespace PlanAI.Tests
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Add in-memory DbContext
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });
                });
            });
        }

        [Fact]
        public async Task Register_ValidData_Returns200AndToken()
        {
            var client = _factory.CreateClient();
            var email = $"test{DateTime.UtcNow.Ticks}@example.com";
            var req = new System.Collections.Generic.Dictionary<string, string>
            {
                { "name", "Test User" },
                { "email", email },
                { "password", "password123!" },
                { "role", "Member" }
            };
            
            var response = await client.PostAsJsonAsync("/api/auth/register", req);
            
            // Expected 200 OK or 201 Created depending on our controller implementation
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Status code: {response.StatusCode}, Body: {body}");
            }
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns400()
        {
            var client = _factory.CreateClient();
            var email = $"duplicate{DateTime.UtcNow.Ticks}@example.com";
            var req = new System.Collections.Generic.Dictionary<string, string>
            {
                { "name", "Test User" },
                { "email", email },
                { "password", "password123!" },
                { "role", "Member" }
            };
            
            await client.PostAsJsonAsync("/api/auth/register", req);
            var response = await client.PostAsJsonAsync("/api/auth/register", req);
            
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var client = _factory.CreateClient();
            var email = $"login{DateTime.UtcNow.Ticks}@example.com";
            var req = new System.Collections.Generic.Dictionary<string, string>
            {
                { "name", "Test User" },
                { "email", email },
                { "password", "password123!" },
                { "role", "Member" }
            };
            await client.PostAsJsonAsync("/api/auth/register", req);

            var loginReq = new { Email = email, Password = "wrongpassword" };
            var response = await client.PostAsJsonAsync("/api/auth/login", loginReq);
            
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private async Task<HttpClient> CreateClientWithToken(string role)
        {
            var client = _factory.CreateClient();
            var email = $"{role}{DateTime.UtcNow.Ticks}@example.com";
            var req = new System.Collections.Generic.Dictionary<string, string>
            {
                { "name", $"{role} User" },
                { "email", email },
                { "password", "password123!" },
                { "role", role }
            };
            var regRes = await client.PostAsJsonAsync("/api/auth/register", req);
            if (!regRes.IsSuccessStatusCode)
            {
                var b = await regRes.Content.ReadAsStringAsync();
                throw new Exception($"Register failed: {regRes.StatusCode} - {b}");
            }
            
            var loginReq = new { Email = email, Password = "password123!" };
            var loginRes = await client.PostAsJsonAsync("/api/auth/login", loginReq);
            if (!loginRes.IsSuccessStatusCode)
            {
                var b = await loginRes.Content.ReadAsStringAsync();
                throw new Exception($"Login failed: {loginRes.StatusCode} - {b}");
            }
            
            var content = await loginRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            // Since we added ApiResponse, it's inside data.token
            var token = content.GetProperty("data").GetProperty("token").GetString();
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task RBAC_AdminPostProjects_Returns201()
        {
            var client = await CreateClientWithToken("Admin");
            var req = new { Description = "Test project" };
            var response = await client.PostAsJsonAsync("/api/projects/generate", req);
            
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task RBAC_MemberPostProjects_Returns403()
        {
            var client = await CreateClientWithToken("Member");
            var req = new { Description = "Test project" };
            var response = await client.PostAsJsonAsync("/api/projects/generate", req);
            
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RBAC_UnauthenticatedGetProjects_Returns401()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/projects");
            
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Tasks_PostTasksValidData_Returns201()
        {
            var client = await CreateClientWithToken("Admin");
            var req = new { Title = "New Task", Priority = "High" };
            var response = await client.PostAsJsonAsync("/api/tasks", req);
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Task create failed: {response.StatusCode} - {body}");
            }
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Dashboard_DivisionByZero()
        {
            var client = await CreateClientWithToken("Admin");
            // First create a project
            var req = new { Description = "Test project" };
            var projRes = await client.PostAsJsonAsync("/api/projects/generate", req);
            var content = await projRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var projectId = content.GetProperty("data").GetProperty("id").GetString();

            // The Dashboard shouldn't crash if tasks=0
            var dashRes = await client.GetAsync($"/api/projects/{projectId}/dashboard");
            Assert.Equal(HttpStatusCode.OK, dashRes.StatusCode);
            
            var dashContent = await dashRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var prog = dashContent.GetProperty("data").GetProperty("overallProgressPercent").GetInt32();
            Assert.Equal(0, prog);
        }
    }
}
