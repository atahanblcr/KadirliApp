using KadirliApp.Application.Features.ErrorLogs.Commands;
using KadirliApp.Application.Features.ErrorLogs.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KadirliApp.Api.Controllers;

/// <summary>
/// Faz 12.1 — mobil istemcinin hata/çökme bildirimi.
///
/// 🔑 <b>Neden anonim:</b> çökme çoğu zaman oturum açılmadan önce olur (açılış ekranı,
/// giriş akışı). <c>[Authorize]</c> konsaydı raporlanamayan hatalar tam da en kritik
/// olanlar olurdu. Bedeli spam yüzeyi; karşılığı <c>public-write</c> hız sınırı,
/// gövde tavanları ve <b>parmak izi tekilleştirmesi</b> (aynı yalan bin kez gönderilse
/// bile tabloda tek satır olur).
///
/// ⚠️ Bu uç <c>EndpointAuthorizationSweepTests</c>'in "bilinçli anonim yazma uçları"
/// listesinde <b>açıkça</b> yer alır — sessizce eklenmiş değil.
/// </summary>
[Route("v1/client-errors")]
public class ClientErrorsController : ApiControllerBase
{
    /// <summary>
    /// İstemci hatasını kaydeder. Yanıt her zaman <c>202</c>: istemci hata raporunun
    /// akıbetiyle ilgilenmemeli, bu bir "ateşle-unut" ucudur. Kuyruk doluysa olay düşer
    /// ama istemci yine 202 alır — aksi hâlde mobil, hata raporunu göndermek için
    /// yeniden denemeye başlar ve zaten sorunlu olan sistemi daha çok yorar.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Report([FromBody] ReportClientErrorDto dto)
    {
        // ⚠️ Source ve TraceId gövdeden ALINMAZ — sunucuda üretilir (bkz. komut belgesi).
        var traceId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var accepted = await Sender.Send(new ReportClientErrorCommand(
            dto,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
            traceId));

        return Accepted(new { accepted });
    }
}
