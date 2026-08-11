using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.News.Dtos;

/// <summary>
/// Faz 12.13 — panelin haber satırı.
/// </summary>
/// <remarks>
/// 🔑 <b>Hem etkin değeri hem iki kaynağı birden taşır.</b> Public DTO yalnız etkin değeri
/// verir (<c>Title = TitleOverride ?? SourceTitle</c>) — vatandaşın iki kolonu bilmesine
/// gerek yok. Panelde ise ayrım <b>ekranın kendisidir</b>: yönetici "kaynakta ne yazıyor,
/// ben ne yazdım" sorusunu soramazsa override'ın ne yaptığını hiçbir zaman göremez ve
/// "kaynağa dön" butonu bir tahmine dönüşür.
/// </remarks>
public class NewsAdminDto
{
    public Guid Id { get; set; }
    public int WpId { get; set; }

    /// <summary>Etkin başlık — override varsa o (<c>NewsAdminProjection</c>, tek sahip).</summary>
    public string Title { get; set; } = default!;
    public string SourceTitle { get; set; } = default!;
    public string? TitleOverride { get; set; }

    public string? Excerpt { get; set; }
    public string? SourceExcerpt { get; set; }
    public string? ExcerptOverride { get; set; }

    /// <summary>Etkin kapak görseli — <b>göreli</b> URL (§7 madde 9).</summary>
    public string? ImageUrl { get; set; }
    public string? SourceImageUrl { get; set; }
    public Guid? CoverImageFileIdOverride { get; set; }
    public string? CoverImageOverrideUrl { get; set; }

    public string SourceUrl { get; set; } = default!;
    public DateTime PublishedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public int ReadingMinutes { get; set; }

    /// <summary>Yalnız Ayrıntı ekranında dolu — <b>düz metin</b>, HTML değil.</summary>
    /// <remarks>
    /// 🔴 Gövde panelde <c>@Html.Raw</c> ile <b>basılamaz</b> (§7 madde 33): içinde kaynağın
    /// <c>&lt;img&gt;</c>'leri var → hem CSP'nin engelleyeceği dış origin, hem depolanmış XSS
    /// yüzeyi. Bu yüzden panele HTML değil <b>düz metin</b> taşınıyor; "asıl hâli" için
    /// "Kaynakta aç" bağlantısı var.
    /// </remarks>
    public string? ContentPreview { get; set; }

    // ── Görünürlük ──────────────────────────────────────────────────────────────
    public bool IsArchived { get; set; }
    public string? ArchivedReason { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedByName { get; set; }
    public string SourceState { get; set; } = default!;

    /// <summary>Türetilmiş: <c>published</c> · <c>archived</c> · <c>gone</c> (<see cref="NewsStates"/>).</summary>
    public string State { get; set; } = default!;

    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }

    /// <summary>Öne çıkarma süresi <b>dolmuş</b> mu (rozet "süresi doldu" der).</summary>
    public bool FeaturedExpired { get; set; }

    // ── Override durumu ─────────────────────────────────────────────────────────
    public bool HasOverrides { get; set; }
    public DateTime? OverrideUpdatedAt { get; set; }
    public string? OverrideUpdatedByName { get; set; }

    /// <summary>
    /// 🔴 <b>Override bayatladı:</b> kaynak, yöneticinin düzeltmesinden <b>sonra</b> değişti.
    /// </summary>
    /// <remarks>
    /// 12.12 senkronun paneli ezmesini engelledi; bu, o kararın ikinci yüzü: artık ezmiyor,
    /// ama kaynakta başlık değişince override <b>eski metne dayanmaya devam ediyor ve bunu
    /// kimse bilmiyor</b>. Sayı panelde görünmezse yönetici sebebini hiçbir zaman anlamaz
    /// (12.3'ün "eşleşmemiş mahalle" sayacının aynı gerekçesi).
    /// </remarks>
    public bool OverrideIsStale { get; set; }

    public List<NewsCategoryRefDto> Categories { get; set; } = new();
}

/// <summary>Panel liste süzgeci.</summary>
public class QueryNewsAdminDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;

    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }

    /// <summary><see cref="NewsStates"/> değerlerinden biri; bilinmeyen değer <b>süzmez</b> (§5).</summary>
    public string? State { get; set; }

    /// <summary><c>true</c> → yalnız elle düzenlenmiş kayıtlar.</summary>
    public bool? Edited { get; set; }

    /// <summary><c>true</c> → yalnız override'ı bayatlamış kayıtlar.</summary>
    public bool? SourceUpdated { get; set; }

    public bool? Featured { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public string? Sort { get; set; }
}

/// <summary>Panelin kategori satırı — public DTO'dan farkı <b>dışlanmışları da</b> taşıması.</summary>
public class NewsCategoryAdminDto
{
    public Guid Id { get; set; }
    public int WpId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>Kaynağın bildirdiği sayı — <b>bizdeki değil</b>.</summary>
    public int SourceArticleCount { get; set; }

    /// <summary>Bizde <b>görünür</b> olan haber sayısı (dışlama/arşiv sonrası gerçek sayı).</summary>
    public int VisibleArticleCount { get; set; }

    /// <summary>Bu kategoriye bağlı toplam haber sayısı (görünmeyenler dâhil).</summary>
    public int TotalArticleCount { get; set; }

    public bool IsExcluded { get; set; }
    public bool ShowInFilterStrip { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>
    /// 🔴 <b>Anahtarı çevirmenin önizlemesi</b> — okunuşu kategorinin bugünkü durumuna bağlı:
    /// dışlanmamışsa <i>"dışlarsam kaç haber uygulamadan kalkar"</i>, dışlanmışsa
    /// <i>"dışlamayı kaldırırsam kaç haber geri gelir"</i>.
    /// </summary>
    /// <remarks>
    /// 🔑 Sayı <b>gerçek görünürlük sorgusundan</b> gelir (<c>NewsVisibility</c>), ayrı bir
    /// sayımdan değil — 12.2b'nin dersi (§7 madde 38): önizleme "342 kişi" der, gönderim 280
    /// yazar ve fark <b>hiçbir yerde görünmez</b>.
    /// ⚠️ İki yönde de <b>başka bir dışlanmış kategorisi olan</b> haberler sayılmaz: onlar
    /// zaten görünmüyor ve anahtar çevrildiğinde de görünmeyecekler. Sayılsalardı yönetici
    /// "366 haber geri gelecek" diye okur, 41'i hiç gelmezdi.
    /// </remarks>
    public int AffectedCount { get; set; }

    /// <summary>Kaynakta ilk kez bu senkronda görülmüş mü (panel <b>bildirir</b>, dışlamaz).</summary>
    public bool IsNew { get; set; }
}

/// <summary>Senkron koşusunun panel satırı.</summary>
public class NewsSyncRunDto
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Mode { get; set; } = default!;
    public string Trigger { get; set; } = default!;
    public string? TriggeredByName { get; set; }
    public string Status { get; set; } = default!;
    public string? ErrorMessage { get; set; }

    public int Fetched { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int MarkedGone { get; set; }
    public int Restored { get; set; }

    public DateTime? CursorFrom { get; set; }
    public DateTime? CursorTo { get; set; }

    /// <summary>Koşunun süresi — bitmemişse <c>null</c>.</summary>
    public TimeSpan? Duration => CompletedAt is null ? null : CompletedAt - StartedAt;
}

/// <summary>Senkronun "şu an" durumu — pano başlığı ve Dashboard kutusu <b>aynı</b> kaynaktan.</summary>
/// <remarks>
/// 🔑 İki ekran ayrı hesaplasaydı biri "taze" derken diğeri "durdu" derdi (§7 madde 35'in
/// sınıfı: ikiz eşikler tek kaynaktan gelmeli). Eşiklerin sahibi <c>NewsSyncHealth</c>.
/// </remarks>
public class NewsSyncStatusDto
{
    public DateTime? LastSuccessfulRunAt { get; set; }
    public NewsSyncFreshness Freshness { get; set; }

    /// <summary>Şu anda koşan bir iş var mı (varsa "Senkronu başlat" <b>kapalı</b> çizilir).</summary>
    public bool IsRunning { get; set; }
    public DateTime? RunningSince { get; set; }

    /// <summary>
    /// 🔴 Butonun görünürlük koşulu <b>sunucudan</b> gelir (12.2b'nin <c>CanCancel</c> dersi):
    /// görünüm kendi koşulunu yazarsa, komutun reddedeceği bir buton çizilir.
    /// </summary>
    public bool CanTrigger => !IsRunning;

    public int TotalArticles { get; set; }
    public int VisibleArticles { get; set; }
    public int ArchivedArticles { get; set; }
    public int GoneArticles { get; set; }

    /// <summary>Override'ı bayatlamış kayıt sayısı — panelin "bakılması gereken" işi.</summary>
    public int StaleOverrides { get; set; }

    /// <summary>Arşiv derinliği hedefi (<c>News:Backfill:MaxTotalPosts</c>) ve tükendi mi.</summary>
    public int TargetArticleCount { get; set; }
    public bool ArchiveCompleted { get; set; }

    public NewsSyncRunDto? LastRun { get; set; }
}
