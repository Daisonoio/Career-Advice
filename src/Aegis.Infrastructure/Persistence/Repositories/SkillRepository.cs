using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly AegisDbContext _context;

    public SkillRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<Skill?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Skills
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Skill?> GetByCanonicalNameAsync(string canonicalName, CancellationToken ct = default)
        => await _context.Skills
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.CanonicalName == canonicalName, ct);

    public async Task<Skill?> GetByExternalIdAsync(string sourceExternalId, CancellationToken ct = default)
        => await _context.Skills
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.SourceExternalId == sourceExternalId, ct);

    public async Task<List<Skill>> SearchByNameAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _context.Skills
            .Include(s => s.Category)
            .Where(s => EF.Functions.ILike(s.Name, $"%{lowerQuery}%")
                     || EF.Functions.ILike(s.CanonicalName, $"%{lowerQuery}%")
                     || (s.NameIt != null && EF.Functions.ILike(s.NameIt, $"%{lowerQuery}%")))
            .OrderBy(s => s.Name)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<Skill>> GetAllWithCategoriesAsync(CancellationToken ct = default)
        => await _context.Skills
            .Include(s => s.Category)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<List<Skill>> GetByCategoryAsync(int categoryId, CancellationToken ct = default)
        => await _context.Skills
            .Include(s => s.Category)
            .Where(s => s.CategoryId == categoryId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task UpsertAsync(Skill skill, CancellationToken ct = default)
    {
        var existing = await _context.Skills
            .FirstOrDefaultAsync(s => s.CanonicalName == skill.CanonicalName, ct);

        if (existing is not null)
            _context.Skills.Remove(existing);

        await _context.Skills.AddAsync(skill, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SyncAsync(Skill incoming, CancellationToken ct = default)
    {
        // Lookup order: external ID first (most precise), then canonical name
        var existing = incoming.SourceExternalId is not null
            ? await _context.Skills.FirstOrDefaultAsync(
                s => s.SourceExternalId == incoming.SourceExternalId, ct)
            : null;

        existing ??= await _context.Skills.FirstOrDefaultAsync(
            s => s.CanonicalName == incoming.CanonicalName, ct);

        if (existing is not null)
        {
            existing.Sync(
                incoming.Name,
                incoming.NameIt,
                incoming.CanonicalName,
                incoming.CategoryId,
                incoming.Aliases,
                incoming.SourceType!,
                incoming.SourceExternalId!,
                incoming.ConfidenceScore);

            _context.Skills.Update(existing);
        }
        else
        {
            await _context.Skills.AddAsync(incoming, ct);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Skills.CountAsync(ct);
}
