using Aegis.Domain.Entities;

namespace Aegis.Domain.Interfaces;

public interface ISkillRepository
{
    Task<Skill?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Skill?> GetByCanonicalNameAsync(string canonicalName, CancellationToken ct = default);
    Task<List<Skill>> SearchByNameAsync(string query, int limit = 20, CancellationToken ct = default);
    Task<List<Skill>> GetAllWithCategoriesAsync(CancellationToken ct = default);
    Task<List<Skill>> GetByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task UpsertAsync(Skill skill, CancellationToken ct = default);
}
