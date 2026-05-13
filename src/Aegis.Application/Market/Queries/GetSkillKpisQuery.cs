using Aegis.Application.Market.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Market.Queries;

public record GetSkillKpisQuery(int SkillId, string? GeoCountry = null) : IRequest<MarketKpiResponse?>;

public class GetSkillKpisQueryHandler : IRequestHandler<GetSkillKpisQuery, MarketKpiResponse?>
{
    private readonly IMarketKpiRepository _kpiRepository;
    private readonly ISkillRepository _skillRepository;

    public GetSkillKpisQueryHandler(IMarketKpiRepository kpiRepository, ISkillRepository skillRepository)
    {
        _kpiRepository = kpiRepository;
        _skillRepository = skillRepository;
    }

    public async Task<MarketKpiResponse?> Handle(GetSkillKpisQuery request, CancellationToken cancellationToken)
    {
        var kpi = await _kpiRepository.GetLatestAsync(request.SkillId, null, request.GeoCountry, cancellationToken);
        if (kpi is null) return null;

        var skill = await _skillRepository.GetByIdAsync(request.SkillId, cancellationToken);

        return new MarketKpiResponse(
            SkillId: kpi.SkillId,
            SkillName: skill?.Name,
            RoleCanonical: kpi.RoleCanonical,
            GeoCountry: kpi.GeoCountry,
            DemandScore: kpi.DemandScore,
            JobCount30d: kpi.JobCount30d,
            JobCountYoyGrowth: kpi.JobCountYoyGrowth,
            SalaryMedian: kpi.SalaryMedian,
            SalaryP25: kpi.SalaryP25,
            SalaryP75: kpi.SalaryP75,
            SalaryCurrency: kpi.SalaryCurrency,
            RemotePremiumPct: kpi.RemotePremiumPct,
            SaturationIndex: kpi.SaturationIndex,
            AIRiskScore: kpi.AIRiskScore,
            AutomationProbability: kpi.AutomationProbability,
            GrowthMomentum: kpi.GrowthMomentum,
            CareerStability: kpi.CareerStability,
            DataConfidence: kpi.DataConfidence,
            ComputedAt: kpi.UpdatedAt);
    }
}
