using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Queries;

/// <summary>
/// Faz 12.13 — panelin kategori listesi (<c>LookupsAdmin</c> içindeki "Haber Kategorileri").
/// </summary>
/// <remarks>
/// 🔴 <b>Görünürlük semantiği DIŞLAMA'dır</b>, "en az bir görünür kategorisi olsun" değil.
/// Ölçüm bunu zorluyor: bir haber çoklu kategoride (<c>[49,51,52]</c>). OR semantiğinde
/// E-Gazete'yi kapatmak <b>işe yaramazdı</b> — o haberler "Haberler"e de ait olduğu için
/// görünmeye devam eder, yönetici anahtarı çevirir ve <b>hiçbir şey olmazdı</b>
/// (§7 madde 37'nin *"panelin en sinsi yalan biçimi"*).
///
/// ⚠️ Sayımların hepsi <b>toplu</b> (GROUP BY) yapılır, kategori başına alt sorgu ile değil:
/// 12.12 sonrası denetimin 5. bulgusu tam olarak buydu ve 15 kategori × 27k satır demekti.
/// </remarks>
public record GetNewsCategoriesAdminQuery : IRequest<List<NewsCategoryAdminDto>>;

public class GetNewsCategoriesAdminQueryHandler
    : IRequestHandler<GetNewsCategoriesAdminQuery, List<NewsCategoryAdminDto>>
{
    /// <summary>
    /// "Yeni kategori" penceresi.
    /// </summary>
    /// <remarks>
    /// 📌 <b>Dürüst sınır:</b> bu bir "okundu" işareti değil, bir zaman penceresi — sekiz gün
    /// önce açılmış ve fark edilmemiş bir kategori artık "yeni" görünmez. Kalıcı bir
    /// onaylama alanı (<c>AcknowledgedAt</c> + "gördüm" butonu) daha doğru olurdu; bilinçli
    /// olarak ertelendi ve ekrandaki metin de <b>"son 7 günde"</b> diyerek bunu söylüyor —
    /// panelin söylediği şey, gerçekte bildiği şeyden fazla olmamalı.
    /// </remarks>
    public static readonly TimeSpan NewCategoryWindow = TimeSpan.FromDays(7);

    private readonly IUnitOfWork _uow;

    public GetNewsCategoriesAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<NewsCategoryAdminDto>> Handle(GetNewsCategoriesAdminQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var articles = _uow.Repository<NewsArticle>().Query();

        // 1) Bugün gerçekten görünen haberlerin kategori dağılımı.
        //    🔑 Dışlanmamış bir kategori için bu sayı, "dışlarsam kaç haber kalkar"ın TA KENDİSİ:
        //    görünür küme zaten "hiçbir dışlanmış kategorisi olmayan" haberlerden oluşuyor.
        var visibleCounts = await NewsVisibility.Published(articles)
            .SelectMany(a => a.Categories)
            .GroupBy(c => c.Id)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

        // 2) Kategorinin toplam yükü (arşivlenmiş ve kaynaktan kalkmış olanlar dâhil).
        var totalCounts = await articles
            .SelectMany(a => a.Categories)
            .GroupBy(c => c.Id)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

        // 3) Dışlanmış kategoriler için ters yön: "dışlamayı kaldırırsam kaç haber GERİ GELİR".
        //    Yalnız TEK bir dışlanmış kategorisi olan haberler sayılır — ikinci bir dışlanmış
        //    kategorisi olan haber bu anahtar çevrilse de görünmeyecek ve onu saymak
        //    yöneticiye doğrudan yalan söylemek olurdu.
        var restoreCounts = await articles
            .Where(a => !a.IsArchived
                        && a.SourceState == NewsSourceStates.Published
                        && a.Categories.Count(c => c.IsExcluded) == 1)
            .SelectMany(a => a.Categories)
            .Where(c => c.IsExcluded)
            .GroupBy(c => c.Id)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

        var categories = await _uow.Repository<NewsCategory>().Query()
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new NewsCategoryAdminDto
            {
                Id = c.Id,
                WpId = c.WpId,
                Name = c.Name,
                Slug = c.Slug,
                SourceArticleCount = c.ArticleCount,
                IsExcluded = c.IsExcluded,
                ShowInFilterStrip = c.ShowInFilterStrip,
                DisplayOrder = c.DisplayOrder,
                IsNew = c.CreatedAt >= now - NewCategoryWindow
            })
            .ToListAsync(ct);

        foreach (var category in categories)
        {
            category.VisibleArticleCount = visibleCounts.TryGetValue(category.Id, out var visible) ? visible : 0;
            category.TotalArticleCount = totalCounts.TryGetValue(category.Id, out var total) ? total : 0;
            category.AffectedCount = category.IsExcluded
                ? (restoreCounts.TryGetValue(category.Id, out var restore) ? restore : 0)
                : category.VisibleArticleCount;
        }

        return categories;
    }
}
