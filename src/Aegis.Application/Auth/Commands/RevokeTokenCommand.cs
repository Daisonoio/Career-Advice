using Aegis.Application.Common.Exceptions;
using Aegis.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Aegis.Application.Auth.Commands;

public record RevokeTokenCommand(string RefreshToken) : IRequest;

public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepository.GetActiveByTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.RefreshToken), request.RefreshToken[..8] + "...");

        token.Revoke();
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
    }
}
