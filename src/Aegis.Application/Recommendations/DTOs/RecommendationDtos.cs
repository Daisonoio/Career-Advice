namespace Aegis.Application.Recommendations.DTOs;

public record RecommendationResponse(
    int Id,
    DateTime GeneratedAt,
    double MarketFitScore,
    double FutureRiskScore,
    double CompetitiveScore,
    double SalaryPercentile,
    string? LlmExecutiveSummary,
    double GenerationConfidence,
    List<RecommendationPathDto> Paths);

public record RecommendationPathDto(
    int Id,
    string TargetRole,
    int Rank,
    double? SalaryUpliftPct,
    string TransitionDifficulty,
    int? EstimatedMonths,
    double? SkillOverlapPct,
    List<string> SkillGaps,
    double? MarketDemandScore,
    double? AIRiskScore,
    double? GrowthMomentum,
    double? Confidence,
    string? LlmRationale);
