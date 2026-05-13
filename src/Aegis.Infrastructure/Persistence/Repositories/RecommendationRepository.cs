using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class RecommendationRepository : IRecommendationRepository
{
    private readonly AegisDbContext _context;

    public RecommendationRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<Recommendation?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Recommendations
            .Include(r => r.Paths)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Recommendation?> GetLatestByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.Recommendations
            .Include(r => r.Paths)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<List<Recommendation>> GetByUserIdAsync(int userId, int limit = 10, CancellationToken ct = default)
        => await _context.Recommendations
            .Include(r => r.Paths)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.GeneratedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task AddAsync(Recommendation recommendation, CancellationToken ct = default)
    {
        await _context.Recommendations.AddAsync(recommendation, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Recommendation recommendation, CancellationToken ct = default)
    {
        _context.Recommendations.Update(recommendation);
        await _context.SaveChangesAsync(ct);
    }
}
