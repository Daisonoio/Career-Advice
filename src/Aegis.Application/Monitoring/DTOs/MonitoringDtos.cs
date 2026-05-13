namespace Aegis.Application.Monitoring.DTOs;

public record MonitoringDashboardResponse(
    double? CompetitiveScore,
    double? MarketFitScore,
    double? SalaryPercentile,
    double? FutureRiskScore,
    int UnreadAlertsCount,
    DateTime? LastAssessmentDate,
    DateTime? LastRecommendationDate);

public record AlertResponse(
    int Id,
    string AlertType,
    string Title,
    string? Body,
    string Severity,
    DateTime CreatedAt,
    bool IsRead);

public record CompetitivenessHistoryResponse(
    List<MonitoringSnapshotDto> Snapshots);

public record MonitoringSnapshotDto(
    DateTime SnapshotDate,
    double? CompetitiveScore,
    double? MarketFitScore,
    double? SalaryPercentile,
    double? FutureRiskScore);
