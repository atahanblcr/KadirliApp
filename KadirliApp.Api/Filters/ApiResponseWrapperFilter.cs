using KadirliApp.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KadirliApp.Api.Filters;

/// <summary>
/// Flutter kontratı: her başarılı yanıt { success, data, meta } zarfıyla döner (Masterclass 11.1).
/// Handler zaten ApiResponse&lt;T&gt; dönüyorsa ikinci kez sarmaz; ancak Faz 10.13'ten beri o zarfın
/// meta'sı boşsa DOLDURULUR — böylece TÜM public uçlarda meta.timestamp/path/traceId tutarlıdır
/// (eskiden yalnız filter'la sarılan yanıtların meta'sı vardı, kendi ApiResponse'unu dönen ~13 handler'ınki null'dı).
/// </summary>
public class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext ctx, ResultExecutionDelegate next)
    {
        if (ctx.Result is ObjectResult obj && obj.Value is not ProblemDetails)
        {
            if (IsAlreadyWrapped(obj.Value))
            {
                // Handler kendi ApiResponse<T>'sini döndü (eski desen): zarfı bozma, yalnız boş meta'yı doldur.
                var metaProp = obj.Value!.GetType().GetProperty(nameof(ApiResponse<object>.Meta));
                if (metaProp is not null && metaProp.GetValue(obj.Value) is null)
                    metaProp.SetValue(obj.Value, BuildMeta(ctx));
            }
            else
            {
                obj.Value = new
                {
                    success = true,
                    data = obj.Value,
                    meta = BuildMeta(ctx)
                };
                // ObjectResult tip bildirimi eski değer tipine kilitli kalmasın
                obj.DeclaredType = null;
            }
        }

        await next();
    }

    // Faz 9.3: traceId destek taleplerinde log eşleştirme için — Flutter zarfına geriye uyumlu ek alan.
    private static object BuildMeta(ResultExecutingContext ctx) => new
    {
        timestamp = DateTime.UtcNow.ToString("o"),
        path = ctx.HttpContext.Request.Path.Value,
        traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier
    };

    private static bool IsAlreadyWrapped(object? value)
    {
        if (value is null) return false;
        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
    }
}
