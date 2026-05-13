using Aegis.Application.Auth.DTOs;
using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Aegis.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenSettings _tokenSettings;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        ITokenSettings tokenSettings)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _tokenSettings = tokenSettings;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenString = _jwtTokenService.GenerateRefreshToken();

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenString,
            DateTime.UtcNow.AddDays(_tokenSettings.RefreshTokenExpiryDays));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_tokenSettings.AccessTokenExpiryMinutes),
            Email: user.Email,
            SubscriptionTier: user.SubscriptionTier.ToString());
    }
}
