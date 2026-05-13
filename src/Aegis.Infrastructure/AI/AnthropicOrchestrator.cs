using Aegis.Application.Interfaces;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aegis.Infrastructure.AI;

public class AnthropicOrchestrator : IAIOrchestrator
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AnthropicOrchestrator> _logger;
    private const string Model = "claude-sonnet-4-6";

    public AnthropicOrchestrator(IConfiguration config, ILogger<AnthropicOrchestrator> logger)
    {
        _client = new AnthropicClient(config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey not configured"));
        _logger = logger;
    }

    public async Task<string> GenerateAssessmentQuestionAsync(
        AssessmentQuestionContext ctx, CancellationToken ct = default)
    {
        var previousContext = ctx.PreviousAnswers.TakeLast(3)
            .Select(a => $"Q: {a.Question}\nA: {a.Answer}")
            .Aggregate("", (acc, s) => acc + "\n" + s);

        var prompt = $"""
            Skill area: {ctx.Skill}
            Target seniority: {ctx.SeniorityTarget}
            Question difficulty: {ctx.Difficulty}/5
            {(previousContext.Length > 0 ? $"\nPrevious answers:\n{previousContext}" : "")}

            Generate ONE precise technical assessment question. No preamble.
            Focus on practical experience, real-world tradeoffs, and failure modes.
            """;

        return await CallAsync(
            system: "You are a senior technical interviewer. Ask ONE precise question. Be direct. No multiple questions.",
            prompt: prompt,
            maxTokens: 300,
            ct: ct);
    }

    public async Task<AnswerEvaluation> EvaluateAnswerAsync(
        string question, string answer, string skill, int difficulty, CancellationToken ct = default)
    {
        var response = await CallAsync(
            system: """
                Evaluate this technical answer. Return JSON only:
                {"score": 0.0-1.0, "depth": "superficial|adequate|deep",
                 "bluff_signals": [], "strong_signals": [], "weak_signals": []}
                No extra text.
                """,
            prompt: $"Skill: {skill}\nDifficulty: {difficulty}/5\nQuestion: {question}\nAnswer: {answer}",
            maxTokens: 500,
            ct: ct);

        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            return new AnswerEvaluation(
                Score: root.GetProperty("score").GetDouble(),
                Depth: root.GetProperty("depth").GetString() ?? "superficial",
                BluffSignals: ParseStringList(root, "bluff_signals"),
                StrongSignals: ParseStringList(root, "strong_signals"),
                WeakSignals: ParseStringList(root, "weak_signals"));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM evaluation response, using defaults");
            return new AnswerEvaluation(0.3, "superficial", [], [], ["Could not parse response"]);
        }
    }

    public async Task<string> GenerateCareerInsightAsync(
        CareerInsightRequest request, CancellationToken ct = default)
    {
        var kpiText = string.Join("\n", request.MarketKpis.Select(kv => $"  {kv.Key}: {kv.Value:F1}"));

        return await CallAsync(
            system: """
                You are a strategic career analyst. Write a concise, data-driven insight.
                Use ONLY the KPIs provided — never invent statistics.
                Be direct and specific. Max 200 words. No motivational fluff.
                """,
            prompt: $"""
                Current Role: {request.CurrentRole} ({request.YearsExperience} years)
                Validated Skills: {string.Join(", ", request.ValidatedSkills)}
                Market KPIs:
                {kpiText}
                Recommended Paths: {string.Join(", ", request.RecommendedPaths)}

                Generate a strategic career insight.
                """,
            maxTokens: 500,
            ct: ct);
    }

    public async Task<string> ExplainRecommendationAsync(
        RecommendationExplainContext ctx, CancellationToken ct = default)
    {
        var kpiText = string.Join("\n", ctx.MarketKpis.Select(kv => $"  {kv.Key}: {kv.Value:F1}"));

        return await CallAsync(
            system: """
                Explain a career recommendation backed by data.
                Reference specific KPI values provided. Never invent data.
                Be concise and analytical. Max 150 words.
                """,
            prompt: $"""
                Current Role: {ctx.CurrentRole}
                Recommended Path: {ctx.TargetRole}
                Salary Uplift: {ctx.SalaryUpliftPct:F0}%
                Transition Difficulty: {ctx.TransitionDifficulty}
                Market KPIs:
                {kpiText}
                """,
            maxTokens: 400,
            ct: ct);
    }

    private async Task<string> CallAsync(string system, string prompt, int maxTokens, CancellationToken ct)
    {
        var request = new MessageParameters
        {
            Model = Model,
            MaxTokens = maxTokens,
            System = [new SystemMessage(system)],
            Messages = [new Message(RoleType.User, prompt)]
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request, ct);
        return response.Content[0].Text;
    }

    private static List<string> ParseStringList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var arr)) return [];
        return arr.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
    }
}
