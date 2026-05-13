namespace Aegis.Domain.Entities;

public class Skill : Entity
{
    public string Name { get; private set; } = default!;
    public string? NameIt { get; private set; }
    public string CanonicalName { get; private set; } = default!;
    public int? CategoryId { get; private set; }
    public int? ParentId { get; private set; }
    public List<string> Aliases { get; private set; } = [];

    // pgvector embedding (1536 dims for text-embedding-3-small)
    public float[]? EmbeddingVector { get; private set; }

    // Sync metadata
    public bool IsSystemSkill { get; private set; }
    public string? SourceType { get; private set; }
    public string? SourceExternalId { get; private set; }
    public double ConfidenceScore { get; private set; } = 1.0;
    public DateTime? LastSyncedAt { get; private set; }

    public SkillCategory? Category { get; private set; }

    private Skill() { }

    public static Skill Create(string name, string canonicalName, int? categoryId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
        return new Skill
        {
            Name          = name,
            CanonicalName = canonicalName,
            CategoryId    = categoryId,
            IsSystemSkill = false,
            SourceType    = "Manual",
            ConfidenceScore = 1.0
        };
    }

    public static Skill CreateFromSync(
        string name,
        string? nameIt,
        string canonicalName,
        int? categoryId,
        List<string> aliases,
        string sourceType,
        string sourceExternalId,
        double confidenceScore)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
        return new Skill
        {
            Name             = name,
            NameIt           = nameIt,
            CanonicalName    = canonicalName,
            CategoryId       = categoryId,
            Aliases          = aliases,
            IsSystemSkill    = false,
            SourceType       = sourceType,
            SourceExternalId = sourceExternalId,
            ConfidenceScore  = confidenceScore,
            LastSyncedAt     = DateTime.UtcNow
        };
    }

    // Called on existing skill during a sync run.
    // IsSystemSkill skills: only name/translation/aliases are updated.
    // Non-system skills: full update including canonical name and category.
    public void Sync(
        string name,
        string? nameIt,
        string canonicalName,
        int? categoryId,
        List<string> aliases,
        string sourceType,
        string sourceExternalId,
        double confidenceScore)
    {
        Name             = name;
        NameIt           = nameIt;
        Aliases          = aliases;
        SourceType       = sourceType;
        SourceExternalId = sourceExternalId;
        ConfidenceScore  = confidenceScore;
        LastSyncedAt     = DateTime.UtcNow;

        if (!IsSystemSkill)
        {
            CanonicalName = canonicalName;
            CategoryId    = categoryId;
        }

        Touch();
    }

    public void MarkAsSystemSkill() => IsSystemSkill = true;

    public void SetEmbedding(float[] vector) => EmbeddingVector = vector;

    public void AddAlias(string alias)
    {
        if (!Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
            Aliases.Add(alias);
    }
}
