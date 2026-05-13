namespace Aegis.Domain.Entities;

public class RecommendationPath : Entity
{
    public int RecommendationId { get; private set; }
    public string TargetRole { get; private set; } = default!;
    public string TargetRoleCanonical { get; private set; } = default!;
    public int Rank { get; private set; }
    public double? SalaryUpliftPct { get; private set; }
    public string TransitionDifficulty { get; private set; } = default!;
    public int? EstimatedMonths { get; private set; }
    public double? SkillOverlapPct { get; private set; }
    public string SkillGaps { get; private set; } = "[]";  // JSON array
    public double? MarketDemandScore { get; private set; }
    public double? AIRiskScore { get; private set; }
    public double? GrowthMomentum { get; private set; }
    public double? Confidence { get; private set; }
    public string? LlmRationale { get; private set; }

    public Recommendation Recommendation { get; private set; } = default!;

    private RecommendationPath() { }

    public static RecommendationPath Create(
        int recommendationId,
        string targetRole,
        string targetRoleCanonical,
        int rank,
        double? salaryUpliftPct,
        string transitionDifficulty,
        int? estimatedMonths,
        double? skillOverlapPct,
        string skillGaps,
        double? marketDemandScore,
        double? aiRiskScore,
        double? growthMomentum,
        double? confidence)
        => new()
        {
            RecommendationId = recommendationId,
            TargetRole = targetRole,
            TargetRoleCanonical = targetRoleCanonical,
            Rank = rank,
            SalaryUpliftPct = salaryUpliftPct,
            TransitionDifficulty = transitionDifficulty,
            EstimatedMonths = estimatedMonths,
            SkillOverlapPct = skillOverlapPct,
            SkillGaps = skillGaps,
            MarketDemandScore = marketDemandScore,
            AIRiskScore = aiRiskScore,
            GrowthMomentum = growthMomentum,
            Confidence = confidence
        };

    public void SetLlmRationale(string rationale)
    {
        LlmRationale = rationale;
        Touch();
    }
}
