using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KadirliApp.Infrastructure.Health;

/// <summary>
/// Faz 9.3: Liveness/Readiness uç noktaları (Docker HEALTHCHECK / Kubernetes probe'ları için).
/// Api ve Web aynı kontrolleri paylaştığından mapping tek yerde tutulur.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapInfrastructureHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // 🔑 Faz 12.20a — `.AllowAnonymous()` üçü için de ZORUNLU ve gerekçesi Web tarafında:
        // panel artık fail-closed (Program.cs → FallbackPolicy "kimlik doğrulanmış olsun").
        // Bu satır olmadan orkestratörün probe'ları 302 → /account/login alır; ne 200'dür
        // ne de hata sayılır, yani konteyner "sağlıksız" damgası yer ve **sebep hiçbir logda
        // görünmez**. Api tarafında fallback policy yok, orada bu çağrı etkisiz (no-op).

        // Liveness: process ayakta mı — bağımlılık kontrolü YAPMAZ (bağımlılık çöktü diye pod restart edilmesin)
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();

        // Readiness: trafik alabilir mi — "ready" etiketli kritik bağımlılıklar (Postgres, Redis, Hangfire)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = WriteJsonAsync
        }).AllowAnonymous();

        // Detaylı rapor: tüm kontroller + süreleri (izleme panoları için)
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteJsonAsync })
            .AllowAnonymous();

        return app;
    }

    private static Task WriteJsonAsync(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json; charset=utf-8";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        }));
    }
}
