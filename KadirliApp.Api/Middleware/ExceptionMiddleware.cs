using KadirliApp.Application.Common.Exceptions;

namespace KadirliApp.Api.Middleware;

/// <summary>
/// Flutter kontratı: hata yanıtı { success:false, error:{code,message}, meta } (Masterclass 11.2).
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
        => (_next, _log) = (next, log);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            var (status, code, message) = ex switch
            {
                NotFoundException => (404, "NOT_FOUND", ex.Message),
                ForbiddenException => (403, "FORBIDDEN", ex.Message),
                ConflictException => (409, "CONFLICT", ex.Message),
                UnauthorizedException => (401, "UNAUTHORIZED", ex.Message),
                RateLimitedException => (429, "RATE_LIMITED", ex.Message),
                FluentValidation.ValidationException => (400, "VALIDATION_ERROR", ex.Message),
                AppException app => (400, app.Code, app.Message),
                _ => (500, "INTERNAL_ERROR", "Bir hata oluştu")
            };

            // Faz 9.3: istemcinin bildirdiği traceId ile Serilog/Seq'teki hata kaydı eşleştirilebilir
            var traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.TraceIdentifier;

            if (status >= 500)
                _log.LogError(ex, "{Method} {Path} TraceId={TraceId}", ctx.Request.Method, ctx.Request.Path, traceId);

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = new { code, message },
                meta = new
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    path = ctx.Request.Path.Value,
                    traceId
                }
            });
        }
    }
}
