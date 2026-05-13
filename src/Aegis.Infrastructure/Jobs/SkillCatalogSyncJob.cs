using Aegis.Domain.Interfaces;
using Aegis.Infrastructure.ExternalData.Esco;
using Microsoft.Extensions.Logging;

namespace Aegis.Infrastructure.Jobs;

// Hangfire recurring job — runs daily at 02:00 UTC.
// Queries ESCO for every configured tech role, collects all associated skills
// (essential + optional), and upserts them into the local skill catalogue.
// The job is fully idempotent: re-running it has no side effects.
public class SkillCatalogSyncJob
{
    private readonly EscoApiClient _escoClient;
    private readonly EscoSkillMapper _mapper;
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<SkillCatalogSyncJob> _logger;

    // Tech roles queried against ESCO search.
    // ESCO returns the closest matching occupation; we take the top result.
    private static readonly string[] TargetRoleQueries =
    [
        "software developer",
        "software engineer",
        "back-end developer",
        "front-end developer",
        "full-stack developer",
        "web developer",
        "mobile application developer",
        "software architect",
        "systems analyst",
        "systems administrator",
        "database administrator",
        "data engineer",
        "data scientist",
        "machine learning engineer",
        "computer network engineer",
        "ICT security engineer",
        "DevOps engineer",
        "cloud engineer",
        "embedded systems engineer",
        "technical team leader",
    ];

    public SkillCatalogSyncJob(
        EscoApiClient escoClient,
        EscoSkillMapper mapper,
        ISkillRepository skillRepository,
        ILogger<SkillCatalogSyncJob> logger)
    {
        _escoClient      = escoClient;
        _mapper          = mapper;
        _skillRepository = skillRepository;
        _logger          = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogInformation("SkillCatalogSyncJob started at {Time}", startedAt);

        var before  = await _skillRepository.CountAsync(ct);
        var added   = 0;
        var updated = 0;
        var skipped = 0;
        var errors  = 0;

        // Track processed ESCO URIs to avoid syncing the same skill twice
        // when it appears under multiple occupations.
        var processedUris = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleQuery in TargetRoleQueries)
        {
            if (ct.IsCancellationRequested) break;

            _logger.LogDebug("Searching ESCO for occupation: '{Role}'", roleQuery);

            List<EscoSearchResult> occupations;
            try
            {
                occupations = await _escoClient.SearchOccupationsAsync(roleQuery, maxResults: 1, ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Occupation search failed for '{Role}' — skipping", roleQuery);
                errors++;
                continue;
            }

            if (occupations.Count == 0)
            {
                _logger.LogDebug("No ESCO occupation found for '{Role}'", roleQuery);
                continue;
            }

            var occupation = occupations[0];
            _logger.LogDebug("Matched occupation '{Title}' for query '{Role}'", occupation.Title, roleQuery);

            List<EscoSkillData> skills;
            try
            {
                skills = await _escoClient.GetSkillsForOccupationAsync(
                    occupation.Uri,
                    includeOptional: true,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to fetch skills for occupation '{Title}' — skipping",
                    occupation.Title);
                errors++;
                continue;
            }

            _logger.LogDebug(
                "Occupation '{Title}': {Count} skills to process",
                occupation.Title, skills.Count);

            foreach (var skillData in skills)
            {
                if (ct.IsCancellationRequested) break;
                if (!processedUris.Add(skillData.Uri))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var existsBefore = await _skillRepository.GetByExternalIdAsync(skillData.Uri, ct)
                                   ?? await _skillRepository.GetByCanonicalNameAsync(
                                          EscoSkillMapper.Canonicalize(skillData.TitleEn), ct);

                    var skill = _mapper.Map(skillData);
                    await _skillRepository.SyncAsync(skill, ct);

                    if (existsBefore is null) added++;
                    else                      updated++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to sync skill '{Title}' (URI: {Uri})",
                        skillData.TitleEn, skillData.Uri);
                    errors++;
                }
            }
        }

        var after   = await _skillRepository.CountAsync(ct);
        var elapsed = DateTime.UtcNow - startedAt;

        _logger.LogInformation(
            "SkillCatalogSyncJob completed in {Elapsed:g}. " +
            "Catalogue: {Before} → {After} skills. " +
            "Added={Added}, Updated={Updated}, Deduped={Skipped}, Errors={Errors}",
            elapsed, before, after, added, updated, skipped, errors);
    }
}
