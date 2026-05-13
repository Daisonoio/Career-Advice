using Aegis.Application.Market.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Market.Queries;

public record GetMarketTrendsQuery(string? GeoCountry, int TopN = 10) : IRequest<MarketTrendsResponse>;

public class GetMarketTrendsQueryHandler : IRequestHandler<GetMarketTrendsQuery, MarketTrendsResponse>
{
    private readonly IMarketKpiRepository _kpiRepository;
    private readonly ISkillRepository _skillRepository;

    public GetMarketTrendsQueryHandler(
        IMarketKpiRepository kpiRepository,
        ISkillRepository skillRepository)
    {
        _kpiRepository = kpiRepository;
        _skillRepository = skillRepository;
    }

    public async Task<MarketTrendsResponse> Handle(GetMarketTrendsQuery request, CancellationToken cancellationToken)
    {
        var topKpis = await _kpiRepository.GetTopByGrowthMomentumAsync(request.GeoCountry, request.TopN * 2, cancellationToken);

        var skillIds = topKpis.Where(k => k.SkillId.HasValue).Select(k => k.SkillId!.Value).Distinct().ToList();
        var allSkills = await _skillRepository.GetAllWithCategoriesAsync(cancellationToken);
        var skillMap = allSkills.Where(s => skillIds.Contains(s.Id)).ToDictionary(s => s.Id);

        var skillKpis = topKpis
            .Where(k => k.SkillId.HasValue && skillMap.ContainsKey(k.SkillId.Value))
            .ToList();

        var emerging = skillKpis
            .Where(k => k.GrowthMomentum.HasValue && k.GrowthMomentum.Value >= 5.0)
            .OrderByDescending(k => k.GrowthMomentum)
            .Take(request.TopN)
            .Select(k => new SkillTrendDto(
                SkillId: k.SkillId!.Value,
                SkillName: skillMap[k.SkillId.Value].Name,
                GrowthMomentum: k.GrowthMomentum!.Value,
                DemandScore: k.DemandScore ?? 0.0,
                SalaryMedian: (double?)k.SalaryMedian))
            .ToList();

        var declining = skillKpis
            .Where(k => k.GrowthMomentum.HasValue && k.GrowthMomentum.Value < 5.0)
            .OrderBy(k => k.GrowthMomentum)
            .Take(request.TopN)
            .Select(k => new SkillTrendDto(
                SkillId: k.SkillId!.Value,
                SkillName: skillMap[k.SkillId.Value].Name,
                GrowthMomentum: k.GrowthMomentum!.Value,
                DemandScore: k.DemandScore ?? 0.0,
                SalaryMedian: (double?)k.SalaryMedian))
            .ToList();

        return new MarketTrendsResponse(EmergingSkills: emerging, DecliningSkills: declining);
    }
}
