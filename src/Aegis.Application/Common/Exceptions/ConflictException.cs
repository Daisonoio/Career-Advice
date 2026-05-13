namespace Aegis.Application.Common.Exceptions;

public class ConflictException : AegisException
{
    public ConflictException(string message) : base(message) { }
}
