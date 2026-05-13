namespace Aegis.Application.Common.Exceptions;

public class NotFoundException : AegisException
{
    public NotFoundException(string name, object key)
        : base($"Entity '{name}' ({key}) was not found.") { }
}
