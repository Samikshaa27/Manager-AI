using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PlanAI.Models;

namespace PlanAI.Services
{
    /// <summary>
    /// Small wrapper over a language model provider (LLM).
    /// </summary>
    public class LlmService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _apiKey;
        private readonly ILogger<LlmService> _logger;

        public LlmService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<LlmService> logger)
        {
            _httpFactory = httpFactory;
            _apiKey = config["OpenAI:ApiKey"];
            _logger = logger;

            try
            {
                _logger.LogInformation("API key length: {len}", _apiKey?.Length ?? 0);
            }
            catch
            {
                // ignore logging errors
            }
        }

        /// <summary>
        /// Generates tasks using the LLM. Returns an empty list on failure.
        /// </summary>
        public async Task<List<ProjectTask>> GenerateTasksAsync(string description, string category)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return new List<ProjectTask>();

            var client = _httpFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var systemPrompt = @"You are a project planning expert. 
Generate 10-15 detailed, realistic project tasks for the provided project description and category.
Return a JSON object with a 'tasks' array.

PHASE RULES:
- Logical phases: Initiation, Planning, Design, Execution, Testing, Deployment, Closure.

STRUCTURE:
{
  ""tasks"": [
    {
      ""name"": ""task name"", 
      ""description"": ""full description"", 
      ""durationDays"": 5, 
      ""priority"": ""High"", 
      ""phase"": ""Phase Name""
    }
  ]
}

- Priority must be: Low, Medium, High, or Critical.
- Return ONLY valid JSON.";

            var userPrompt = $"Project Description: {description}\nCategory: {category}";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2,
                max_tokens = 3000,
                response_format = new { type = "json_object" }
            };

            try
            {
                var resp = await client.PostAsync("v1/chat/completions", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                var text = await resp.Content.ReadAsStringAsync();
                _logger.LogInformation("OpenAI HTTP status: {status}", resp.StatusCode);
                _logger.LogInformation("Raw LLM response: {response}", text);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("LLM request failed with status {status}", resp.StatusCode);
                    return new List<ProjectTask>();
                }

                string message = null;
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                try
                {
                    var openAiResp = JsonSerializer.Deserialize<OpenAiResponse>(text, options);
                    message = openAiResp?.Choices?.Length > 0 ? openAiResp.Choices[0]?.Message?.Content : null;
                }
                catch (JsonException) { /* fall back to manual parsing below */ }

                if (string.IsNullOrWhiteSpace(message))
                {
                    // Try manual extraction if structured deserialization didn't yield content
                    using var doc = JsonDocument.Parse(text);
                    if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("LLM returned no choices");
                        return new List<ProjectTask>();
                    }

                    message = choices[0].GetProperty("message").GetProperty("content").GetString();
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        _logger.LogWarning("LLM message content empty");
                        return new List<ProjectTask>();
                    }
                }

                // Strip markdown code fences that often wrap JSON
                var cleaned = message.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                                     .Trim();

                try
                {
                    using var payload = JsonDocument.Parse(cleaned);
                    var root = payload.RootElement;

                    JsonElement tasksElement;
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("tasks", out tasksElement))
                    {
                        // Standard expected format
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        tasksElement = root;
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                         // Check for varied property names like "ProjectTasks" or just returning the first array property
                         var firstArray = root.EnumerateObject().FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.Array);
                         if (firstArray.Value.ValueKind == JsonValueKind.Array)
                         {
                             tasksElement = firstArray.Value;
                         }
                         else
                         {
                             _logger.LogWarning("Unable to find tasks array property in JSON object");
                             return new List<ProjectTask>();
                         }
                    }
                    else
                    {
                        // fallback: try to extract first array via regex/string searching
                        var arr = ExtractJsonArray(cleaned);
                        if (string.IsNullOrWhiteSpace(arr))
                        {
                            _logger.LogWarning("Unable to find tasks array in LLM response");
                            return new List<ProjectTask>();
                        }
                        tasksElement = JsonDocument.Parse(arr).RootElement;
                    }

                    var tasksJson = tasksElement.GetRawText();
                    var tasks = JsonSerializer.Deserialize<List<LlmTaskDto>>(tasksJson, options);
                    if (tasks == null || tasks.Count == 0)
                    {
                        _logger.LogWarning("Deserialized tasks collection is null or empty. Raw: {json}", tasksJson);
                        return new List<ProjectTask>();
                    }

                    _logger.LogInformation("LLM returned {count} tasks", tasks.Count);

                    var result = new List<ProjectTask>();
                    foreach (var t in tasks)
                    {
                        if (string.IsNullOrWhiteSpace(t.Name))
                            continue;

                        if (!Enum.TryParse<PriorityLevel>(t.Priority ?? "Medium", true, out var pr))
                            pr = PriorityLevel.Medium;

                        result.Add(new ProjectTask
                        {
                            Name = t.Name,
                            Description = t.Description ?? t.Name,
                            DurationDays = t.DurationDays,
                            Priority = pr,
                            Phase = t.Phase ?? "Execution"
                        });
                    }

                    return result;
                }
                catch (JsonException jex)
                {
                    _logger.LogError(jex, "Failed to parse LLM cleaned message as JSON: {msg}", cleaned);
                    return new List<ProjectTask>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM GenerateTasksAsync failed");
                return new List<ProjectTask>();
            }
        }

        /// <summary>
        /// Estimates cost range from the LLM. Returns (0,0) on failure.
        /// </summary>
        public async Task<(decimal Min, decimal Max)> EstimateCostAsync(string description, string category, int taskCount, int durationDays)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return (0m, 0m);

            var client = _httpFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var prompt = $@"Estimate project cost range in Indian Rupees for:
Project: {description}
Category: {category}
Tasks: {taskCount}
Duration: {durationDays} days
Return JSON only, no explanation: {{""min"": 0, ""max"": 0}}";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[] {
                    new { role = "system", content = prompt }
                },
                temperature = 0.2,
                max_tokens = 200
            };

            try
            {
                var resp = await client.PostAsync("v1/chat/completions", new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                var text = await resp.Content.ReadAsStringAsync();
                _logger.LogInformation("OpenAI HTTP status: {status}", resp.StatusCode);
                _logger.LogInformation("Raw LLM response: {response}", text);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("LLM cost request failed with status {status}", resp.StatusCode);
                    return (0m, 0m);
                }

                string message = null;
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                try
                {
                    var openAiResp = JsonSerializer.Deserialize<OpenAiResponse>(text, options);
                    message = openAiResp?.Choices?.Length > 0 ? openAiResp.Choices[0]?.Message?.Content : null;
                }
                catch (JsonException) { }

                if (string.IsNullOrWhiteSpace(message))
                {
                    using var doc = JsonDocument.Parse(text);
                    if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("LLM cost returned no choices");
                        return (0m, 0m);
                    }

                    message = choices[0].GetProperty("message").GetProperty("content").GetString();
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        _logger.LogWarning("LLM cost message content empty");
                        return (0m, 0m);
                    }
                }

                var cleaned = message.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                                     .Trim();

                try
                {
                    using var payload = JsonDocument.Parse(cleaned);
                    var root = payload.RootElement;
                    JsonElement objEl;
                    if (root.ValueKind == JsonValueKind.Object)
                        objEl = root;
                    else
                    {
                        var objText = ExtractJsonObject(cleaned);
                        if (string.IsNullOrWhiteSpace(objText))
                        {
                            _logger.LogWarning("Unable to find JSON object for cost in LLM response");
                            return (0m, 0m);
                        }
                        objEl = JsonDocument.Parse(objText).RootElement;
                    }

                    var obj = JsonSerializer.Deserialize<CostDto>(objEl.GetRawText(), options);
                    if (obj == null)
                        return (0m, 0m);

                    _logger.LogInformation("LLM estimated cost: {min} - {max}", obj.Min, obj.Max);
                    return (obj.Min, obj.Max);
                }
                catch (JsonException jex)
                {
                    _logger.LogError(jex, "Failed to parse LLM cost response: {msg}", cleaned);
                    return (0m, 0m);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM EstimateCostAsync failed");
                return (0m, 0m);
            }
        }

        private static string ExtractJsonArray(string text)
        {
            var start = text.IndexOf('[');
            var end = text.LastIndexOf(']');
            if (start >= 0 && end > start)
                return text[start..(end + 1)];
            return null;
        }

        private static string ExtractJsonObject(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
                return text[start..(end + 1)];
            return null;
        }

        private class LlmTaskDto
        {
            [JsonPropertyName("name")] public string Name { get; set; }
            [JsonPropertyName("description")] public string Description { get; set; }
            [JsonPropertyName("durationDays")] public int DurationDays { get; set; }
            [JsonPropertyName("priority")] public string Priority { get; set; }
            [JsonPropertyName("phase")] public string Phase { get; set; }
        }

        private class CostDto
        {
            [JsonPropertyName("min")] public decimal Min { get; set; }
            [JsonPropertyName("max")] public decimal Max { get; set; }
        }

        private class OpenAiResponse
        {
            [JsonPropertyName("choices")] public OpenAiChoice[] Choices { get; set; }
        }

        private class OpenAiChoice
        {
            [JsonPropertyName("message")] public OpenAiMessage Message { get; set; }
        }

        private class OpenAiMessage
        {
            [JsonPropertyName("content")] public string Content { get; set; }
        }
    }
}