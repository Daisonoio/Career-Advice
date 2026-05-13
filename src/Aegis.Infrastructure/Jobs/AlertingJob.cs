using Aegis.Application.Monitoring.Services;
using Aegis.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Infrastructure.Jobs;

// Daily Hangfire job. Iterates every user that has a profile and runs
// AlertingService.EvaluateForUserAsync to fire threshold-based alerts.
// Each user is processed independently; failures are logged and skipped.
public class AlertingJob
{
    private readonly IUserRepository   _userRepository;
    private readonly AlertingService   _alertingService;
    private readonly ILogger<AlertingJob> _logger;

    public AlertingJob(
        IUserRepository userRepository,
        AlertingService alertingService,
        ILogger<AlertingJob> logger)
    {
        _userRepository  = userRepository;
        _alertingService = alertingService;
        _logger          = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("AlertingJob: started");

        var userIds = await _userRepository.GetAllUserIdsWithProfileAsync(ct);
        var processed = 0;
        var errors    = 0;

        foreach (var userId in userIds)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var profile = await _userRepository.GetProfileByUserIdAsync(userId, ct);
                if (profile is null) continue;

                await _alertingService.EvaluateForUserAsync(profile, ct);
                processed++;
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogWarning(ex, "AlertingJob: error evaluating user {UserId}", userId);
            }
        }

        _logger.LogInformation(
            "AlertingJob: completed. Processed={Processed}, Errors={Errors}, Total={Total}",
            processed, errors, userIds.Count);
    }
}
