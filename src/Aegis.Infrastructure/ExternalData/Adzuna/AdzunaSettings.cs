namespace Aegis.Infrastructure.ExternalData.Adzuna;

public class AdzunaSettings
{
    public string AppId { get; init; } = string.Empty;
    public string AppKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.adzuna.com/v1/api/";

    /// <summary>ISO 3166-1 alpha-2 country codes to query. Adzuna supports: gb, us, it, de, fr, nl, at, be, br, ca.</summary>
    public string[] Countries { get; init; } = ["it", "gb"];

    /// <summary>Max job results per API call (Adzuna cap: 50).</summary>
    public int ResultsPerPage { get; init; } = 50;

    /// <summary>Milliseconds to wait between consecutive Adzuna requests to respect rate limits.</summary>
    public int RequestDelayMs { get; init; } = 1200;

    /// <summary>Max retry attempts on transient failures (429, 5xx, network errors).</summary>
    public int MaxRetryAttempts { get; init; } = 3;
}
