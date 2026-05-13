using Aegis.Application.Auth.DTOs;
using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Aegis.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenSettings _tokenSettings;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ITokenSettings tokenSettings)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _tokenSettings = tokenSettings;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _refreshTokenRepository.GetActiveByTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        if (!existingToken.IsActive)
            throw new UnauthorizedException("Refresh token is no longer active.");

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), existingToken.UserId);

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = RefreshToken.Create(
            user.Id,
            newRefreshTokenString,
            DateTime.UtcNow.AddDays(_tokenSettings.RefreshTokenExpiryDays));

        existingToken.Revoke(newRefreshTokenString);
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return new AuthResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenExpiryMinutes),
            Email: user.Email,
            SubscriptionTier: user.SubscriptionTier.ToString());
    }
}
