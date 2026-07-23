namespace KadirliApp.Application.Common.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz yok.")
        : base(message, "FORBIDDEN")
    {
    }
}
