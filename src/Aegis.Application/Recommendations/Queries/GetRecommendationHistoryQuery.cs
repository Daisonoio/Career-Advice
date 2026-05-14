using Aegis.Application.Recommendations.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Recommendations.Queries;

public record GetRecommendationHistoryQuery(int UserId, int Limit = 10)
    : IRequest<List<RecommendationSummaryDto>>;

public class GetRecommendationHistoryQueryHandler
    : IRequestHandler<GetRecommendationHistoryQuery, List<RecommendationSummaryDto>>
{
    private readonly IRecommendationRepository _recommendationRepository;

    public GetRecommendationHistoryQueryHandler(IRecommendationRepository recommendationRepository)
    {
        _recommendationRepository = recommendationRepository;
    }

    public async Task<List<RecommendationSummaryDto>> Handle(
        GetRecommendationHistoryQuery request, CancellationToken cancellationToken)
    {
        var recommendations = await _recommendationRepository.GetByUserIdAsync(
            request.UserId, request.Limit, cancellationToken);

        return recommendations.Select(r => new RecommendationSummaryDto(
            Id:                   r.Id,
            GeneratedAt:          r.GeneratedAt,
            MarketFitScore:       r.MarketFitScore,
            FutureRiskScore:      r.FutureRiskScore,
            CompetitiveScore:     r.CompetitiveScore,
            SalaryPercentile:     r.SalaryPercentile,
            GenerationConfidence: r.GenerationConfidence,
            TopRoles:             r.Paths
                                   .OrderBy(p => p.Rank)
                                   .Select(p => p.TargetRole)
                                   .ToList())).ToList();
    }
}
