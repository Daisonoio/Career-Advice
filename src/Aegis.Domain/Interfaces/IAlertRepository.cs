using Aegis.Domain.Entities;
using Aegis.Domain.Enums;

namespace Aegis.Domain.Interfaces;

public interface IAlertRepository
{
    Task<List<UserAlert>> GetUnreadByUserIdAsync(int userId, CancellationToken ct = default);
    Task<List<UserAlert>> GetByUserIdAsync(int userId, int pageSize, int page, CancellationToken ct = default);
    Task<int> GetTotalCountByUserIdAsync(int userId, CancellationToken ct = default);
    Task<UserAlert?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UserAlert?> GetLatestByUserAndTypeAsync(int userId, AlertType alertType, CancellationToken ct = default);
    Task AddAsync(UserAlert alert, CancellationToken ct = default);
    Task MarkAsReadAsync(int alertId, CancellationToken ct = default);
}
