using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class MonitoringSnapshotRepository : IMonitoringSnapshotRepository
{
    private readonly AegisDbContext _context;

    public MonitoringSnapshotRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserMonitoringSnapshot>> GetByUserIdAsync(
        int userId, int limit, CancellationToken ct = default)
        => await _context.UserMonitoringSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotDate)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<UserMonitoringSnapshot?> GetLatestByUserIdAsync(
        int userId, CancellationToken ct = default)
        => await _context.UserMonitoringSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(UserMonitoringSnapshot snapshot, CancellationToken ct = default)
    {
        await _context.UserMonitoringSnapshots.AddAsync(snapshot, ct);
        await _context.SaveChangesAsync(ct);
    }
}
