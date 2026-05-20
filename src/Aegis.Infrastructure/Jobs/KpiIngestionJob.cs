using Aegis.Application.Market.Services;
using Aegis.Application.Recommendations.Services;
using Aegis.Domain.Interfaces;
using Aegis.Infrastructure.ExternalData.Adzuna;
using Microsoft.Extensions.Logging;

namespace Aegis.Infrastructure.Jobs;

/// <summary>
/// Weekly Hangfire job that ingests live market data from the Adzuna Jobs API
/// and populates the <c>market_kpis</c> table via <see cref="KpiComputationService"/>.
///
/// For each of the 10 canonical roles defined in <see cref="SkillPrioritizationService"/>:
///   1. Queries Adzuna for each configured country (default: IT + GB).
///   2. Aggregates job counts and salary data points across countries.
///   3. Computes YoY job-count growth against the previously stored KPI.
///   4. Delegates all score computation to <see cref="KpiComputationService"/> (deterministic, no LLM).
///   5. Upserts the resulting <see cref="Aegis.Domain.Entities.MarketKpi"/> record.
/// </summary>
public class KpiIngestionJob
{
    private readonly AdzunaApiClient _adzuna;
    private readonly AdzunaSettings _settings;
    private readonly KpiComputationService _computation;
    private readonly IMarketKpiRepository _kpiRepository;
    private readonly ILogger<KpiIngestionJob> _logger;

    public KpiIngestionJob(
        AdzunaApiClient adzuna,
        AdzunaSettings settings,
        KpiComputationService computation,
        IMarketKpiRepository kpiRepository,
        ILogger<KpiIngestionJob> logger)
    {
        _adzuna = adzuna;
        _settings = settings;
        _computation = computation;
        _kpiRepository = kpiRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("KpiIngestionJob started at {Time:O}", DateTimeOffset.UtcNow);

        int success = 0, skipped = 0, failed = 0;

        foreach (var role in SkillPrioritizationService.RoleRequiredSkills.Keys)
        {
            ct.ThrowIfCancellationRequested();

            if (!RoleIngestionProfiles.Profiles.TryGetValue(role, out var profile))
            {
                _logger.LogWarning("No ingestion profile for role '{Role}', skipping", role);
                skipped++;
                continue;
            }

            try
            {
                await IngestRoleAsync(role, profile, ct);
                success++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KPI ingestion failed for role '{Role}'", role);
                failed++;
            }
        }

        _logger.LogInformation(
            "KpiIngestionJob completed. Success={S}, Skipped={Sk}, Failed={F}",
            success, skipped, failed);
    }

    private async Task IngestRoleAsync(string role, RoleProfile profile, CancellationToken ct)
    {
        var salaryDataPoints = new List<decimal>();
        int totalJobCount = 0;
        int successfulCountries = 0;

        foreach (var country in _settings.Countries)
        {
            ct.ThrowIfCancellationRequested();

            var response = await _adzuna.SearchJobsAsync(profile.AdzunaQuery, country, ct);
            if (response is null)
            {
                _logger.LogWarning("Adzuna returned no data for role '{Role}' in {Country}", role, country);
                continue;
            }

            successfulCountries++;
            totalJobCount += response.Count;

            // Collect only confirmed (non-predicted) salary midpoints.
            // Adzuna salary_is_predicted=true means the salary was inferred by their model,
            // not stated in the job ad — we exclude these to keep data quality high.
            foreach (var job in response.Results)
            {
                if (job.SalaryMin is > 0 && job.SalaryMax is > 0 && !job.SalaryIsPredicted)
                    salaryDataPoints.Add((job.SalaryMin.Value + job.SalaryMax.Value) / 2m);
            }
        }

        if (successfulCountries == 0)
        {
            _logger.LogWarning(
                "All Adzuna requests failed for role '{Role}'. Skipping KPI update to preserve existing data.",
                role);
            return;
        }

        // Average job count across successfully queried countries for a normalised EU estimate.
        int jobCount30d = totalJobCount / successfulCountries;

        // YoY growth: compare against the previous stored KPI for this role.
        // On first run (no previous record) growth is 0 — neutral momentum.
        var previousKpi = await _kpiRepository.GetLatestAsync(null, role, null, ct);
        double yoyGrowthPct = 0;
        if (previousKpi?.JobCount30d is > 0)
        {
            yoyGrowthPct = (jobCount30d - previousKpi.JobCount30d.Value)
                           / (double)previousKpi.JobCount30d.Value * 100.0;
        }

        // Hiring velocity: ratio of current job count to twice the baseline (scales 0–1).
        // Values above baseline → high velocity; below → low velocity.
        double hiringVelocity = Math.Clamp(
            jobCount30d / (double)(profile.JobCountBaseline * 2), 0.0, 1.0);

        var rawData = new RawMarketData(
            JobCount30d:           jobCount30d,
            JobCountBaseline:      profile.JobCountBaseline,
            JobCountYoyGrowthPct:  yoyGrowthPct,
            HiringVelocity:        hiringVelocity,
            SalaryDataPoints:      salaryDataPoints,
            SalaryCurrency:        "EUR",
            RemotePremiumPct:      profile.RemotePremiumPct,
            AutomationProbability: profile.AutomationProbability,
            CommoditizationIndex:  profile.CommoditizationIndex,
            DataAgeHours:          0);

        var kpi = await _computation.ComputeForRoleAsync(role, null, rawData, ct);

        _logger.LogInformation(
            "Ingested '{Role}': jobs={Jobs} ({Countries} countries), " +
            "salaryPts={Pts}, demand={Demand:F1}, aiRisk={AiRisk:F1}, " +
            "growth={Growth:F1}, yoy={Yoy:+0.1;-0.1;0}%",
            role, jobCount30d, successfulCountries,
            salaryDataPoints.Count,
            kpi.DemandScore, kpi.AIRiskScore,
            kpi.GrowthMomentum, yoyGrowthPct);
    }
}
