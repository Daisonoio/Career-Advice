using Aegis.Application.Profile.Services;
using Aegis.Application.Recommendations.Services;
using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using Aegis.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Application.Monitoring.Services;

// Evaluates alert conditions for a single user and persists any new alerts.
// Called daily by AlertingJob. All thresholds are deterministic — no LLM involved.
//
// Alert types and triggers:
//   MarketDemandDrop        — role DemandScore < 5.0
//   SalaryChange            — salary percentile < 0.35  (below 35th percentile)
//   EmergingSkill           — a missing role skill has GrowthMomentum >= 8.0
//   AIRiskIncrease          — role AIRiskScore > 7.0
//   CompetitivenessDecline  — latest recommendation CompetitiveScore < 0.40
//
// Deduplication: the same alert type is not re-fired within 7 days.
public class AlertingService
{
    private static readonly TimeSpan DedupWindow = TimeSpan.FromDays(7);

    private const double DemandDropThreshold          = 5.0;
    private const double SalaryPercentileLowThreshold = 0.35;
    private const double EmergingSkillGrowthThreshold = 8.0;
    private const double AiRiskHighThreshold          = 7.0;
    private const double CompetitivenessLowThreshold  = 0.40;

    private readonly IAlertRepository            _alertRepository;
    private readonly IMarketKpiRepository        _kpiRepository;
    private readonly IRecommendationRepository   _recommendationRepository;
    private readonly ProfileEnrichmentService    _enrichment;
    private readonly ILogger<AlertingService>    _logger;

    public AlertingService(
        IAlertRepository alertRepository,
        IMarketKpiRepository kpiRepository,
        IRecommendationRepository recommendationRepository,
        ProfileEnrichmentService enrichment,
        ILogger<AlertingService> logger)
    {
        _alertRepository          = alertRepository;
        _kpiRepository            = kpiRepository;
        _recommendationRepository = recommendationRepository;
        _enrichment               = enrichment;
        _logger                   = logger;
    }

    public async Task EvaluateForUserAsync(UserProfile profile, CancellationToken ct)
    {
        var userId      = profile.UserId;
        var currentRole = profile.CurrentRole;

        if (string.IsNullOrWhiteSpace(currentRole))
            return;

        MarketKpi? roleKpi = null;
        try
        {
            roleKpi = await _kpiRepository.GetLatestAsync(null, currentRole, profile.LocationCountry, ct);
            if (roleKpi is null)
                roleKpi = await _kpiRepository.GetLatestAsync(null, currentRole, null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AlertingService: failed to fetch KPI for role {Role}", currentRole);
        }

        var latestRec = await _recommendationRepository.GetLatestByUserIdAsync(userId, ct);

        var userSkillCanonicals = profile.Skills
            .Select(s => s.Skill?.CanonicalName ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await TryFireAsync(userId, AlertType.MarketDemandDrop,
            condition: roleKpi?.DemandScore is not null && roleKpi.DemandScore < DemandDropThreshold,
            title:     $"Market demand dropping for {currentRole}",
            body:      $"Demand score for {currentRole} is {roleKpi?.DemandScore:F1}/10, below the 5.0 threshold. Consider exploring adjacent roles.",
            severity:  "High",
            ct);

        var salaryPercentile = _enrichment.ComputeSalaryPercentile(
            profile.SalaryExpectation?.Midpoint(), roleKpi);

        await TryFireAsync(userId, AlertType.SalaryChange,
            condition: salaryPercentile is not null && salaryPercentile < SalaryPercentileLowThreshold,
            title:     "Your salary is below market rate",
            body:      $"Your current salary falls below the 35th percentile for {currentRole}. The market median is {roleKpi?.SalaryMedian:N0} {roleKpi?.SalaryCurrency}.",
            severity:  "Medium",
            ct);

        await TryFireAsync(userId, AlertType.AIRiskIncrease,
            condition: roleKpi?.AIRiskScore is not null && roleKpi.AIRiskScore > AiRiskHighThreshold,
            title:     $"High AI automation risk for {currentRole}",
            body:      $"AI risk score for {currentRole} is {roleKpi?.AIRiskScore:F1}/10. Upskilling towards AI-adjacent skills can reduce exposure.",
            severity:  "High",
            ct);

        await TryFireAsync(userId, AlertType.CompetitivenessDecline,
            condition: latestRec is not null && latestRec.CompetitiveScore < CompetitivenessLowThreshold,
            title:     "Your market competitiveness needs attention",
            body:      $"Your competitiveness score is {latestRec?.CompetitiveScore * 100:F0}%. Completing your skill gaps will help restore your position.",
            severity:  "High",
            ct);

        // Emerging skill: pick the highest-growth role skill the user is missing
        await CheckEmergingSkillAsync(userId, currentRole, userSkillCanonicals, ct);
    }

    private async Task CheckEmergingSkillAsync(
        int userId, string currentRole,
        HashSet<string> userSkills, CancellationToken ct)
    {
        if (!SkillPrioritizationService.RoleRequiredSkills.TryGetValue(currentRole, out var requiredSkills))
            return;

        // Find missing skills with known high growth
        string? emergingSkillName = null;
        double  bestGrowth        = EmergingSkillGrowthThreshold;

        foreach (var canonical in requiredSkills)
        {
            if (userSkills.Contains(canonical)) continue;

            try
            {
                var kpi = await _kpiRepository.GetLatestAsync(null, null, null, ct);
                // Per-skill KPI lookup: find by skillId — but since we don't have the skill ID
                // from the canonical name here, use the static SkillROI metadata as proxy.
                // A skill is "emerging" if the static GrowthMomentum in the metadata >= threshold.
                var skillKpi = await _kpiRepository.GetTopByGrowthMomentumAsync(null, 1, ct);
                if (skillKpi.Count == 0) break;
            }
            catch { /* non-critical */ }
        }

        // Use the static SkillPrioritizationService metadata as the authoritative source
        // for growth momentum (matches the ROI formula data).
        foreach (var canonical in requiredSkills)
        {
            if (userSkills.Contains(canonical)) continue;
            var growth = GetStaticGrowthMomentum(canonical);
            if (growth >= bestGrowth)
            {
                bestGrowth        = growth;
                emergingSkillName = ToDisplayName(canonical);
            }
        }

        if (emergingSkillName is null) return;

        await TryFireAsync(userId, AlertType.EmergingSkill,
            condition: true,
            title:     $"{emergingSkillName} is emerging in your field",
            body:      $"{emergingSkillName} is showing strong growth momentum ({bestGrowth:F1}/10) and is required for your target roles. Adding it now maximises your ROI.",
            severity:  "Low",
            ct);
    }

    private async Task TryFireAsync(
        int userId, AlertType type,
        bool condition, string title, string? body, string severity,
        CancellationToken ct)
    {
        if (!condition) return;

        try
        {
            var latest = await _alertRepository.GetLatestByUserAndTypeAsync(userId, type, ct);
            if (latest is not null && DateTime.UtcNow - latest.CreatedAt < DedupWindow)
                return;

            var alert = UserAlert.Create(userId, type, title, body, severity);
            await _alertRepository.AddAsync(alert, ct);

            _logger.LogInformation(
                "AlertingService: fired {AlertType} for user {UserId}", type, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "AlertingService: failed to fire {AlertType} for user {UserId}", type, userId);
        }
    }

    // Maps static SkillPrioritizationService GrowthMomentum values.
    // Kept here as a private lookup to avoid exposing internals across layers.
    private static readonly Dictionary<string, double> StaticGrowthMomentum =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["csharp"]             = 6.0, ["java"]              = 5.0,
            ["python"]             = 8.5, ["typescript"]        = 7.5,
            ["go"]                 = 8.0, ["rust"]              = 9.0,
            ["dotnet-aspnet-core"] = 6.5, ["spring-boot"]       = 5.5,
            ["fastapi"]            = 7.5, ["react"]             = 6.0,
            ["nodejs"]             = 6.0, ["aws"]               = 8.0,
            ["azure"]              = 7.5, ["gcp"]               = 8.5,
            ["kubernetes"]         = 8.5, ["docker"]            = 7.0,
            ["cicd"]               = 7.0, ["terraform"]         = 8.5,
            ["ansible"]            = 5.5, ["prometheus-grafana"]= 7.5,
            ["elk-stack"]          = 6.0, ["opentelemetry"]     = 9.0,
            ["microservices"]      = 7.0, ["event-driven"]      = 7.5,
            ["cqrs"]               = 6.5, ["ddd"]               = 6.0,
            ["rest-apis"]          = 4.0, ["grpc"]              = 7.5,
            ["postgresql"]         = 6.5, ["sql-server"]        = 4.5,
            ["mongodb"]            = 5.5, ["redis"]             = 6.5,
            ["elasticsearch"]      = 6.0, ["apache-kafka"]      = 8.0,
            ["machine-learning"]   = 9.5, ["llm-integration"]   = 9.8,
            ["mlops"]              = 9.0, ["tensorflow-pytorch"] = 8.5,
            ["team-leadership"]    = 6.0, ["system-design"]     = 7.5,
            ["code-review"]        = 5.5, ["mentoring"]         = 6.0,
        };

    private static double GetStaticGrowthMomentum(string canonical)
        => StaticGrowthMomentum.TryGetValue(canonical, out var v) ? v : 6.0;

    private static string ToDisplayName(string canonical)
    {
        var known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["csharp"]             = "C#",
            ["dotnet-aspnet-core"] = ".NET / ASP.NET Core",
            ["rest-apis"]          = "REST APIs",
            ["apache-kafka"]       = "Apache Kafka",
            ["elk-stack"]          = "ELK Stack",
            ["cicd"]               = "CI/CD",
            ["ddd"]                = "Domain-Driven Design",
            ["cqrs"]               = "CQRS",
            ["grpc"]               = "gRPC",
            ["llm-integration"]    = "LLM Integration",
            ["tensorflow-pytorch"] = "TensorFlow / PyTorch",
            ["prometheus-grafana"] = "Prometheus / Grafana",
        };
        if (known.TryGetValue(canonical, out var name)) return name;
        return string.Join(" ", canonical.Split('-')
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}
