using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanAI.Models;

namespace PlanAI.Agents
{
    /// <summary>
    /// Agent responsible for detecting project category from description.
    /// </summary>
    public class CategoryDetectorAgent : IAgent
    {
        /// <inheritdoc />
        public string AgentName => "CategoryDetector";

        // Keep the scoring dictionary private so tests can still call public detection method
        // while the keyword lists remain implementation details.
        private static readonly Dictionary<string, string[]> CategoryKeywords = new()
        {
            ["Solar"] = new[] { "solar", "panel", "photovoltaic", "pv", "inverter", "sun" },
            ["Software"] = new[] { "software", "app", "application", "backend", "frontend", "api", "web" },
            ["Construction"] = new[] { "construction", "build", "contractor", "site", "foundation", "masonry" },
            ["Healthcare"] = new[] { "health", "medical", "clinic", "patient", "hospital", "telemedicine" },
            ["Event"] = new[] { "event", "conference", "meetup", "wedding", "expo", "ceremony" },
            ["Manufacturing"] = new[] { "manufacture", "factory", "production", "assembly", "plant", "industrial" },
            ["Other"] = Array.Empty<string>()
        };

        /// <summary>
        /// Detects the category for the provided input and returns the category name
        /// along with a confidence score (0-100).
        /// </summary>
        /// <param name="input">Plain-English project description.</param>
        /// <returns>Tuple of detected category and confidence percent.</returns>
        public static (string Category, int Confidence) DetectCategory(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return ("Other", 0);
            }

            var text = input.ToLowerInvariant();
            string bestCategory = "Other";
            int bestMatchCount = 0;
            double bestRatio = 0.0;

            foreach (var kvp in CategoryKeywords)
            {
                var category = kvp.Key;
                var keywords = kvp.Value;

                if (keywords == null || keywords.Length == 0)
                {
                    continue;
                }

                int matchCount = 0;
                foreach (var kw in keywords)
                {
                    if (string.IsNullOrWhiteSpace(kw))
                        continue;

                    if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        matchCount++;
                    }
                }

                double ratio = (double)matchCount / Math.Max(1, keywords.Length);

                // Prefer higher absolute match count; break ties with ratio.
                if (matchCount > bestMatchCount || (matchCount == bestMatchCount && ratio > bestRatio))
                {
                    bestMatchCount = matchCount;
                    bestCategory = category;
                    bestRatio = ratio;
                }
            }

            if (bestMatchCount == 0)
            {
                return ("Other", 0);
            }

            // Confidence is percentage of keywords matched for the winning category.
            var winningKeywords = CategoryKeywords[bestCategory];
            int confidence = (int)Math.Round((double)bestMatchCount / Math.Max(1, winningKeywords.Length) * 100);
            confidence = Math.Clamp(confidence, 0, 100);

            return (bestCategory, confidence);
        }

        /// <inheritdoc />
        public Task ExecuteAsync(ProjectContext context)
        {
            var (category, confidence) = DetectCategory(context?.Description ?? string.Empty);
            context.Category = category;
            context.Plan.Category = category;
            context.AgentLog.Add($"{AgentName} → {category} (confidence {confidence}%)");
            return Task.CompletedTask;
        }
    }
}
