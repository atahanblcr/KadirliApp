using System;
using System.Linq;
using System.Linq.Expressions;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — <c>NewsArticle</c> → <see cref="NewsArticleDto"/> projeksiyonunun <b>tek sahibi</b>.
/// </summary>
/// <remarks>
/// 🔑 §7 madde 43'ün dersi: liste ve detay iki ayrı <c>Select</c> bloğu olsaydı, yarın eklenen
/// bir alan yalnız birine yazıldığında <b>detay ekranı sessizce eksik</b> kalırdı — ne
/// derleyici, ne test, ne de gözle bakan insan yakalar.
///
/// ⚠️ Gövde (<see cref="NewsArticleDto.ContentHtml"/>) bir <b>parametreyle</b> açılıp
/// kapanıyor, ikinci bir ifade yazılarak değil. İki ifade yazmak tam olarak yukarıdaki
/// hatanın kapısıdır; parametre ise tek satırda görünür ve testle kilitli.
///
/// 🔑 <b>Görsel önceliği:</b> yöneticinin koyduğu kapak > kaynağınki. Kaynağın 650×368
/// sınırı (ölçüm: 40 haberin 39'u) tam da bunun için: yönetici gerektiğinde daha iyi bir
/// görsel koyabilmeli.
/// </remarks>
public static class NewsProjection
{
    public static Expression<Func<NewsArticle, NewsArticleDto>> Select(bool includeContent) => x => new NewsArticleDto
    {
        Id = x.Id,
        Title = x.TitleOverride ?? x.SourceTitle,
        Excerpt = x.ExcerptOverride ?? x.SourceExcerpt,
        ContentHtml = includeContent ? x.SourceContentHtml : null,
        ImageUrl = x.CoverImageOverrideFile != null
            ? x.CoverImageOverrideFile.CdnUrl
            : (x.SourceImage != null ? x.SourceImage.CdnUrl : null),
        ImageWidth = x.CoverImageFileIdOverride != null ? null : x.SourceImageWidth,
        ImageHeight = x.CoverImageFileIdOverride != null ? null : x.SourceImageHeight,
        SourceUrl = x.SourceUrl,
        PublishedAt = x.SourcePublishedAt,
        ModifiedAt = x.SourceModifiedAt,
        ReadingMinutes = x.ReadingMinutes,
        IsFeatured = x.IsFeatured,
        Categories = x.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new NewsCategoryRefDto { Id = c.Id, Name = c.Name, Slug = c.Slug })
            .ToList()
    };
}
