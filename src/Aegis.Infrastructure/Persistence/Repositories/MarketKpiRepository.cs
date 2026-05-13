using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class MarketKpiRepository : IMarketKpiRepository
{
    private readonly AegisDbContext _context;

    public MarketKpiRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<MarketKpi?> GetLatestAsync(int? skillId, string? roleCanonical, string? geoCountry, CancellationToken ct = default)
    {
        var query = _context.MarketKpis.AsQueryable();

        if (skillId.HasValue)
            query = query.Where(k => k.SkillId == skillId.Value);

        if (!string.IsNullOrEmpty(roleCanonical))
            query = query.Where(k => k.RoleCanonical == roleCanonical);

        if (!string.IsNullOrEmpty(geoCountry))
            query = query.Where(k => k.GeoCountry == geoCountry);

        return await query
            .OrderByDescending(k => k.UpdatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<MarketKpi>> GetByRoleAsync(string roleCanonical, CancellationToken ct = default)
        => await _context.MarketKpis
            .Where(k => k.RoleCanonical == roleCanonical)
            .OrderByDescending(k => k.UpdatedAt)
            .ToListAsync(ct);

    public async Task<List<MarketKpi>> GetBySkillAsync(int skillId, CancellationToken ct = default)
        => await _context.MarketKpis
            .Where(k => k.SkillId == skillId)
            .OrderByDescending(k => k.UpdatedAt)
            .ToListAsync(ct);

    public async Task<List<MarketKpi>> GetTopByGrowthMomentumAsync(string? geoCountry, int topN, CancellationToken ct = default)
    {
        var query = _context.MarketKpis
            .Where(k => k.GrowthMomentum.HasValue);

        if (!string.IsNullOrEmpty(geoCountry))
            query = query.Where(k => k.GeoCountry == geoCountry);

        return await query
            .OrderByDescending(k => k.GrowthMomentum)
            .Take(topN)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(MarketKpi kpi, CancellationToken ct = default)
    {
        var existing = await _context.MarketKpis
            .FirstOrDefaultAsync(k =>
                k.SkillId == kpi.SkillId &&
                k.RoleCanonical == kpi.RoleCanonical &&
                k.GeoCountry == kpi.GeoCountry,
                ct);

        if (existing is not null)
            _context.MarketKpis.Remove(existing);

        await _context.MarketKpis.AddAsync(kpi, ct);
        await _context.SaveChangesAsync(ct);
    }
}
