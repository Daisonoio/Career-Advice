using Aegis.Domain.Entities;

namespace Aegis.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<UserProfile?> GetProfileByUserIdAsync(int userId, CancellationToken ct = default);
    Task AddOrUpdateProfileAsync(UserProfile profile, CancellationToken ct = default);
    Task<List<int>> GetAllUserIdsWithProfileAsync(CancellationToken ct = default);
}
