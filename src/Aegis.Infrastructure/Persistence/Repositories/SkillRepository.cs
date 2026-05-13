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

    public async Task<List<Skill>> SearchByNameAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _context.Skills
            .Include(s => s.Category)
            .Where(s => EF.Functions.ILike(s.Name, $"%{lowerQuery}%") ||
                        EF.Functions.ILike(s.CanonicalName, $"%{lowerQuery}%"))
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
}
