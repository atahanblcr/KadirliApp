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

    /// <summary>
    /// Faz 12.17 (plan dışı ek) — <b>belirli bir sürümün</b> metni: "ben neyi onaylamıştım?"
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔑 12.16 rızayı sürüme bağladı ve <c>GET /v1/users/me/consents</c> onayladığınız
    /// <c>consentedVersionId</c>'yi söylüyordu — ama o kimlikten <b>metne</b> giden bir yol
    /// yoktu: yeni sürüm yayınlandığı an vatandaş <b>neyi kabul ettiğini bir daha
    /// göremiyordu</b>.
    /// </para>
    /// <para>
    /// 🔴 <b>Taslak 404</b>'tür; yürürlükten kalkmış sürüm <b>döner</b> (<c>isLive:false</c> ile) —
    /// ucun bütün amacı zaten eski metni okuyabilmek.
    /// </para>
    /// </remarks>
    [HttpGet("versions/{versionId:guid}")]
    public async Task<IActionResult> Version(Guid versionId)
    {
        var version = await Sender.Send(new GetLegalVersionByIdQuery(versionId));

        return version is null
            ? NotFound(new { code = "NOT_FOUND", message = "Metin bulunamadı." })
            : Success(version);
    }
}
