using Aegis.Application.Skills.DTOs;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Skills.Queries;

public record GetSkillCategoriesQuery : IRequest<List<SkillCategoryDto>>;

public class GetSkillCategoriesQueryHandler : IRequestHandler<GetSkillCategoriesQuery, List<SkillCategoryDto>>
{
    private readonly ISkillRepository _skillRepository;

    public GetSkillCategoriesQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<List<SkillCategoryDto>> Handle(GetSkillCategoriesQuery request, CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetAllWithCategoriesAsync(cancellationToken);

        return skills
            .Where(s => s.Category != null)
            .GroupBy(s => s.Category!)
            .Select(g => new SkillCategoryDto(
                Id: g.Key.Id,
                Name: g.Key.Name,
                Description: g.Key.Description,
                SkillCount: g.Count()))
            .OrderBy(c => c.Name)
            .ToList();
    }
}
