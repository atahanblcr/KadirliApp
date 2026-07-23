namespace KadirliApp.Application.Common.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Oturum geçersiz veya süresi dolmuş.")
        : base(message, "UNAUTHORIZED")
    {
    }
}
