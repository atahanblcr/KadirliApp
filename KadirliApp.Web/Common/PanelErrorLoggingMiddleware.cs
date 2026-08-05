using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Observability;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 12.1 — panelde oluşan işlenmemiş hataları <c>error_logs</c>'a yazar.
///
/// API'nin <c>ExceptionMiddleware</c>'inin panel karşılığı, ama bir farkla: burada
/// <b>yanıt üretilmez</b>. Panelin kendi hata sayfası zaten var
/// (<c>UseExceptionHandler("/Home/Error")</c> + <c>UseStatusCodePagesWithReExecute</c>);
/// bu katman yalnız <b>kaydeder ve istisnayı yeniden fırlatır</b>. Yanıtı da üretseydi
/// Development'taki ayrıntılı hata sayfası kaybolur, geliştirme zorlaşırdı.
///
/// 🔴 Kayıt <b>kuyruğa</b> yapılır ve kayıt hatası yutulur — istisna zaten yolda,
/// buradaki ikinci bir hata onu maskeler ve asıl sorun görünmez olurdu.
/// </summary>
public sealed class PanelErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PanelErrorLoggingMiddleware> _log;
    private readonly IErrorLogSink _errorLogs;

    public PanelErrorLoggingMiddleware(
        RequestDelegate next,
        ILogger<PanelErrorLoggingMiddleware> log,
        IErrorLogSink errorLogs)
        => (_next, _log, _errorLogs) = (next, log, errorLogs);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            Record(ctx, ex);
            throw; // Yanıtı panelin kendi hata sayfası üretir.
        }
    }

    private void Record(HttpContext ctx, Exception ex)
    {
        try
        {
            var traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.TraceIdentifier;

            _errorLogs.TryWrite(new ErrorLogEntry(
                Source: ErrorLogSources.Web,
                Level: ErrorLogLevels.Error,
                Code: ex.GetType().Name,
                Message: $"{ex.GetType().Name}: {ex.Message}",
                StackTrace: ex.ToString(),
                Path: SensitiveDataMasker.MaskPath(ctx.Request.Path + ctx.Request.QueryString),
                Method: ctx.Request.Method,
                StatusCode: 500,
                TraceId: traceId,
                // Panelde kimlik NameIdentifier'da (API'de user_id claim'i) — iki host,
                // iki farklı claim adı; bu fark 11.15b'de bir kez daha ısırmıştı.
                UserId: Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null,
                IpAddress: ctx.Connection.RemoteIpAddress?.ToString(),
                UserAgent: ctx.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null));
        }
        catch (Exception sinkEx)
        {
            _log.LogError(sinkEx, "Panel hata kaydı kuyruğa alınamadı (yutuldu).");
        }
    }
}
