using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Aegis.Infrastructure.ExternalData.Esco;

public class EscoApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EscoApiClient> _logger;
    private readonly int _requestDelayMs;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EscoApiClient(HttpClient http, ILogger<EscoApiClient> logger, int requestDelayMs = 250)
    {
        _http           = http;
        _logger         = logger;
        _requestDelayMs = requestDelayMs;
    }

    // Search for occupations matching a free-text query.
    // Returns the top N results ordered by relevance score.
    public async Task<List<EscoSearchResult>> SearchOccupationsAsync(
        string query,
        int maxResults = 3,
        CancellationToken ct = default)
    {
        var url = $"search?text={Uri.EscapeDataString(query)}&language=en&type=occupation&selectedVersion=latest";

        try
        {
            await ThrottleAsync(ct);
            var response = await _http.GetFromJsonAsync<EscoSearchResponse>(url, JsonOptions, ct);
            return response?.Embedded?.Results
                ?.OrderByDescending(r => r.Score)
                .Take(maxResults)
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ESCO occupation search failed for query '{Query}'", query);
            return [];
        }
    }

    // Fetch all skills (essential + optional) for a given occupation URI.
    // For each skill, fetches both EN and IT localisations.
    public async Task<List<EscoSkillData>> GetSkillsForOccupationAsync(
        string occupationUri,
        bool includeOptional,
        CancellationToken ct = default)
    {
        var occupation = await FetchOccupationAsync(occupationUri, ct);
        if (occupation?.Links is null)
            return [];

        var skillLinks = new List<(EscoSkillLink Link, bool IsEssential)>();

        foreach (var link in occupation.Links.EssentialSkills ?? [])
            skillLinks.Add((link, true));

        if (includeOptional)
            foreach (var link in occupation.Links.OptionalSkills ?? [])
                skillLinks.Add((link, false));

        var result = new List<EscoSkillData>();

        foreach (var (link, isEssential) in skillLinks)
        {
            var skillData = await FetchSkillBilingualAsync(link.Uri, isEssential, ct);
            if (skillData is not null)
                result.Add(skillData);
        }

        return result;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<EscoOccupationResource?> FetchOccupationAsync(
        string uri,
        CancellationToken ct)
    {
        var url = $"resource/occupation?uri={Uri.EscapeDataString(uri)}&language=en";
        try
        {
            await ThrottleAsync(ct);
            return await _http.GetFromJsonAsync<EscoOccupationResource>(url, JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ESCO occupation fetch failed for URI '{Uri}'", uri);
            return null;
        }
    }

    private async Task<EscoSkillData?> FetchSkillBilingualAsync(
        string skillUri,
        bool isEssential,
        CancellationToken ct)
    {
        var skillEn = await FetchSkillAsync(skillUri, "en", ct);
        if (skillEn is null) return null;

        // Italian translation — best-effort, null if not available
        var skillIt = await FetchSkillAsync(skillUri, "it", ct);

        var broaderTitle = skillEn.Links?.BroaderConcepts?.FirstOrDefault()?.Title;
        var skillType    = skillEn.SkillType?.Split('/').LastOrDefault() ?? "skill";

        return new EscoSkillData(
            Uri:                 skillUri,
            TitleEn:             skillEn.Title,
            TitleIt:             skillIt?.Title,
            BroaderConceptTitle: broaderTitle,
            SkillType:           skillType,
            IsEssential:         isEssential);
    }

    private async Task<EscoSkillResource?> FetchSkillAsync(
        string uri,
        string language,
        CancellationToken ct)
    {
        var url = $"resource/skill?uri={Uri.EscapeDataString(uri)}&language={language}";
        try
        {
            await ThrottleAsync(ct);
            return await _http.GetFromJsonAsync<EscoSkillResource>(url, JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ESCO skill fetch failed for URI '{Uri}' lang={Lang}", uri, language);
            return null;
        }
    }

    private Task ThrottleAsync(CancellationToken ct)
        => _requestDelayMs > 0
            ? Task.Delay(_requestDelayMs, ct)
            : Task.CompletedTask;
}
