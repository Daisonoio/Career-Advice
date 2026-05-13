namespace Aegis.Domain.Entities;

public class Recommendation : Entity
{
    public int UserId { get; private set; }
    public int? AssessmentId { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public double MarketFitScore { get; private set; }
    public double FutureRiskScore { get; private set; }
    public double CompetitiveScore { get; private set; }
    public double SalaryPercentile { get; private set; }
    public string? LlmExecutiveSummary { get; private set; }
    public double GenerationConfidence { get; private set; }

    public IReadOnlyList<RecommendationPath> Paths => _paths.AsReadOnly();
    private readonly List<RecommendationPath> _paths = [];

    public User User { get; private set; } = default!;

    private Recommendation() { }

    public static Recommendation Create(
        int userId,
        int? assessmentId,
        double marketFitScore,
        double futureRiskScore,
        double competitiveScore,
        double salaryPercentile)
        => new()
        {
            UserId = userId,
            AssessmentId = assessmentId,
            GeneratedAt = DateTime.UtcNow,
            MarketFitScore = marketFitScore,
            FutureRiskScore = futureRiskScore,
            CompetitiveScore = competitiveScore,
            SalaryPercentile = salaryPercentile
        };

    public void SetLlmSummary(string summary)
    {
        LlmExecutiveSummary = summary;
        Touch();
    }

    public void SetConfidence(double confidence)
    {
        GenerationConfidence = confidence;
        Touch();
    }

    public void AddPath(RecommendationPath path)
    {
        _paths.Add(path);
        Touch();
    }
}
