namespace KadirliApp.Application.Common.Interfaces;

/// <summary>Kaynaktaki bir görsel boyutu (<c>media_details.sizes</c> girdisi).</summary>
public sealed record NewsSourceImage(string Url, int? Width, int? Height);

/// <summary>
/// Kaynaktan okunan bir gönderi. <b>Ham HTML taşır</b> — temizlik ve düz metin üretimi
/// alım hattının bir sonraki adımında (<see cref="INewsHtmlSanitizer"/>) yapılır.
/// </summary>
public sealed record NewsSourcePost(
    int WpId,
    string Title,
    string? ExcerptHtml,
    string ContentHtml,
    string Url,
    DateTime PublishedAtUtc,
    DateTime ModifiedAtUtc,
    IReadOnlyList<int> CategoryWpIds,
    IReadOnlyDictionary<string, NewsSourceImage> ImageSizes);

public sealed record NewsSourceCategory(int WpId, string Name, string Slug, int ArticleCount);

/// <summary>Tek bir sayfa + kaynağın bildirdiği toplamlar (<c>X-WP-Total*</c> başlıkları).</summary>
public sealed record NewsSourcePage(IReadOnlyList<NewsSourcePost> Posts, int TotalPages, int TotalCount);

/// <summary>
/// Mutabakat penceresi: kaynakta <b>şu anda yayında olan</b> kimlikler ve pencerenin en eski
/// yayın tarihi.
/// </summary>
/// <remarks>
/// ⚠️ Tarih olmadan bu liste tehlikelidir: "bizde olup listede olmayan her kayıt gitmiştir"
/// kuralı, tarama penceresi arşiv derinliğimizden dar olduğu anda <b>bütün eski haberleri</b>
/// <c>gone</c> yapar.
/// </remarks>
public sealed record NewsSourceIdWindow(IReadOnlyList<int> WpIds, DateTime? OldestPublishedAtUtc);

/// <summary>
/// Faz 12.12 — haber kaynağının (WordPress REST API) <b>tek kapısı</b>.
///
/// 🔑 Application katmanı yalnız bu arayüzü görür: HTTP, yeniden deneme, <c>X-WP-Total</c>
/// başlıkları ve <c>_embed</c> ayrıştırması Infrastructure'da kalır (katman kuralı §1).
/// Bu sayede senkron mantığı <b>ağa çıkmadan</b> test edilebilir — bu bloğun bütün
/// davranış testleri sahte bir istemciyle koşuyor.
/// </summary>
public interface INewsSourceClient
{
    /// <summary>
    /// İleri imleç: <c>orderby=modified&amp;order=asc</c> + <c>modified_after</c>.
    /// </summary>
    /// <param name="siteLocalSince">
    /// 🔴 <b>SİTE-YEREL</b> damga (UTC+3). Ham UTC verilirse pencere 3 saat geriye kayar
    /// (zararsız, upsert idempotent); ters yön <b>3 saatlik haberi sessizce atlar</b>.
    /// Dönüşümün tek sahibi <c>WordPressTimeWindow</c>.
    /// </param>
    Task<NewsSourcePage> GetPostsModifiedSinceAsync(DateTime siteLocalSince, int page, int perPage, CancellationToken ct);

    /// <summary>
    /// Geri imleç: <c>orderby=date&amp;order=desc</c> + <c>before</c> — arşiv derinleştirmesi.
    /// </summary>
    /// <param name="beforeSiteLocal">
    /// Bu andan <b>öncesini</b> getir (site-yerel). <c>null</c> = en yeniden başla.
    /// 🔑 Sayfa numarası yerine tarih kullanılıyor: koşular arasında yeni bir haber
    /// yayınlandığında sayfa numaraları bir kayar ve tam sınırdaki haber
    /// <b>sonsuza kadar atlanır</b> — hiçbir hata vermeden.
    /// </param>
    Task<NewsSourcePage> GetPostsByDateDescendingAsync(DateTime? beforeSiteLocal, int perPage, CancellationToken ct);

    Task<IReadOnlyList<NewsSourceCategory>> GetCategoriesAsync(CancellationToken ct);

    /// <summary>Mutabakat: <c>_fields=id,date_gmt</c> ile yalnız kimlik + tarih (27k kimlik ≈ birkaç yüz KB).</summary>
    Task<NewsSourceIdWindow> GetPublishedIdWindowAsync(int maxPosts, CancellationToken ct);
}
