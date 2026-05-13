using Aegis.Domain.Entities;

namespace Aegis.Domain.Interfaces;

public interface IRecommendationRepository
{
    Task<Recommendation?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Recommendation?> GetLatestByUserIdAsync(int userId, CancellationToken ct = default);
    Task<List<Recommendation>> GetByUserIdAsync(int userId, int limit = 10, CancellationToken ct = default);
    Task AddAsync(Recommendation recommendation, CancellationToken ct = default);
    Task UpdateAsync(Recommendation recommendation, CancellationToken ct = default);
}
