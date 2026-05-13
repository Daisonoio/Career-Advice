namespace Aegis.Application.Common.Exceptions;

public class AegisException : Exception
{
    public AegisException(string message) : base(message) { }
    public AegisException(string message, Exception innerException) : base(message, innerException) { }
}
