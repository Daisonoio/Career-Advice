using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AegisDbContext _context;

    public UserRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<UserProfile?> GetProfileByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.UserProfiles
            .Include(p => p.Skills)
                .ThenInclude(s => s.Skill)
                    .ThenInclude(s => s!.Category)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task AddOrUpdateProfileAsync(UserProfile profile, CancellationToken ct = default)
    {
        var existing = await _context.UserProfiles
            .AnyAsync(p => p.Id == profile.Id, ct);

        if (existing)
            _context.UserProfiles.Update(profile);
        else
            await _context.UserProfiles.AddAsync(profile, ct);

        await _context.SaveChangesAsync(ct);
    }
}
