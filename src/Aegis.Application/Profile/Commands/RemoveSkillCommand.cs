using Aegis.Application.Common.Exceptions;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using MediatR;

namespace Aegis.Application.Profile.Commands;

public record RemoveSkillCommand(int UserId, int SkillId) : IRequest;

public class RemoveSkillCommandHandler : IRequestHandler<RemoveSkillCommand>
{
    private readonly IUserRepository _userRepository;

    public RemoveSkillCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(RemoveSkillCommand request, CancellationToken cancellationToken)
    {
        var profile = await _userRepository.GetProfileByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.UserId);

        profile.RemoveSkill(request.SkillId);
        await _userRepository.AddOrUpdateProfileAsync(profile, cancellationToken);
    }
}
