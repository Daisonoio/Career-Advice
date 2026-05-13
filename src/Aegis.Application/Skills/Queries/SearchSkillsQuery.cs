using Aegis.Application.Skills.DTOs;
using Aegis.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Aegis.Application.Skills.Queries;

public record SearchSkillsQuery(string Query, int Limit = 20) : IRequest<List<SkillDto>>;

public class SearchSkillsQueryValidator : AbstractValidator<SearchSkillsQuery>
{
    public SearchSkillsQueryValidator()
    {
        RuleFor(x => x.Query).NotEmpty().WithMessage("Query is required.");
        RuleFor(x => x.Limit).InclusiveBetween(1, 100).WithMessage("Limit must be between 1 and 100.");
    }
}

public class SearchSkillsQueryHandler : IRequestHandler<SearchSkillsQuery, List<SkillDto>>
{
    private readonly ISkillRepository _skillRepository;

    public SearchSkillsQueryHandler(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<List<SkillDto>> Handle(SearchSkillsQuery request, CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.SearchByNameAsync(request.Query, request.Limit, cancellationToken);
        return skills.Select(s => new SkillDto(
            Id: s.Id,
            Name: s.Name,
            CanonicalName: s.CanonicalName,
            Category: s.Category?.Name,
            CategoryId: s.CategoryId)).ToList();
    }
}
