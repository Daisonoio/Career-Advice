using Aegis.Domain.Entities;

namespace Aegis.Domain.Interfaces;

public interface IJobApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<JobApplication>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task AddAsync(JobApplication jobApplication, CancellationToken ct = default);
    Task UpdateAsync(JobApplication jobApplication, CancellationToken ct = default);
}
