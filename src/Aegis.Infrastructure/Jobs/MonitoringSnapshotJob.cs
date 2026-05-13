using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Infrastructure.Jobs;

// Weekly Hangfire job. Captures a point-in-time snapshot of each user's
// career scores from their latest recommendation for trend chart rendering.
public class MonitoringSnapshotJob
{
    private readonly IUserRepository               _userRepository;
    private readonly IRecommendationRepository     _recommendationRepository;
    private readonly IMonitoringSnapshotRepository _snapshotRepository;
    private readonly ILogger<MonitoringSnapshotJob> _logger;

    public MonitoringSnapshotJob(
        IUserRepository userRepository,
        IRecommendationRepository recommendationRepository,
        IMonitoringSnapshotRepository snapshotRepository,
        ILogger<MonitoringSnapshotJob> logger)
    {
        _userRepository           = userRepository;
        _recommendationRepository = recommendationRepository;
        _snapshotRepository       = snapshotRepository;
        _logger                   = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("MonitoringSnapshotJob: started");

        var userIds  = await _userRepository.GetAllUserIdsWithProfileAsync(ct);
        var created  = 0;
        var skipped  = 0;
        var errors   = 0;

        foreach (var userId in userIds)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var recommendation = await _recommendationRepository.GetLatestByUserIdAsync(userId, ct);
                if (recommendation is null)
                {
                    skipped++;
                    continue;
                }

                var snapshot = UserMonitoringSnapshot.Create(
                    userId:          userId,
                    competitiveScore: recommendation.CompetitiveScore,
                    marketFitScore:  recommendation.MarketFitScore,
                    salaryPercentile: recommendation.SalaryPercentile,
                    futureRiskScore: recommendation.FutureRiskScore);

                await _snapshotRepository.AddAsync(snapshot, ct);
                created++;
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogWarning(ex, "MonitoringSnapshotJob: error for user {UserId}", userId);
            }
        }

        _logger.LogInformation(
            "MonitoringSnapshotJob: completed. Created={Created}, Skipped={Skipped}, Errors={Errors}",
            created, skipped, errors);
    }
}
