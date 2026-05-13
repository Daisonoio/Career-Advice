using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class AssessmentRepository : IAssessmentRepository
{
    private readonly AegisDbContext _context;

    public AssessmentRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<Assessment?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Assessments
            .Include(a => a.Questions)
            .Include(a => a.Result)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Assessment?> GetActiveForUserAsync(int userId, CancellationToken ct = default)
        => await _context.Assessments
            .Include(a => a.Questions)
            .Where(a => a.UserId == userId && a.CompletedAt == null)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<List<Assessment>> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.Assessments
            .Include(a => a.Questions)
            .Include(a => a.Result)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Assessment assessment, CancellationToken ct = default)
    {
        await _context.Assessments.AddAsync(assessment, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Assessment assessment, CancellationToken ct = default)
    {
        _context.Assessments.Update(assessment);
        await _context.SaveChangesAsync(ct);
    }
}
