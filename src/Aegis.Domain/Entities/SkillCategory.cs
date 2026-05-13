namespace Aegis.Domain.Entities;

public class SkillCategory : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private SkillCategory() { }

    public static SkillCategory Create(string name, string? description = null)
        => new() { Name = name, Description = description };
}
