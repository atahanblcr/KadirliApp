using KadirliApp.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KadirliApp.Api.Filters;

/// <summary>
/// Flutter kontratı: her başarılı yanıt { success, data, meta } zarfıyla döner (Masterclass 11.1).
/// Handler zaten ApiResponse&lt;T&gt; dönüyorsa ikinci kez sarmaz.
/// </summary>
public class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext ctx, ResultExecutionDelegate next)
    {
        if (ctx.Result is ObjectResult obj
            && obj.Value is not ProblemDetails
            && !IsAlreadyWrapped(obj.Value))
        {
            obj.Value = new
            {
                success = true,
                data = obj.Value,
                meta = new
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    path = ctx.HttpContext.Request.Path.Value,
                    // Faz 9.3: destek taleplerinde log eşleştirme için — Flutter zarfına geriye uyumlu ek alan
                    traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier
                }
            };
            // ObjectResult tip bildirimi eski değer tipine kilitli kalmasın
            obj.DeclaredType = null;
        }

        await next();
    }

    private static bool IsAlreadyWrapped(object? value)
    {
        if (value is null) return false;
        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
    }
}
