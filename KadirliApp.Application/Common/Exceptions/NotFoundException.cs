namespace KadirliApp.Application.Common.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string name, object key) 
        : base($"Entity \"{name}\" ({key}) was not found.", "NOT_FOUND")
    {
    }
}
