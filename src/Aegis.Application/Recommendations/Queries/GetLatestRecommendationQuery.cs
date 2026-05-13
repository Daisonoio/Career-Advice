using Aegis.Application.Recommendations.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace Aegis.Application.Recommendations.Queries;

public record GetLatestRecommendationQuery(int UserId) : IRequest<RecommendationResponse?>;

public class GetLatestRecommendationQueryHandler : IRequestHandler<GetLatestRecommendationQuery, RecommendationResponse?>
{
    private readonly IRecommendationRepository _recommendationRepository;

    public GetLatestRecommendationQueryHandler(IRecommendationRepository recommendationRepository)
    {
        _recommendationRepository = recommendationRepository;
    }

    public async Task<RecommendationResponse?> Handle(GetLatestRecommendationQuery request, CancellationToken cancellationToken)
    {
        var recommendation = await _recommendationRepository.GetLatestByUserIdAsync(request.UserId, cancellationToken);
        if (recommendation is null) return null;

        var paths = recommendation.Paths.Select(p => new RecommendationPathDto(
            Id: p.Id,
            TargetRole: p.TargetRole,
            Rank: p.Rank,
            SalaryUpliftPct: p.SalaryUpliftPct,
            TransitionDifficulty: p.TransitionDifficulty,
            EstimatedMonths: p.EstimatedMonths,
            SkillOverlapPct: p.SkillOverlapPct,
            SkillGaps: DeserializeSkillGaps(p.SkillGaps),
            MarketDemandScore: p.MarketDemandScore,
            AIRiskScore: p.AIRiskScore,
            GrowthMomentum: p.GrowthMomentum,
            Confidence: p.Confidence,
            LlmRationale: p.LlmRationale)).ToList();

        return new RecommendationResponse(
            Id: recommendation.Id,
            GeneratedAt: recommendation.GeneratedAt,
            MarketFitScore: recommendation.MarketFitScore,
            FutureRiskScore: recommendation.FutureRiskScore,
            CompetitiveScore: recommendation.CompetitiveScore,
            SalaryPercentile: recommendation.SalaryPercentile,
            LlmExecutiveSummary: recommendation.LlmExecutiveSummary,
            GenerationConfidence: recommendation.GenerationConfidence,
            Paths: paths);
    }

    private static List<PrioritizedSkillGapDto> DeserializeSkillGaps(string skillGapsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<PrioritizedSkillGapDto>>(skillGapsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
