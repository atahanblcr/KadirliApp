using KadirliApp.Application.Features.Legal.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

/// <summary>
/// Faz 12.16 — hukuki metinler (<c>/v1/legal/*</c>).
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>ANONİM OLMAK ZORUNDA.</b> Bu ucu çağıran henüz kayıtlı <b>değildir</b>: rızayı
/// vermeden önce metni okuması gerekiyor. <c>EndpointAuthorizationSweepTests</c>'in anonim
/// listesine <b>bilinçli</b> eklendi — o test kaçak anonim uçları yakalamak için var ve
/// listeye sessizce eklenen her ad, korumayı bir parça azaltır.
/// </para>
/// <para>
/// ⚠️ Uç <b>yalnız okur</b>: yazma yolu yok, dolayısıyla public-write hız sınırına
/// (§7 checklist) gerek yok.
/// </para>
/// </remarks>
[AllowAnonymous]
[Route("v1/legal")]
public class LegalController : ApiControllerBase
{
    /// <summary>
    /// Yayında olan hukuki belgeler — <b>metinleriyle birlikte</b>.
    /// </summary>
    /// <param name="registrationOnly">
    /// <c>true</c> ise yalnız kayıt ekranında sorulacaklar. ⚠️ Varsayılan <c>false</c>:
    /// ayarlar ekranı yayında olan <b>her</b> belgeyi okuyabilmeli.
    /// </param>
    [HttpGet("documents")]
    public async Task<IActionResult> Documents([FromQuery] bool registrationOnly = false)
    {
        var documents = await Sender.Send(new GetLegalDocumentsQuery { RegistrationOnly = registrationOnly });
        return Success(documents);
    }

    /// <summary>
    /// Tek belge (ayarlardan "KVKK Aydınlatma Metni"ne dokunulduğunda).
    /// </summary>
    /// <remarks>
    /// ⚠️ Tanınmayan tür <b>varsayılana düşmez</b>, <c>404</c> olur: yanlış hukuki metni
    /// göstermek, kullanıcıya okumadığı bir belgeyi onaylatmanın en sessiz yoludur.
    /// </remarks>
    [HttpGet("documents/{type}")]
    public async Task<IActionResult> Document(string type)
    {
        var document = await Sender.Send(new GetLegalDocumentByTypeQuery(type));

        return document is null
            ? NotFound(new { code = "NOT_FOUND", message = "Belge bulunamadı." })
            : Success(document);
    }
}
