using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Application.Recommendations.DTOs;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace Aegis.Application.Recommendations.Commands;

public record GenerateRecommendationCommand(int UserId) : IRequest<RecommendationResponse>;

public class GenerateRecommendationCommandHandler : IRequestHandler<GenerateRecommendationCommand, RecommendationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IMarketKpiRepository _kpiRepository;
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IAIOrchestrator _aiOrchestrator;

    // Candidate roles for recommendations
    private static readonly string[] CandidateRoles =
    [
        "Backend Engineer",
        "Platform Engineer",
        "Software Architect",
        "DevOps Engineer",
        "AI/ML Engineer",
        "Tech Lead"
    ];

    // Required skills per role (canonical names)
    private static readonly Dictionary<string, List<string>> RoleRequiredSkills = new()
    {
        ["Backend Engineer"] = ["csharp", "dotnet-aspnet-core", "rest-apis", "postgresql", "docker"],
        ["Platform Engineer"] = ["kubernetes", "docker", "terraform", "aws", "cicd"],
        ["Software Architect"] = ["microservices", "event-driven", "ddd", "cqrs", "system-design"],
        ["DevOps Engineer"] = ["kubernetes", "terraform", "cicd", "prometheus-grafana", "ansible"],
        ["AI/ML Engineer"] = ["machine-learning", "python", "llm-integration", "mlops", "tensorflow-pytorch"],
        ["Tech Lead"] = ["team-leadership", "system-design", "code-review", "mentoring", "microservices"]
    };

    public GenerateRecommendationCommandHandler(
        IUserRepository userRepository,
        IAssessmentRepository assessmentRepository,
        IMarketKpiRepository kpiRepository,
        IRecommendationRepository recommendationRepository,
        IAIOrchestrator aiOrchestrator)
    {
        _userRepository = userRepository;
        _assessmentRepository = assessmentRepository;
        _kpiRepository = kpiRepository;
        _recommendationRepository = recommendationRepository;
        _aiOrchestrator = aiOrchestrator;
    }

    public async Task<RecommendationResponse> Handle(GenerateRecommendationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);

        // Determine user's current skills (validated > declared)
        var assessments = await _assessmentRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var lastCompleted = assessments
            .Where(a => a.IsComplete)
            .OrderByDescending(a => a.CompletedAt)
            .FirstOrDefault();

        var userSkillCanonicals = profile?.Skills
            .Select(s => s.Skill?.CanonicalName ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet() ?? new HashSet<string>();

        var currentRole = profile?.CurrentRole ?? "Software Engineer";

        // Score each candidate role
        var scoredPaths = new List<(string Role, MarketKpi? Kpi, double Score, List<string> SkillGaps, double OverlapPct)>();

        foreach (var role in CandidateRoles)
        {
            var kpi = await _kpiRepository.GetLatestAsync(null, role, null, cancellationToken);

            var requiredSkills = RoleRequiredSkills.GetValueOrDefault(role, []);
            var skillGaps = requiredSkills
                .Where(s => !userSkillCanonicals.Contains(s))
                .ToList();

            var overlapCount = requiredSkills.Count - skillGaps.Count;
            var overlapPct = requiredSkills.Count > 0
                ? (double)overlapCount / requiredSkills.Count
                : 0.5;

            // Composite score: demand + growth - ai_risk - transition_penalty
            var demandScore = kpi?.DemandScore ?? 5.0;
            var growthMomentum = kpi?.GrowthMomentum ?? 5.0;
            var aiRisk = kpi?.AIRiskScore ?? 5.0;
            var transitionDifficultyScore = CalculateTransitionDifficulty(skillGaps.Count, requiredSkills.Count);

            var compositeScore =
                (demandScore * 0.3) +
                (growthMomentum * 0.3) +
                (overlapPct * 10 * 0.2) -
                (aiRisk * 0.1) -
                (transitionDifficultyScore * 10 * 0.1);

            scoredPaths.Add((role, kpi, compositeScore, skillGaps, overlapPct));
        }

        // Take top 3 paths
        var top3 = scoredPaths
            .OrderByDescending(p => p.Score)
            .Take(3)
            .ToList();

        // Compute summary scores
        var avgDemand = top3.Average(p => p.Kpi?.DemandScore ?? 5.0);
        var avgAiRisk = top3.Average(p => p.Kpi?.AIRiskScore ?? 5.0);
        var avgOverlap = top3.Average(p => p.OverlapPct);

        var marketFitScore = avgDemand / 10.0;
        var futureRiskScore = avgAiRisk / 10.0;
        var competitiveScore = avgOverlap;
        var salaryPercentile = 0.5; // Default without reference data

        var recommendation = Recommendation.Create(
            userId: request.UserId,
            assessmentId: lastCompleted?.Id,
            marketFitScore: marketFitScore,
            futureRiskScore: futureRiskScore,
            competitiveScore: competitiveScore,
            salaryPercentile: salaryPercentile);

        recommendation.SetConfidence(0.75);

        await _recommendationRepository.AddAsync(recommendation, cancellationToken);

        // Build paths with LLM rationale
        var pathDtos = new List<RecommendationPathDto>();
        for (int i = 0; i < top3.Count; i++)
        {
            var (role, kpi, _, skillGaps, overlapPct) = top3[i];
            var requiredSkills = RoleRequiredSkills.GetValueOrDefault(role, []);
            var difficultyString = GetTransitionDifficultyString(skillGaps.Count, requiredSkills.Count);
            var estimatedMonths = CalculateEstimatedMonths(skillGaps.Count);
            var salaryUplift = CalculateSalaryUplift(kpi, profile?.SalaryExpectation?.Midpoint());

            // Generate LLM rationale
            string? rationale = null;
            try
            {
                var kpiData = new Dictionary<string, double>();
                if (kpi?.DemandScore.HasValue == true) kpiData["demand_score"] = kpi.DemandScore.Value;
                if (kpi?.GrowthMomentum.HasValue == true) kpiData["growth_momentum"] = kpi.GrowthMomentum.Value;
                if (kpi?.AIRiskScore.HasValue == true) kpiData["ai_risk_score"] = kpi.AIRiskScore.Value;
                if (kpi?.SalaryMedian.HasValue == true) kpiData["salary_median"] = (double)kpi.SalaryMedian.Value;

                var explainCtx = new RecommendationExplainContext(
                    TargetRole: role,
                    SalaryUpliftPct: salaryUplift ?? 0,
                    TransitionDifficulty: difficultyString,
                    MarketKpis: kpiData,
                    CurrentRole: currentRole);

                rationale = await _aiOrchestrator.ExplainRecommendationAsync(explainCtx, cancellationToken);
            }
            catch
            {
                // Non-critical
            }

            var path = RecommendationPath.Create(
                recommendationId: recommendation.Id,
                targetRole: role,
                targetRoleCanonical: role.ToLowerInvariant().Replace(" ", "-"),
                rank: i + 1,
                salaryUpliftPct: salaryUplift,
                transitionDifficulty: difficultyString,
                estimatedMonths: estimatedMonths,
                skillOverlapPct: overlapPct,
                skillGaps: JsonSerializer.Serialize(skillGaps),
                marketDemandScore: kpi?.DemandScore,
                aiRiskScore: kpi?.AIRiskScore,
                growthMomentum: kpi?.GrowthMomentum,
                confidence: 0.75);

            if (rationale is not null)
                path.SetLlmRationale(rationale);

            recommendation.AddPath(path);

            pathDtos.Add(new RecommendationPathDto(
                Id: path.Id,
                TargetRole: role,
                Rank: i + 1,
                SalaryUpliftPct: salaryUplift,
                TransitionDifficulty: difficultyString,
                EstimatedMonths: estimatedMonths,
                SkillOverlapPct: overlapPct,
                SkillGaps: skillGaps,
                MarketDemandScore: kpi?.DemandScore,
                AIRiskScore: kpi?.AIRiskScore,
                GrowthMomentum: kpi?.GrowthMomentum,
                Confidence: 0.75,
                LlmRationale: rationale));
        }

        // Generate executive summary
        string? executiveSummary = null;
        try
        {
            var insightRequest = new CareerInsightRequest(
                CurrentRole: currentRole,
                YearsExperience: profile?.YearsExperience ?? 0,
                ValidatedSkills: userSkillCanonicals.ToList(),
                MarketKpis: top3.SelectMany(p => new[]
                {
                    (Key: $"{p.Role}_demand", Value: p.Kpi?.DemandScore ?? 5.0),
                    (Key: $"{p.Role}_growth", Value: p.Kpi?.GrowthMomentum ?? 5.0)
                }).ToDictionary(x => x.Key, x => x.Value),
                RecommendedPaths: top3.Select(p => p.Role).ToList());

            executiveSummary = await _aiOrchestrator.GenerateCareerInsightAsync(insightRequest, cancellationToken);
            recommendation.SetLlmSummary(executiveSummary);
        }
        catch
        {
            // Non-critical
        }

        await _recommendationRepository.UpdateAsync(recommendation, cancellationToken);

        return new RecommendationResponse(
            Id: recommendation.Id,
            GeneratedAt: recommendation.GeneratedAt,
            MarketFitScore: marketFitScore,
            FutureRiskScore: futureRiskScore,
            CompetitiveScore: competitiveScore,
            SalaryPercentile: salaryPercentile,
            LlmExecutiveSummary: executiveSummary,
            GenerationConfidence: 0.75,
            Paths: pathDtos);
    }

    private static double CalculateTransitionDifficulty(int gapCount, int totalRequired)
    {
        if (totalRequired == 0) return 0.5;
        return (double)gapCount / totalRequired;
    }

    private static string GetTransitionDifficultyString(int gapCount, int totalRequired)
    {
        var ratio = totalRequired > 0 ? (double)gapCount / totalRequired : 0;
        return ratio switch
        {
            < 0.2 => "Easy",
            < 0.4 => "Moderate",
            < 0.7 => "Challenging",
            _ => "Hard"
        };
    }

    private static int CalculateEstimatedMonths(int gapCount)
        => gapCount switch
        {
            0 => 3,
            1 => 6,
            2 => 9,
            3 => 12,
            _ => gapCount * 4
        };

    private static double? CalculateSalaryUplift(MarketKpi? kpi, decimal? currentSalaryMidpoint)
    {
        if (kpi?.SalaryMedian is null || currentSalaryMidpoint is null || currentSalaryMidpoint == 0)
            return null;

        var uplift = ((double)kpi.SalaryMedian.Value - (double)currentSalaryMidpoint.Value) / (double)currentSalaryMidpoint.Value * 100;
        return Math.Round(uplift, 1);
    }
}
