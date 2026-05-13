using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AegisDbContext _context;

    public RefreshTokenRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetActiveByTokenAsync(string token, CancellationToken ct = default)
        => await _context.RefreshTokens
            .FirstOrDefaultAsync(t =>
                t.Token == token &&
                t.RevokedAt == null &&
                t.ExpiresAt > DateTime.UtcNow,
                ct);

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(ct);
    }
}
