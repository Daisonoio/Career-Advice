using Aegis.Domain.Entities;

namespace Aegis.Domain.Interfaces;

public interface IMonitoringSnapshotRepository
{
    Task<List<UserMonitoringSnapshot>> GetByUserIdAsync(int userId, int limit, CancellationToken ct = default);
    Task<UserMonitoringSnapshot?> GetLatestByUserIdAsync(int userId, CancellationToken ct = default);
    Task AddAsync(UserMonitoringSnapshot snapshot, CancellationToken ct = default);
}
