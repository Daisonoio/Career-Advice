using Aegis.Application.Monitoring.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Monitoring.Queries;

public record GetCompetitivenessHistoryQuery(int UserId, int Limit = 12) : IRequest<CompetitivenessHistoryResponse>;

public class GetCompetitivenessHistoryQueryHandler
    : IRequestHandler<GetCompetitivenessHistoryQuery, CompetitivenessHistoryResponse>
{
    private readonly IMonitoringSnapshotRepository _snapshotRepository;

    public GetCompetitivenessHistoryQueryHandler(IMonitoringSnapshotRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<CompetitivenessHistoryResponse> Handle(
        GetCompetitivenessHistoryQuery request, CancellationToken cancellationToken)
    {
        var snapshots = await _snapshotRepository.GetByUserIdAsync(
            request.UserId, request.Limit, cancellationToken);

        var dtos = snapshots
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new MonitoringSnapshotDto(
                SnapshotDate:    s.SnapshotDate,
                CompetitiveScore: s.CompetitiveScore,
                MarketFitScore:  s.MarketFitScore,
                SalaryPercentile: s.SalaryPercentile,
                FutureRiskScore: s.FutureRiskScore))
            .ToList();

        return new CompetitivenessHistoryResponse(dtos);
    }
}
