namespace Aegis.Domain.Entities;

// Weekly snapshot of a user's computed career scores.
// Used to render the competitiveness history chart and to detect
// score declines that trigger CompetitivenessDecline alerts.
public class UserMonitoringSnapshot : Entity
{
    public int UserId { get; private set; }
    public DateTime SnapshotDate { get; private set; }
    public double? CompetitiveScore { get; private set; }
    public double? MarketFitScore { get; private set; }
    public double? SalaryPercentile { get; private set; }
    public double? FutureRiskScore { get; private set; }

    public User User { get; private set; } = default!;

    private UserMonitoringSnapshot() { }

    public static UserMonitoringSnapshot Create(
        int userId,
        double? competitiveScore,
        double? marketFitScore,
        double? salaryPercentile,
        double? futureRiskScore)
        => new()
        {
            UserId           = userId,
            SnapshotDate     = DateTime.UtcNow,
            CompetitiveScore = competitiveScore,
            MarketFitScore   = marketFitScore,
            SalaryPercentile = salaryPercentile,
            FutureRiskScore  = futureRiskScore
        };
}
