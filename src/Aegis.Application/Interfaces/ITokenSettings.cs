namespace Aegis.Application.Interfaces;

public interface ITokenSettings
{
    int AccessTokenExpiryMinutes { get; }
    int RefreshTokenExpiryDays { get; }
}
