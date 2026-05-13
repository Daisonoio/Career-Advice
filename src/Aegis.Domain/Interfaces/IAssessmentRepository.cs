using Aegis.Domain.Entities;

namespace Aegis.Domain.Interfaces;

public interface IAssessmentRepository
{
    Task<Assessment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Assessment?> GetActiveForUserAsync(int userId, CancellationToken ct = default);
    Task<List<Assessment>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task AddAsync(Assessment assessment, CancellationToken ct = default);
    Task UpdateAsync(Assessment assessment, CancellationToken ct = default);
}
