using Aegis.Application.Auth.DTOs;
using Aegis.Application.Common.Exceptions;
using Aegis.Application.Interfaces;
using Aegis.Domain.Entities;
using Aegis.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Aegis.Application.Auth.Commands;

public record RegisterCommand(string Email, string Password, string ConfirmPassword) : IRequest<AuthResponse>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenSettings _tokenSettings;

    public RegisterCommandHandler(
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

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"User with email '{request.Email}' already exists.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash);
        await _userRepository.AddAsync(user, cancellationToken);

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
