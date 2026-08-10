using System.Security.Cryptography;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 12.9 — panelin Content-Security-Policy başlığı (istek başına nonce ile).
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden var:</b> 12.9 öncesinde panelde CSP başlığı <b>hiç yoktu</b> ve dört
/// üçüncü taraf origin (<c>cdn.tailwindcss.com</c>, <c>cdnjs.cloudflare.com</c>,
/// <c>fonts.googleapis.com</c>, <c>unpkg.com</c>) <b><c>super_admin</c> oturumu açık</b>
/// bir tarayıcıda sınırsız JavaScript çalıştırıyordu. Origin'ler artık yerelleştirildi;
/// bu başlık o kararı <b>tarayıcıda zorunlu</b> kılar — yani yarın bir görünüme
/// yanlışlıkla bir CDN satırı girse bile tarayıcı onu <b>yüklemez</b>.
/// </para>
/// <para>
/// 🔴 <b><c>'unsafe-inline'</c> BİLİNÇLİ OLARAK YOK</b> (script tarafında).
/// Açmak korumanın kendisini iptal ederdi: panelde gösterilen metnin bir kısmı
/// <i>vatandaştan</i> geliyor (hata kayıtlarının mesajı, şikayet başlıkları) ve
/// depolanmış XSS bu projenin zaten savaştığı bir sınıf (görünmez sözleşme #33).
/// Bedeli 12.9'da ödendi: <b>47 satır içi <c>on*=</c> işleyicisi</b> delege
/// dinleyicilere taşındı, çünkü nonce yalnız <c>&lt;script&gt;</c> <b>bloklarını</b>
/// kapsar, <b>öznitelikleri kapsamaz</b>.
/// </para>
/// <para>
/// ⚠️ <b><c>style-src</c>'ta <c>'unsafe-inline'</c> VAR ve bu bir taviz.</b> Sebep
/// Leaflet: harita panellerini/işaretçilerini konumlandırmak için elemanların
/// <c>style</c> özniteliğine yazıyor. CSP3'ün <c>style-src-attr</c>'ı bunu daha dar
/// kapsayabilirdi ama Firefox/Safari onu yok sayıp <c>style-src</c>'a düşer — yani
/// harita seçici <b>o tarayıcılarda</b> kırılırdı ve bu, 12.9'un düzeltmek için
/// var olduğu hasarın aynısı olurdu. Stil enjeksiyonu betik enjeksiyonundan
/// belirgin biçimde zayıf bir vektör; taviz bilinçli ve <b>yalnız stile</b> ait.
/// </para>
/// <para>
/// ⚠️ <c>img-src</c>'ta <c>tile.openstreetmap.org</c> <b>bilinçli olarak açık</b>:
/// bir dünya haritasının kareleri self-host edilemez. Ama "Leaflet gelmedi" ile
/// "kareler gelmedi" aynı şey değil — ilki seçiciyi <b>tamamen öldürüyordu</b>,
/// ikincisinde harita gri kalır ve <b>koordinat seçimi çalışmaya devam eder</b>.
/// </para>
/// </remarks>
public sealed class ContentSecurityPolicyMiddleware
{
    /// <summary>
    /// Nonce'un görünümlere taşındığı anahtar. Razor tarafı
    /// <c>@Context.Items["csp-nonce"]</c> ile okur.
    /// </summary>
    public const string NonceItemKey = "csp-nonce";

    /// <summary>Harita karelerinin geldiği tek dış origin (yukarıdaki gerekçe).</summary>
    public const string MapTileOrigin = "https://tile.openstreetmap.org";

    private readonly RequestDelegate _next;

    public ContentSecurityPolicyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = GenerateNonce();
        context.Items[NonceItemKey] = nonce;

        // ⚠️ Başlık YANIT YAZILMADAN ÖNCE eklenmeli. Yanıt gövdesi akmaya
        // başladıktan sonra başlık eklemek sessizce yok sayılır (ASP.NET Core
        // fırlatmaz, yalnız yazmaz) — yani koruma "var gibi görünür, yoktur".
        context.Response.OnStarting(() =>
        {
            // Zaten varsa dokunma: ters vekil ya da başka bir katman kendi
            // politikasını koyduysa iki başlık ÇAKIŞIR ve tarayıcı ikisinin de
            // KESİŞİMİNİ uygular — panel sebepsiz kırılır.
            if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                context.Response.Headers["Content-Security-Policy"] = BuildPolicy(nonce);
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }

    /// <summary>
    /// Politika metnini üretir. <b>Test bu metoda bakar</b> — canlı yanıt başlığına
    /// bakan test de ayrıca var, ikisi birlikte "üretiliyor ama gönderilmiyor"
    /// boşluğunu kapatıyor.
    /// </summary>
    public static string BuildPolicy(string nonce) => string.Join("; ",
    [
        "default-src 'self'",
        // 🔴 'unsafe-inline' YOK — bkz. sınıf açıklaması.
        $"script-src 'self' 'nonce-{nonce}'",
        // ⚠️ Taviz, yalnız stile ait ve Leaflet yüzünden — bkz. sınıf açıklaması.
        "style-src 'self' 'unsafe-inline'",
        // data: → görsel önizlemesi ve Leaflet'in ürettiği bazı görseller.
        $"img-src 'self' data: blob: {MapTileOrigin}",
        "font-src 'self'",
        // Panel kendi ucundan başka bir yere istek atmaz (duyuru türü ekleme fetch'i).
        "connect-src 'self'",
        // Panel hiçbir yere gömülmez ve hiçbir şey gömmez.
        "frame-ancestors 'none'",
        "frame-src 'none'",
        "object-src 'none'",
        // <base> enjekte edilerek göreli script yollarının kaçırılmasını engeller.
        "base-uri 'self'",
        // Form yalnız panele post edilebilir; enjekte edilmiş bir form dışarı veri taşıyamaz.
        "form-action 'self'"
    ]);

    /// <summary>
    /// İstek başına 128 bitlik kriptografik nonce (<b>base64url</b>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>İstek başına ve tahmin edilemez olmak zorunda.</b> Sabit ya da
    /// öngörülebilir bir nonce, enjekte edilen betiğin onu <b>kopyalayarak</b>
    /// çalışmasına izin verir — yani CSP görünürde durur, fiilen yoktur.
    /// </para>
    /// <para>
    /// 🐛 <b>Neden base64url, düz base64 değil (canlı doğrulamada bulundu).</b>
    /// Düz base64 <c>+</c> ve <c>/</c> üretir; Razor öznitelik değerlerini
    /// HTML-kaçırdığı için nonce sayfaya <c>…ErK&amp;#x2B;7Mf…</c> olarak basılıyordu.
    /// Tarayıcı bunu doğru çözüyor, yani <i>çalışıyordu</i> — ama güvenlik kritik
    /// bir değeri karşılaştırmadan önce bir kodlama gidiş-dönüşünden geçirmek,
    /// bir gün ayrışacak türden bir kırılganlık: başlıktaki metin ile sayfadaki
    /// metin artık <b>bayt bayt aynı değildi</b> ve bu farkın kendisi hiçbir yerde
    /// görünmüyordu. Base64url (<c>-</c> ve <c>_</c>) CSP'nin dilbilgisinde
    /// geçerlidir ve HTML kaçırması <b>hiç devreye girmez</b>.
    /// </para>
    /// </remarks>
    private static string GenerateNonce()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        // .NET 8'de hazır `Base64Url` yok (9 ile geldi) — dönüşüm elle yapılıyor.
        // Dolgu (`=`) atılıyor: CSP dilbilgisi izin veriyor ama gereksiz.
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
