namespace Aegis.Application.Skills.DTOs;

public record SkillDto(int Id, string Name, string CanonicalName, string? Category, int? CategoryId);

public record SkillCategoryDto(int Id, string Name, string? Description, int SkillCount);
