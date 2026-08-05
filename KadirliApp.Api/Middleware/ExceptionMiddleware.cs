using System.Security.Claims;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Observability;

namespace KadirliApp.Api.Middleware;

/// <summary>
/// Flutter kontratı: hata yanıtı { success:false, error:{code,message}, meta } (Masterclass 11.2).
///
/// Faz 12.1: 5xx'ler artık Seq'e ek olarak <c>error_logs</c>'a da düşüyor — panelden
/// görülebilsinler diye. ⚠️ Yazma <b>kuyruğa</b> yapılır; buradan senkron DB'ye yazmak,
/// veritabanı çöktüğünde bu <c>catch</c> bloğunun içinde ikinci bir istisna doğurur ve
/// istemci <b>zarfsız</b> yanıt alır (görünmez sözleşme #10). Bkz. <see cref="IErrorLogSink"/>.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;
    private readonly IErrorLogSink _errorLogs;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log, IErrorLogSink errorLogs)
        => (_next, _log, _errorLogs) = (next, log, errorLogs);

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
            {
                _log.LogError(ex, "{Method} {Path} TraceId={TraceId}", ctx.Request.Method, ctx.Request.Path, traceId);

                // ⚠️ Yalnız 5xx. 4xx'ler (doğrulama, yetki, bulunamadı) kullanıcı kaynaklı ve
                // beklenen durumlar — tabloya alınsalardı gerçek hatalar gürültüde kaybolurdu.
                RecordError(ctx, ex, code, traceId);
            }

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

    private void RecordError(HttpContext ctx, Exception ex, string code, string traceId)
    {
        // 🔴 Bu metot ASLA fırlatmaz: catch bloğunun içindeyiz, buradaki bir istisna
        // yanıtın hiç yazılmamasına yol açar (istemci zarfsız ham 500 alır).
        try
        {
            _errorLogs.TryWrite(new ErrorLogEntry(
                Source: ErrorLogSources.Api,
                Level: ErrorLogLevels.Error,
                Code: code,
                // Kullanıcıya "Bir hata oluştu" diyoruz; panele gerçeğini yazıyoruz.
                Message: $"{ex.GetType().Name}: {ex.Message}",
                StackTrace: ex.ToString(),
                // ⚠️ Sorgu dizesi maskelenir — OTP akışında telefon oraya düşebiliyor.
                Path: SensitiveDataMasker.MaskPath(ctx.Request.Path + ctx.Request.QueryString),
                Method: ctx.Request.Method,
                StatusCode: 500,
                TraceId: traceId,
                UserId: Guid.TryParse(ctx.User.FindFirstValue("user_id"), out var userId) ? userId : null,
                IpAddress: ctx.Connection.RemoteIpAddress?.ToString(),
                UserAgent: ctx.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null));
        }
        catch (Exception sinkEx)
        {
            _log.LogError(sinkEx, "Hata kaydı kuyruğa alınamadı (yutuldu).");
        }
    }
}
