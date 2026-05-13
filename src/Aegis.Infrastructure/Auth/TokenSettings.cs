using Aegis.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Aegis.Infrastructure.Auth;

public class TokenSettings : ITokenSettings
{
    public int AccessTokenExpiryMinutes { get; }
    public int RefreshTokenExpiryDays { get; }

    public TokenSettings(IConfiguration config)
    {
        AccessTokenExpiryMinutes = config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60);
        RefreshTokenExpiryDays = config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
    }
}
