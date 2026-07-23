namespace KadirliApp.Application.Common.Exceptions;

/// <summary>İstek/deneme limiti aşıldı — ExceptionMiddleware 429 TOO_MANY_REQUESTS'e çevirir.</summary>
public class RateLimitedException : AppException
{
    public RateLimitedException(string message) : base(message, "RATE_LIMITED")
    {
    }
}
