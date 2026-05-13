using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Application.Profile.Services;
using Aegis.Application.Recommendations.DTOs;
using Aegis.Application.Recommendations.Services;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Aegis.Domain.ValueObjects;
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
    private readonly SkillPrioritizationService _skillPrioritization;
    private readonly ProfileEnrichmentService _enrichment;

    public GenerateRecommendationCommandHandler(
        IUserRepository userRepository,
        IAssessmentRepository assessmentRepository,
        IMarketKpiRepository kpiRepository,
        IRecommendationRepository recommendationRepository,
        IAIOrchestrator aiOrchestrator,
        SkillPrioritizationService skillPrioritization,
        ProfileEnrichmentService enrichment)
    {
        _userRepository = userRepository;
        _assessmentRepository = assessmentRepository;
        _kpiRepository = kpiRepository;
        _recommendationRepository = recommendationRepository;
        _aiOrchestrator = aiOrchestrator;
        _skillPrioritization = skillPrioritization;
        _enrichment = enrichment;
    }

    public async Task<RecommendationResponse> Handle(GenerateRecommendationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken);

        var assessments = await _assessmentRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var lastCompleted = assessments
            .Where(a => a.IsComplete)
            .OrderByDescending(a => a.CompletedAt)
            .FirstOrDefault();

        var userSkillCanonicals = profile?.Skills
            .Select(s => s.Skill?.CanonicalName ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet() ?? [];

        var currentRole = profile?.CurrentRole ?? "Software Engineer";

        // Score each candidate role drawn from the single-source-of-truth dictionary
        var scoredPaths = new List<ScoredPath>();

        foreach (var role in SkillPrioritizationService.RoleRequiredSkills.Keys)
        {
            var kpi = await _kpiRepository.GetLatestAsync(null, role, null, cancellationToken);

            var requiredSkills = SkillPrioritizationService.RoleRequiredSkills[role];

            var demandScore    = kpi?.DemandScore    ?? 5.0;
            var growthMomentum = kpi?.GrowthMomentum ?? 5.0;
            var aiRisk         = kpi?.AIRiskScore     ?? 5.0;

            var prioritizedGaps = _skillPrioritization.Prioritize(
                targetRole:        role,
                userSkillCanonicals: userSkillCanonicals,
                roleDemandScore:   demandScore,
                roleGrowthMomentum: growthMomentum,
                roleAiRisk:        aiRisk);

            var overlapCount = requiredSkills.Count - prioritizedGaps.Count;
            var overlapPct   = requiredSkills.Count > 0
                ? (double)overlapCount / requiredSkills.Count
                : 0.5;

            var transitionDifficultyScore = requiredSkills.Count > 0
                ? (double)prioritizedGaps.Count / requiredSkills.Count
                : 0.5;

            var compositeScore =
                (demandScore    * 0.3) +
                (growthMomentum * 0.3) +
                (overlapPct * 10 * 0.2) -
                (aiRisk          * 0.1) -
                (transitionDifficultyScore * 10 * 0.1);

            scoredPaths.Add(new ScoredPath(role, kpi, compositeScore, prioritizedGaps, overlapPct));
        }

        var top3 = scoredPaths
            .OrderByDescending(p => p.Score)
            .Take(3)
            .ToList();

        var avgDemand  = top3.Average(p => p.Kpi?.DemandScore    ?? 5.0);
        var avgAiRisk  = top3.Average(p => p.Kpi?.AIRiskScore     ?? 5.0);
        var avgOverlap = top3.Average(p => p.OverlapPct);

        var marketFitScore  = avgDemand / 10.0;
        var futureRiskScore = avgAiRisk / 10.0;
        var competitiveScore = avgOverlap;

        // Real salary percentile — reuses the same logic as the profile read path
        var currentRoleKpi = await _kpiRepository.GetLatestAsync(
            null, currentRole, profile?.LocationCountry, cancellationToken);

        if (currentRoleKpi is null && profile?.LocationCountry is not null)
            currentRoleKpi = await _kpiRepository.GetLatestAsync(null, currentRole, null, cancellationToken);

        var salaryPercentile = _enrichment.ComputeSalaryPercentile(
            profile?.SalaryExpectation?.Midpoint(), currentRoleKpi) ?? 0.5;

        var recommendation = Recommendation.Create(
            userId:            request.UserId,
            assessmentId:      lastCompleted?.Id,
            marketFitScore:    marketFitScore,
            futureRiskScore:   futureRiskScore,
            competitiveScore:  competitiveScore,
            salaryPercentile:  salaryPercentile);

        recommendation.SetConfidence(0.75);

        await _recommendationRepository.AddAsync(recommendation, cancellationToken);

        var pathDtos = new List<RecommendationPathDto>();
        for (int i = 0; i < top3.Count; i++)
        {
            var sp = top3[i];
            var requiredSkills   = SkillPrioritizationService.RoleRequiredSkills[sp.Role];
            var difficultyString = GetTransitionDifficultyString(sp.SkillGaps.Count, requiredSkills.Count);
            var estimatedMonths  = CalculateEstimatedMonths(sp.SkillGaps);
            var salaryUplift     = CalculateSalaryUplift(sp.Kpi, profile?.SalaryExpectation?.Midpoint());

            string? rationale = null;
            try
            {
                var kpiData = new Dictionary<string, double>();
                if (sp.Kpi?.DemandScore.HasValue    == true) kpiData["demand_score"]    = sp.Kpi.DemandScore.Value;
                if (sp.Kpi?.GrowthMomentum.HasValue == true) kpiData["growth_momentum"] = sp.Kpi.GrowthMomentum.Value;
                if (sp.Kpi?.AIRiskScore.HasValue    == true) kpiData["ai_risk_score"]   = sp.Kpi.AIRiskScore.Value;
                if (sp.Kpi?.SalaryMedian.HasValue   == true) kpiData["salary_median"]   = (double)sp.Kpi.SalaryMedian.Value;

                var explainCtx = new RecommendationExplainContext(
                    TargetRole:           sp.Role,
                    SalaryUpliftPct:      salaryUplift ?? 0,
                    TransitionDifficulty: difficultyString,
                    MarketKpis:           kpiData,
                    CurrentRole:          currentRole);

                rationale = await _aiOrchestrator.ExplainRecommendationAsync(explainCtx, cancellationToken);
            }
            catch
            {
                // Non-critical — path is still returned without rationale
            }

            var path = RecommendationPath.Create(
                recommendationId:    recommendation.Id,
                targetRole:          sp.Role,
                targetRoleCanonical: sp.Role.ToLowerInvariant().Replace(" ", "-"),
                rank:                i + 1,
                salaryUpliftPct:     salaryUplift,
                transitionDifficulty: difficultyString,
                estimatedMonths:     estimatedMonths,
                skillOverlapPct:     sp.OverlapPct,
                skillGaps:           JsonSerializer.Serialize(sp.SkillGaps),
                marketDemandScore:   sp.Kpi?.DemandScore,
                aiRiskScore:         sp.Kpi?.AIRiskScore,
                growthMomentum:      sp.Kpi?.GrowthMomentum,
                confidence:          0.75);

            if (rationale is not null)
                path.SetLlmRationale(rationale);

            recommendation.AddPath(path);

            pathDtos.Add(new RecommendationPathDto(
                Id:                   path.Id,
                TargetRole:           sp.Role,
                Rank:                 i + 1,
                SalaryUpliftPct:      salaryUplift,
                TransitionDifficulty: difficultyString,
                EstimatedMonths:      estimatedMonths,
                SkillOverlapPct:      sp.OverlapPct,
                SkillGaps:            sp.SkillGaps.Select(MapGapToDto).ToList(),
                MarketDemandScore:    sp.Kpi?.DemandScore,
                AIRiskScore:          sp.Kpi?.AIRiskScore,
                GrowthMomentum:       sp.Kpi?.GrowthMomentum,
                Confidence:           0.75,
                LlmRationale:         rationale));
        }

        string? executiveSummary = null;
        try
        {
            var insightRequest = new CareerInsightRequest(
                CurrentRole:      currentRole,
                YearsExperience:  profile?.YearsExperience ?? 0,
                ValidatedSkills:  userSkillCanonicals.ToList(),
                MarketKpis: top3.SelectMany(p => new[]
                {
                    (Key: $"{p.Role}_demand", Value: p.Kpi?.DemandScore    ?? 5.0),
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
            Id:                    recommendation.Id,
            GeneratedAt:           recommendation.GeneratedAt,
            MarketFitScore:        marketFitScore,
            FutureRiskScore:       futureRiskScore,
            CompetitiveScore:      competitiveScore,
            SalaryPercentile:      salaryPercentile,
            LlmExecutiveSummary:   executiveSummary,
            GenerationConfidence:  0.75,
            Paths:                 pathDtos);
    }

    private static PrioritizedSkillGapDto MapGapToDto(PrioritizedSkillGap g) =>
        new(g.SkillName, g.SkillCanonical, g.Type, g.SkillRoi,
            g.SalaryPremiumPct, g.EstimatedWeeks, g.LearningDifficulty);

    private static string GetTransitionDifficultyString(int gapCount, int totalRequired)
    {
        var ratio = totalRequired > 0 ? (double)gapCount / totalRequired : 0;
        return ratio switch
        {
            < 0.2 => "Easy",
            < 0.4 => "Moderate",
            < 0.7 => "Challenging",
            _     => "Hard"
        };
    }

    // Estimate transition time from the sum of per-skill week estimates (converted to months)
    private static int CalculateEstimatedMonths(List<PrioritizedSkillGap> gaps)
    {
        if (gaps.Count == 0) return 3;
        var totalWeeks = gaps.Sum(g => g.EstimatedWeeks);
        var months = (int)Math.Ceiling(totalWeeks / 4.33);
        return Math.Max(3, months);
    }

    private static double? CalculateSalaryUplift(MarketKpi? kpi, decimal? currentSalaryMidpoint)
    {
        if (kpi?.SalaryMedian is null || currentSalaryMidpoint is null || currentSalaryMidpoint == 0)
            return null;

        var uplift = ((double)kpi.SalaryMedian.Value - (double)currentSalaryMidpoint.Value)
                     / (double)currentSalaryMidpoint.Value * 100;
        return Math.Round(uplift, 1);
    }

    private record ScoredPath(
        string Role,
        MarketKpi? Kpi,
        double Score,
        List<PrioritizedSkillGap> SkillGaps,
        double OverlapPct);
}
