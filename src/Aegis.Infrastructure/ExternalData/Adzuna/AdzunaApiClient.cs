using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Aegis.Infrastructure.ExternalData.Adzuna;

public class AdzunaApiClient
{
    private readonly HttpClient _http;
    private readonly AdzunaSettings _settings;
    private readonly ILogger<AdzunaApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdzunaApiClient(HttpClient http, AdzunaSettings settings, ILogger<AdzunaApiClient> logger)
    {
        _http = http;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Searches Adzuna for full-time permanent jobs matching <paramref name="query"/>
    /// in the given <paramref name="countryCode"/>. Returns null when all retry attempts
    /// are exhausted or the API key is invalid.
    /// </summary>
    public async Task<AdzunaSearchResponse?> SearchJobsAsync(
        string query,
        string countryCode,
        CancellationToken ct = default)
    {
        var url = BuildUrl(query, countryCode);

        for (int attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            await ThrottleAsync(ct);

            HttpResponseMessage response;
            try
            {
                response = await _http.GetAsync(url, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex,
                    "Adzuna network error for '{Query}' in {Country} (attempt {Attempt}/{Max})",
                    query, countryCode, attempt, _settings.MaxRetryAttempts);

                if (attempt < _settings.MaxRetryAttempts)
                    await ExponentialBackoffAsync(attempt, ct);

                continue;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning(
                    "Adzuna rate-limited for '{Query}' in {Country} (attempt {Attempt}/{Max})",
                    query, countryCode, attempt, _settings.MaxRetryAttempts);

                if (attempt < _settings.MaxRetryAttempts)
                    await ExponentialBackoffAsync(attempt, ct);

                continue;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized
                || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError(
                    "Adzuna authentication failed ({Status}). Check AppId/AppKey configuration.",
                    response.StatusCode);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Adzuna returned {Status} for '{Query}' in {Country}",
                    (int)response.StatusCode, query, countryCode);
                return null;
            }

            try
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<AdzunaSearchResponse>(json, JsonOptions);

                _logger.LogDebug(
                    "Adzuna '{Query}' in {Country}: count={Count}, results={Results}",
                    query, countryCode, result?.Count, result?.Results.Count);

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialise Adzuna response for '{Query}' in {Country}", query, countryCode);
                return null;
            }
        }

        _logger.LogError(
            "Adzuna search failed after {Max} attempts for '{Query}' in {Country}",
            _settings.MaxRetryAttempts, query, countryCode);

        return null;
    }

    private string BuildUrl(string query, string countryCode)
    {
        var encoded = Uri.EscapeDataString(query);
        return $"jobs/{countryCode}/search/1" +
               $"?app_id={Uri.EscapeDataString(_settings.AppId)}" +
               $"&app_key={Uri.EscapeDataString(_settings.AppKey)}" +
               $"&what={encoded}" +
               $"&results_per_page={_settings.ResultsPerPage}" +
               $"&salary_include_unknown=0" +
               $"&full_time=1" +
               $"&permanent=1" +
               $"&content-type=application/json";
    }

    private Task ThrottleAsync(CancellationToken ct)
        => Task.Delay(_settings.RequestDelayMs, ct);

    private static Task ExponentialBackoffAsync(int attempt, CancellationToken ct)
        => Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
}
