using System.Security.Claims;
using Aegis.Domain.Entities;

namespace Aegis.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    int GetUserIdFromToken(string token);
}
