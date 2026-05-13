using Aegis.Domain.Entities;
using Aegis.Domain.Enums;

namespace Aegis.Application.Assessment.Services;

/// <summary>
/// Computes seniority level from assessment question evaluations.
/// Fully deterministic — no LLM involvement.
/// </summary>
public class SeniorityScorer
{
    private static readonly Dictionary<int, double> LayerWeights = new()
    {
        { 1, 0.20 }, // Screening
        { 2, 0.50 }, // Deep Dive
        { 3, 0.30 }, // Tradeoff Analysis
    };

    public SeniorityResult Score(IEnumerable<AssessmentQuestion> questions)
    {
        var answered = questions.Where(q => q.EvaluatedScore.HasValue).ToList();
        if (answered.Count == 0) return new SeniorityResult(SeniorityLevel.Junior, 0, 0);

        var weightedScore = answered
            .GroupBy(q => q.Layer)
            .Where(g => LayerWeights.ContainsKey(g.Key))
            .Sum(g => g.Average(q => q.EvaluatedScore!.Value) * LayerWeights[g.Key]);

        var normalizedScore = answered
            .GroupBy(q => q.Layer)
            .Select(g => LayerWeights.GetValueOrDefault(g.Key, 0))
            .Sum();

        // Normalize for missing layers
        if (normalizedScore > 0)
            weightedScore /= normalizedScore;

        var seniority = weightedScore switch
        {
            >= 0.88 => SeniorityLevel.Principal,
            >= 0.75 => SeniorityLevel.Staff,
            >= 0.60 => SeniorityLevel.Senior,
            >= 0.40 => SeniorityLevel.Mid,
            _ => SeniorityLevel.Junior
        };

        var confidence = ComputeConfidence(answered);
        return new SeniorityResult(seniority, weightedScore, confidence);
    }

    private static double ComputeConfidence(List<AssessmentQuestion> answered)
    {
        // More questions and layer coverage → higher confidence
        var questionScore = Math.Min(answered.Count / 12.0, 1.0) * 0.6;
        var layerCoverage = answered.Select(q => q.Layer).Distinct().Count() / 3.0 * 0.4;
        return questionScore + layerCoverage;
    }
}

public record SeniorityResult(SeniorityLevel Level, double Score, double Confidence);
