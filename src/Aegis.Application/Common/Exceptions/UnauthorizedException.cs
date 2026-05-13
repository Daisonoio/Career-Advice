namespace Aegis.Application.Common.Exceptions;

public class UnauthorizedException : AegisException
{
    public UnauthorizedException(string message = "Unauthorized access.") : base(message) { }
}
