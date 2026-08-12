using System.Collections.Generic;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — haber gövdesinin <b>beyaz listesi</b>. Kütüphanenin varsayılanı değil,
/// bizim ürün kararımız; bu yüzden Application'da yaşıyor ve testle kilitli.
/// </summary>
/// <remarks>
/// 📊 <b>Korpusta gerçekten bulunanlar</b> (400 haber): <c>p</c> 3674 · <c>div</c> 864 ·
/// <c>figure</c> 720 · <c>img</c> 272 · <c>br</c> 260 · <c>a</c> 106 · <c>span</c> 88 ·
/// <c>strong</c> 24 · <b><c>object</c> 14 · <c>video</c> 4 · <c>form</c> 2</b>.
/// Son üçü tam olarak bu listenin var olma sebebi: <c>&lt;form&gt;</c> içeren bir haber
/// gövdesi, uygulamanın içinde <b>başka bir siteye veri gönderen</b> bir kutu demektir.
/// </remarks>
public static class NewsHtmlPolicy
{
    /// <summary>Kalacak etiketler.</summary>
    public static readonly IReadOnlySet<string> AllowedTags = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "u", "a",
        "figure", "figcaption", "img",
        "ul", "ol", "li", "blockquote",
        "h2", "h3", "h4"
    };

    /// <summary>
    /// Kalacak öznitelikler. ⚠️ <c>style</c> <b>yok</b>: kaynağın tema stilleri uygulamanın
    /// tipografisini bozar ve <c>style</c> içinden çalışan saldırı biçimleri vardır.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedAttributes = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "href", "title", "alt", "src", "width", "height"
    };

    /// <summary>
    /// İçeriğiyle birlikte atılacaklar. <c>script</c>/<c>style</c> için bu şart:
    /// yalnız etiketi atıp içeriği bırakmak, sayfaya <b>CSS/JS metnini</b> düz yazı olarak basar.
    /// </summary>
    public static readonly IReadOnlySet<string> DroppedWithContent = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "object", "embed", "form", "input", "video", "audio", "svg"
    };

    /// <summary>İzin verilen şemalar — <c>javascript:</c> ve <c>data:</c> bilinçli olarak yok.</summary>
    public static readonly IReadOnlySet<string> AllowedSchemes =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "http", "https" };

    /// <summary>
    /// ✅ <b>Faz 12.14: metin arası görseller de aynalanıyor</b> (12.12'de bilinçli olarak
    /// ertelenmişti).
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Borcun neden kapatıldığı:</b> haberlerin %35'inde 1–3 gövde görseli var ve
    /// <b>%9'u süreli <c>fbcdn</c>/<c>outlook</c> linki</b> — yani zamanla <b>mutlaka</b>
    /// 403'e düşecekler. Düştüklerinde istemci onları <i>zarifçe gizliyor</i> (§7 madde 61),
    /// yani hasarın <b>hiçbir belirtisi olmayacaktı</b>: haberler sessizce görselsizleşecek,
    /// uçlar 200 dönecek, log temiz kalacaktı. Bu, ertelenebilir değil <b>zamanla kötüleşen</b>
    /// bir borçtu.
    ///
    /// ⚠️ Aynalama <b>ideal değil, dayanıklı</b>: indirilemeyen görsel gövdede <b>olduğu gibi
    /// bırakılır</b> (hotlink) — yani en kötü hâlde 12.14 öncesine düşülür, haber düşmez.
    /// Yeniden deneme yoktur ve sebebi ölçülmüş: imzalı bir adresin hatası <b>kalıcıdır</b>,
    /// her 15 dakikada bir denemek günde 96 boşuna istek demekti.
    ///
    /// 📌 12.14 <b>öncesinden</b> kalan kayıtlar <c>MirrorNewsBodyImagesJob</c> ile turlu
    /// olarak onarılır: senkron yalnız <i>kaynakta değişen</i> haberi yeniden yazdığı için
    /// o kayıtlar başka türlü hiç düzelmezdi.
    /// </remarks>
    public const bool MirrorsInlineImages = true;
}
