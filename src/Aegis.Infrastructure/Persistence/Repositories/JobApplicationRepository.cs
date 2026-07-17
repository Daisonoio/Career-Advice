using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence.Repositories;

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly AegisDbContext _context;

    public JobApplicationRepository(AegisDbContext context)
    {
        _context = context;
    }

    public async Task<JobApplication?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.JobApplications.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<List<JobApplication>> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _context.JobApplications
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(JobApplication jobApplication, CancellationToken ct = default)
    {
        await _context.JobApplications.AddAsync(jobApplication, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(JobApplication jobApplication, CancellationToken ct = default)
    {
        _context.JobApplications.Update(jobApplication);
        await _context.SaveChangesAsync(ct);
    }
}
