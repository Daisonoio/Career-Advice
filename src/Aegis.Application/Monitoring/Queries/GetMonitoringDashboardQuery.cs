using Aegis.Application.Monitoring.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Monitoring.Queries;

public record GetMonitoringDashboardQuery(int UserId) : IRequest<MonitoringDashboardResponse>;

public class GetMonitoringDashboardQueryHandler : IRequestHandler<GetMonitoringDashboardQuery, MonitoringDashboardResponse>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly IAssessmentRepository _assessmentRepository;
    private readonly IAlertRepository _alertRepository;

    public GetMonitoringDashboardQueryHandler(
        IRecommendationRepository recommendationRepository,
        IAssessmentRepository assessmentRepository,
        IAlertRepository alertRepository)
    {
        _recommendationRepository = recommendationRepository;
        _assessmentRepository = assessmentRepository;
        _alertRepository = alertRepository;
    }

    public async Task<MonitoringDashboardResponse> Handle(GetMonitoringDashboardQuery request, CancellationToken cancellationToken)
    {
        var latestRecommendation = await _recommendationRepository.GetLatestByUserIdAsync(request.UserId, cancellationToken);
        var unreadAlerts = await _alertRepository.GetUnreadByUserIdAsync(request.UserId, cancellationToken);
        var assessments = await _assessmentRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var lastAssessment = assessments
            .Where(a => a.IsComplete)
            .OrderByDescending(a => a.CompletedAt)
            .FirstOrDefault();

        return new MonitoringDashboardResponse(
            CompetitiveScore: latestRecommendation?.CompetitiveScore,
            MarketFitScore: latestRecommendation?.MarketFitScore,
            SalaryPercentile: latestRecommendation?.SalaryPercentile,
            FutureRiskScore: latestRecommendation?.FutureRiskScore,
            UnreadAlertsCount: unreadAlerts.Count,
            LastAssessmentDate: lastAssessment?.CompletedAt,
            LastRecommendationDate: latestRecommendation?.GeneratedAt);
    }
}
