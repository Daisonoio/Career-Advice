using Aegis.Application.Market.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Market.Queries;

public record GetRoleKpisQuery(string RoleCanonical, string? GeoCountry = null) : IRequest<MarketKpiResponse?>;

public class GetRoleKpisQueryHandler : IRequestHandler<GetRoleKpisQuery, MarketKpiResponse?>
{
    private readonly IMarketKpiRepository _kpiRepository;

    public GetRoleKpisQueryHandler(IMarketKpiRepository kpiRepository)
    {
        _kpiRepository = kpiRepository;
    }

    public async Task<MarketKpiResponse?> Handle(GetRoleKpisQuery request, CancellationToken cancellationToken)
    {
        var kpi = await _kpiRepository.GetLatestAsync(null, request.RoleCanonical, request.GeoCountry, cancellationToken);
        if (kpi is null) return null;

        return new MarketKpiResponse(
            SkillId: kpi.SkillId,
            SkillName: null,
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
