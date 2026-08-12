using System;
using System.Linq;
using System.Linq.Expressions;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.13 — <c>NewsArticle</c> → <see cref="NewsAdminDto"/> projeksiyonunun <b>tek sahibi</b>
/// (panel listesi <b>ve</b> ayrıntısı).
/// </summary>
/// <remarks>
/// 🔑 §7 madde 43'ün dersi: 12.4'te etkinliğin liste ve detay sorguları iki ayrı <c>Select</c>
/// bloğuydu; dört yeni alan yalnız birine eklenseydi <b>detay ekranı sessizce eksik</b>
/// kalırdı. Burada gövde bir <b>parametreyle</b> açılıp kapanıyor, ikinci bir ifade yazılarak
/// değil.
///
/// ⚠️ İfade ağacına <b>çevrilemeyen</b> hesaplar (durum türetmesi, öne çıkarmanın süresi)
/// burada <b>yapılmaz</b>: ham alanlar taşınır ve <see cref="Finish"/> bellekte tek adımda
/// tamamlar. Aynı desen <c>EventProjection</c>'da da var — sebebi, ifade ağacına sığdırmak
/// için kuralın SQL'de <b>ikinci kez</b> yazılmasının önüne geçmek.
/// </remarks>
public static class NewsAdminProjection
{
    /// <param name="users">
    /// Yönetici adlarını çözmek için (denetim izi ve giriş denemelerindeki aynı desen).
    /// ⚠️ Çağıran <c>IgnoreQueryFilters()</c> ile geçirir: hesabını silmiş yöneticinin
    /// yaptığı düzeltme kayıtta durur, adı "—" olmaz.
    /// </param>
    public static Expression<Func<NewsArticle, NewsAdminDto>> Select(
        IQueryable<User> users, bool includeContent) => x => new NewsAdminDto
    {
        Id = x.Id,
        WpId = x.WpId,

        // Etkin değerin kuralı public projeksiyonla BİREBİR aynı (`NewsProjection`):
        // ayrışsalardı panel bir başlık gösterir, vatandaş başkasını görürdü.
        Title = x.TitleOverride ?? x.SourceTitle,
        SourceTitle = x.SourceTitle,
        TitleOverride = x.TitleOverride,

        Excerpt = x.ExcerptOverride ?? x.SourceExcerpt,
        SourceExcerpt = x.SourceExcerpt,
        ExcerptOverride = x.ExcerptOverride,

        ImageUrl = x.CoverImageOverrideFile != null
            ? x.CoverImageOverrideFile.CdnUrl
            : (x.SourceImage != null ? x.SourceImage.CdnUrl : null),
        SourceImageUrl = x.SourceImage != null ? x.SourceImage.CdnUrl : null,
        CoverImageFileIdOverride = x.CoverImageFileIdOverride,
        CoverImageOverrideUrl = x.CoverImageOverrideFile != null ? x.CoverImageOverrideFile.CdnUrl : null,

        SourceUrl = x.SourceUrl,
        PublishedAt = x.SourcePublishedAt,
        ModifiedAt = x.SourceModifiedAt,
        ReadingMinutes = x.ReadingMinutes,

        // 🔴 HTML değil DÜZ METİN: panelde `@Html.Raw` yasak (§7 madde 33) ve kaynağın
        // `<img>`'leri CSP'ye takılırdı (§7 madde 51).
        ContentPreview = includeContent ? x.SourcePlainText : null,

        IsArchived = x.IsArchived,
        ArchivedReason = x.ArchivedReason,
        ArchivedAt = x.ArchivedAt,
        ArchivedByName = users.Where(u => u.Id == x.ArchivedBy).Select(u => u.Username).FirstOrDefault(),
        SourceState = x.SourceState,

        IsFeatured = x.IsFeatured,
        FeaturedUntil = x.FeaturedUntil,

        // Faz 12.15 — "bunu duyurdum mu?" liste satırında da cevaplanır.
        NotificationSentAt = x.NotificationSentAt,
        NotificationCampaignId = x.NotificationCampaignId,
        NotificationRecipientCount = x.NotificationRecipientCount,
        NotificationSentByName = users.Where(u => u.Id == x.NotificationSentBy).Select(u => u.Username).FirstOrDefault(),

        HasOverrides = x.TitleOverride != null || x.ExcerptOverride != null || x.CoverImageFileIdOverride != null,
        OverrideUpdatedAt = x.OverrideUpdatedAt,
        OverrideUpdatedByName = users.Where(u => u.Id == x.OverrideUpdatedBy).Select(u => u.Username).FirstOrDefault(),

        Categories = x.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new NewsCategoryRefDto { Id = c.Id, Name = c.Name, Slug = c.Slug })
            .ToList()
    };

    /// <summary>İfade ağacına sığmayan türetmeler — <b>bellekte, tek adımda</b>.</summary>
    public static NewsAdminDto Finish(NewsAdminDto dto, DateTime now)
    {
        dto.State = NewsStates.Of(dto.IsArchived, dto.SourceState);
        dto.FeaturedExpired = dto.IsFeatured && dto.FeaturedUntil is { } until && until <= now;
        dto.OverrideIsStale = IsStale(dto.OverrideUpdatedAt, dto.ModifiedAt);
        return dto;
    }

    /// <summary>
    /// "Override bayatladı" kuralı — <b>bellek yüzü</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Aynı kuralın bir de <b>SQL yüzü</b> var (<see cref="StaleOverride"/>): süzgeç ve
    /// sayaç sayfalamayı bozmadan çalışabilmek için kuralı sorguda tarif etmek zorunda.
    /// Bu bilinçli bir ikizlik (12.2b'de <c>PushCampaignStatus</c> ile aynı durum) ve
    /// bedelini test öder: <c>PanelNewsTests</c> "süzgeç ne getiriyorsa DTO'nun bayrağı da
    /// odur" iddiasını kurar. İkisi ayrışırsa panel "7 haber bayat" der, listede 5 tane
    /// çıkar ve <b>fark hiçbir yerde görünmez</b>.
    /// </remarks>
    public static bool IsStale(DateTime? overrideUpdatedAt, DateTime sourceModifiedAt)
        => overrideUpdatedAt is { } updated && sourceModifiedAt > updated;

    /// <summary>"Override bayatladı" kuralı — <b>SQL yüzü</b> (bkz. <see cref="IsStale"/>).</summary>
    public static readonly Expression<Func<NewsArticle, bool>> StaleOverride =
        x => x.OverrideUpdatedAt != null && x.SourceModifiedAt > x.OverrideUpdatedAt;

    /// <summary>"Elle düzenlenmiş" kuralı — SQL yüzü; bellek karşılığı <c>HasOverrides</c>.</summary>
    public static readonly Expression<Func<NewsArticle, bool>> Edited =
        x => x.TitleOverride != null || x.ExcerptOverride != null || x.CoverImageFileIdOverride != null;
}
