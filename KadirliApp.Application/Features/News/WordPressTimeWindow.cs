using System;
using System.Globalization;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — <b>WordPress zaman penceresinin tek sahibi.</b>
///
/// 🔴 <b>Ölçülen gerçek (11 Ağustos 2026, canlı API):</b> <c>modified_after</c> parametresi
/// <c>post_modified</c> ile, yani <b>SİTE-YEREL</b> saatle (UTC+3, <c>gmt_offset=3</c>)
/// karşılaştırılıyor:
/// <code>
/// modified_after=2026-08-11T10:11:36  (yerel değer)  ->  X-WP-Total: 0
/// modified_after=2026-08-11T07:11:36  (UTC  değer)   ->  X-WP-Total: 4
/// </code>
///
/// 🔑 <b>Bu, §7 madde 6'daki "TR günü, 00:00 UTC" tuzağının birebir kardeşi</b> — ve o sınıf
/// bu projede <b>4 kez</b> tekrarladı (11.7/11.10/11.11/11.13). Yön kritik:
/// <list type="bullet">
///   <item>Yerel yerine <b>UTC</b> göndermek → pencere genişler → mükerrer kayıt →
///         upsert idempotent olduğu için <b>zararsız</b>.</item>
///   <item>Ters yön (damgayı "UTC'ye çevireyim" diye 3 saat <b>ileri</b> almak) → her koşuda
///         <b>3 saatlik haber sessizce atlanır</b>, hiçbir hata oluşmaz, panelde belirti yok.</item>
/// </list>
///
/// ⚠️ <c>DateTime.UtcNow</c> <b>asla</b> doğrudan <c>modified_after</c>'a yazılmaz; imleç
/// <c>modified_gmt</c>'den (UTC) saklanır, sorguya buradan geçerek gider.
/// </summary>
public static class WordPressTimeWindow
{
    /// <summary>Kaynağın <c>gmt_offset</c>'i. Kaynak taşınırsa değişecek <b>tek</b> sabit.</summary>
    public const int SiteUtcOffsetHours = 3;

    /// <summary>
    /// Bilinçli çakışma payı: imleç <b>bu kadar geriye</b> alınarak sorgulanır.
    /// </summary>
    /// <remarks>
    /// Neden gerekli: saat farkı, kaynağın önbelleği (LiteSpeed) ve koşu sırasında
    /// yayınlanan bir haber, tam sınırdaki kayıtları iki koşu <b>arasına</b> düşürebilir.
    /// Bedeli birkaç mükerrer okuma (upsert idempotent), kazancı "hiç görünmeyen haber" yok.
    /// </remarks>
    public static readonly TimeSpan Overlap = TimeSpan.FromMinutes(30);

    /// <summary>UTC damgayı kaynağın yerel saatine çevirir.</summary>
    public static DateTime ToSiteLocal(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(SiteUtcOffsetHours);

    /// <summary>Kaynağın yerel damgasını UTC'ye çevirir (<c>date</c>/<c>modified</c> alanları için).</summary>
    public static DateTime ToUtc(DateTime siteLocal) =>
        DateTime.SpecifyKind(siteLocal.AddHours(-SiteUtcOffsetHours), DateTimeKind.Utc);

    /// <summary>
    /// İmleçten (UTC) sorgu tabanını üretir: <b>çakışma payı düşülür, sonra yerele çevrilir</b>.
    /// Sıra önemsiz görünür ama tek satırda olması şart — iki yerde yazılırsa biri payı unutur.
    /// </summary>
    public static DateTime QueryFloor(DateTime cursorUtc) => ToSiteLocal(cursorUtc - Overlap);

    /// <summary>WordPress'in beklediği biçim (ISO 8601, saat dilimi <b>eki yok</b>).</summary>
    /// <remarks>
    /// ⚠️ Sonuna <c>Z</c> ya da <c>+03:00</c> eklemek, alanın yerel olarak karşılaştırıldığı
    /// gerçeğini değiştirmez — yalnız okuyanı yanıltır.
    /// </remarks>
    public static string Format(DateTime siteLocal) =>
        siteLocal.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>İmleçten doğrudan sorgu metni (çağıranın iki adımı ayrı yazmasına gerek yok).</summary>
    public static string ModifiedAfterParameter(DateTime cursorUtc) => Format(QueryFloor(cursorUtc));

    /// <summary>
    /// Kaynaktan gelen bir gönderinin damgalarını UTC'ye normalleştirir.
    /// <paramref name="gmt"/> varsa <b>o kullanılır</b>; yoksa yerel damga çevrilir.
    /// </summary>
    /// <remarks>
    /// Yedek yol gerçek: <c>_fields</c> ile <c>*_gmt</c> alanını istemeyi unutan bir sorgu,
    /// damgaları 3 saat ileride kaydeder ve haberler <b>gelecekten</b> görünür.
    /// </remarks>
    public static DateTime NormalizeToUtc(DateTime? gmt, DateTime? siteLocal) =>
        gmt.HasValue ? DateTime.SpecifyKind(gmt.Value, DateTimeKind.Utc)
        : siteLocal.HasValue ? ToUtc(siteLocal.Value)
        : DateTime.UtcNow;
}
