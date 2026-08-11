using System;
using System.Linq;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — <b>"vatandaş neyi görür?" sorusunun tek cevabı.</b>
///
/// 🔴 Değişmez Kural #3 bu modülde üç koşula dönüşüyor:
/// <list type="number">
///   <item><b>Arşivlenmemiş</b> — yönetici geri alınabilir biçimde gizlemiş olabilir.</item>
///   <item><b>Kaynağı yayında</b> (<c>SourceState = published</c>) — kaynaktan kalkan haber
///         bizde durur ama görünmez (<c>ReconcileNewsJob</c>).</item>
///   <item><b>Dışlanmış kategorisi yok</b> — çoklu kategori yüzünden "bir tanesi bile
///         dışlanmışsa gizle" (bkz. <see cref="NewsCategory.IsExcluded"/>).</item>
/// </list>
///
/// 🔑 <b>Neden ayrı bir sınıf:</b> aynı tanım en az üç yerde gerekiyor — public liste, public
/// detay ve (12.13'te) panelin "yayında kaç haber var" sayacı. Ayrı yazılırlarsa panel ile
/// vatandaş <b>farklı gerçeklik görür ve kimse hata almaz</b> (§7 madde 23; 11.15c'de canlıda
/// yaşandı: panel "Aktif İlanlar 1" derken uç 0 döndürüyordu).
/// </summary>
public static class NewsVisibility
{
    public static IQueryable<NewsArticle> Published(IQueryable<NewsArticle> query) =>
        query.Where(x =>
            !x.IsArchived &&
            x.SourceState == NewsSourceStates.Published &&
            !x.Categories.Any(c => c.IsExcluded));

    /// <summary>
    /// Öne çıkarma süresi dolmuş haber "öne çıkan" değildir.
    /// </summary>
    /// <remarks>
    /// ⚠️ Süre kontrolü <b>sunucuda</b>: istemcide yapılsaydı süresi dolmuş bir haber
    /// mağazadaki eski sürümlerde manşette kalırdı ve hiçbir yerde görünmezdi.
    /// </remarks>
    public static IQueryable<NewsArticle> Featured(IQueryable<NewsArticle> query, DateTime now) =>
        query.Where(x => x.IsFeatured && (x.FeaturedUntil == null || x.FeaturedUntil > now));
}
