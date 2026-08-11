using System.Collections.Generic;
using System.Linq;
using KadirliApp.Application.Common.Interfaces;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — kaynaktaki görsel boyutlarından <b>hangisinin aynalanacağına</b> karar veren
/// tek yer.
///
/// 📊 <b>Ölçülen gerçek (400 haberlik korpus, 11 Ağustos 2026):</b>
/// <list type="bullet">
///   <item>Evrensel boyutlar: <c>thumbnail</c> 150×85 · <c>medium</c> 300×170 · <c>full</c> 650×368.</item>
///   <item>🔴 <b>"Büyük görsel" YOK:</b> 40 haberin 39'unda <c>full</c> bile 650px — detayda
///         3x telefonda yukarı ölçekleniyor. Bu bir tasarım kısıtı, hata değil.</item>
///   <item>⚠️ <c>large</c> / <c>medium_large</c> 40 haberde <b>1</b> kez var →
///         <b>zincire konulamaz</b>: konulsaydı 39 haberde zincir sessizce bir adım kayardı.</item>
///   <item>⚠️ <c>jannah-image-*</c> 40/40 var <b>ama WP temasından geliyor</b> → tema
///         değişirse <b>sessizce kaybolur</b>. Yedek zincirde olabilir, <b>tek kaynak</b> olamaz.</item>
/// </list>
/// </summary>
public static class NewsImagePicker
{
    /// <summary>
    /// Kapak için yedek zinciri. İlk sıra <c>full</c>; gerisi "hiç görsel yok"a düşmemek için.
    /// </summary>
    /// <remarks>
    /// Şüphede kalınca <b>göstermek</b> doğru yön (§7 madde 49'un sınıfı): <c>full</c>'ü
    /// olmayan tek bir haber yüzünden kartı görselsiz bırakmak, küçük bir görsel göstermekten
    /// kötüdür.
    /// </remarks>
    public static readonly IReadOnlyList<string> CoverChain =
        new[] { "full", "medium", "jannah-image-large", "thumbnail" };

    /// <summary>Küçük görsel zinciri (12.13/12.14 liste kartı için hazır — bugün kapak aynalanıyor).</summary>
    public static readonly IReadOnlyList<string> ThumbnailChain =
        new[] { "medium", "jannah-image-large", "thumbnail", "full" };

    /// <summary>
    /// ⚠️ Bilinçli olarak <b>hiçbir zincirde olmayan</b> boyutlar. Testte adıyla kilitli:
    /// birisi "daha büyük görsel" arayışıyla bunları zincire eklerse, 40'ta 39 haberde
    /// bulunmadıkları için sessizce atlanır ve hiçbir kazanç sağlamaz — ama zinciri okuyan
    /// bir sonraki kişi kaynağın büyük görsel verdiğini <b>sanır</b>.
    /// </summary>
    public static readonly IReadOnlySet<string> UnreliableSizes =
        new HashSet<string>(System.StringComparer.Ordinal) { "large", "medium_large" };

    public static NewsSourceImage? PickCover(IReadOnlyDictionary<string, NewsSourceImage>? sizes) =>
        Pick(sizes, CoverChain);

    public static NewsSourceImage? PickThumbnail(IReadOnlyDictionary<string, NewsSourceImage>? sizes) =>
        Pick(sizes, ThumbnailChain);

    private static NewsSourceImage? Pick(
        IReadOnlyDictionary<string, NewsSourceImage>? sizes, IReadOnlyList<string> chain)
    {
        if (sizes is null || sizes.Count == 0) return null;

        foreach (var key in chain)
            if (sizes.TryGetValue(key, out var image) && !string.IsNullOrWhiteSpace(image.Url))
                return image;

        // Zincirin tamamı boşsa: kaynak beklenmedik bir boyut adı kullanıyor demektir.
        // "Hiç görsel yok" demek yerine eldeki ilk geçerli boyutu al — ama güvenilmez
        // boyutları (large/medium_large) buradan da geçirme, yoksa zincirin anlamı kalmaz.
        return sizes
            .Where(kv => !UnreliableSizes.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value.Url))
            .Select(kv => kv.Value)
            .FirstOrDefault();
    }
}
