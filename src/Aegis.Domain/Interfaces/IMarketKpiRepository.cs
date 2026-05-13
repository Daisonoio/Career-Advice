using Aegis.Domain.Entities;

namespace Aegis.Domain.Interfaces;

public interface IMarketKpiRepository
{
    Task<MarketKpi?> GetLatestAsync(int? skillId, string? roleCanonical, string? geoCountry, CancellationToken ct = default);
    Task<List<MarketKpi>> GetByRoleAsync(string roleCanonical, CancellationToken ct = default);
    Task<List<MarketKpi>> GetBySkillAsync(int skillId, CancellationToken ct = default);
    Task<List<MarketKpi>> GetTopByGrowthMomentumAsync(string? geoCountry, int topN, CancellationToken ct = default);
    Task UpsertAsync(MarketKpi kpi, CancellationToken ct = default);
}
