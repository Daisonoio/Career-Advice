using System.Text.Json.Serialization;

namespace Aegis.Infrastructure.ExternalData.Adzuna;

public record AdzunaSearchResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("results")] List<AdzunaJobResult> Results);

public record AdzunaJobResult(
    [property: JsonPropertyName("salary_min")] decimal? SalaryMin,
    [property: JsonPropertyName("salary_max")] decimal? SalaryMax,
    [property: JsonPropertyName("salary_is_predicted")] bool SalaryIsPredicted,
    [property: JsonPropertyName("contract_type")] string? ContractType,
    [property: JsonPropertyName("contract_time")] string? ContractTime,
    [property: JsonPropertyName("location")] AdzunaLocation? Location,
    [property: JsonPropertyName("created")] string? Created);

public record AdzunaLocation(
    [property: JsonPropertyName("area")] List<string>? Area);
