using Aegis.Domain.Entities;
using Aegis.Domain.Enums;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly AegisDbContext _context;

    public AlertRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAlert>> GetUnreadByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.UserAlerts
            .Where(a => a.UserId == userId && a.ReadAt == null)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<List<UserAlert>> GetByUserIdAsync(int userId, int pageSize, int page, CancellationToken ct = default)
        => await _context.UserAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> GetTotalCountByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.UserAlerts.CountAsync(a => a.UserId == userId, ct);

    public async Task<UserAlert?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.UserAlerts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(UserAlert alert, CancellationToken ct = default)
    {
        await _context.UserAlerts.AddAsync(alert, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<UserAlert?> GetLatestByUserAndTypeAsync(
        int userId, AlertType alertType, CancellationToken ct = default)
        => await _context.UserAlerts
            .Where(a => a.UserId == userId && a.AlertType == alertType)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task MarkAsReadAsync(int alertId, CancellationToken ct = default)
    {
        var alert = await _context.UserAlerts.FirstOrDefaultAsync(a => a.Id == alertId, ct);
        if (alert is not null)
        {
            alert.MarkAsRead();
            await _context.SaveChangesAsync(ct);
        }
    }
}
