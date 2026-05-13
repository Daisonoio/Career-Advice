namespace Aegis.Domain.Entities;

public class Skill : Entity
{
    public string Name { get; private set; } = default!;
    public string CanonicalName { get; private set; } = default!;
    public int? CategoryId { get; private set; }
    public int? ParentId { get; private set; }
    public List<string> Aliases { get; private set; } = [];

    // pgvector embedding (1536 dims for text-embedding-3-small)
    public float[]? EmbeddingVector { get; private set; }

    public SkillCategory? Category { get; private set; }

    private Skill() { }

    public static Skill Create(string name, string canonicalName, int? categoryId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
        return new Skill { Name = name, CanonicalName = canonicalName, CategoryId = categoryId };
    }

    public void SetEmbedding(float[] vector) => EmbeddingVector = vector;
    public void AddAlias(string alias) { if (!Aliases.Contains(alias)) Aliases.Add(alias); }
}
