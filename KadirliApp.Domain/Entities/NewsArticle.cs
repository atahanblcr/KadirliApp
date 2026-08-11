using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.12 — <b>dış kaynaktan (WordPress) alınan bir haber.</b>
///
/// 🔑 <b>Bu varlığın kolonları ÜÇ KÜMEYE ayrılır ve ayrımı DERLEYİCİ korur:</b>
/// <list type="number">
///   <item><b><c>Source*</c> — kaynağın sahibi.</b> Yalnız <see cref="ApplySourceSnapshot"/>
///         (ve durum için <see cref="MarkSourceGone"/>/<see cref="MarkSourcePublished"/>)
///         yazar. Panel bu alanlara <b>yazamaz</b>.</item>
///   <item><b><c>*Override</c> — yöneticinin sahibi.</b> Yalnız <see cref="SetOverrides"/> /
///         <see cref="ClearOverrides"/> yazar. Senkron bu alanları <b>göremez</b>.</item>
///   <item><b>Bizim alanlarımız</b> (<see cref="IsArchived"/>, <see cref="IsFeatured"/>,
///         <see cref="AnnouncementId"/>): WordPress'te karşılığı yok, geçişleri yine
///         varlığın metotlarında.</item>
/// </list>
///
/// 🔴 <b>Neden kilit bayrağı (<c>IsLocked</c>) DEĞİL de iki kolon:</b> kilit bayrağıyla
/// korunan bir kayıt kaynakta bir yazım hatası düzeltildiğinde **hiç güncellenmez** ve
/// kilit sessizce eskir; "kaynakta ne değişti?" bilgisi kaybolur; geri alma belirsizdir.
/// Asıl belirleyici olan dördüncü fark: kilit bayrağında koruma <i>senkron kodunun kilidi
/// kontrol etmesine güvenmek</i>tir — burada ise senkron o kolonu **göremez**.
/// Bu, 12.11'in dersinin birebir uygulanması: <i>korumayı taramanın erişemeyeceği yere taşı.</i>
///
/// ⚠️ Alanların <c>init</c> olması (bkz. <c>ARCHITECTURE.md</c> §7 madde 53) bilinçli:
/// nesne başlatıcıdan yazmaya (oluşturma, seed, test kurulumu) izin verir, <b>yüklenmiş
/// varlığa</b> yazmaya izin vermez. Hasarın tamamı ikincisinden gelir.
/// Bir gün <c>CS8852</c> alırsan çözüm alanı <c>set</c>'e açmak <b>değil</b>, geçişi buradaki
/// bir metoda taşımaktır.
///
/// 📌 Bu <b>genel bir "zengin domain" kararı değil</b> (§7 madde 53'ün kapsam uyarısı aynen
/// geçerli): kapatılan tek değişmez "senkron ile panel aynı kolona yazamaz".
/// </summary>
public class NewsArticle : BaseEntity
{
    // ── 1) KAYNAĞIN sahibi ──────────────────────────────────────────────────────
    private string _sourceTitle = default!;
    private string? _sourceExcerpt;
    private string _sourceContentHtml = default!;
    private string _sourcePlainText = default!;
    private string _sourceUrl = default!;
    private DateTime _sourcePublishedAt;
    private DateTime _sourceModifiedAt;
    private string _sourceChecksum = default!;
    private string _sourceState = NewsSourceStates.Published;
    private DateTime? _sourceStateChangedAt;
    private Guid? _sourceImageFileId;
    private string? _sourceImageUrl;
    private int? _sourceImageWidth;
    private int? _sourceImageHeight;
    private int _readingMinutes;
    // ⚠️ `readonly` DEĞİL: EF materyalizasyonda koleksiyonu kendi örneğiyle değiştirebilmeli.
    private List<NewsCategory> _categories = new();

    // ── 2) YÖNETİCİNİN sahibi ───────────────────────────────────────────────────
    private string? _titleOverride;
    private string? _excerptOverride;
    private Guid? _coverImageFileIdOverride;
    private DateTime? _overrideUpdatedAt;
    private Guid? _overrideUpdatedBy;

    // ── 3) BİZİM alanlarımız ────────────────────────────────────────────────────
    private bool _isArchived;
    private string? _archivedReason;
    private Guid? _archivedBy;
    private DateTime? _archivedAt;
    private bool _isFeatured;
    private DateTime? _featuredUntil;

    /// <summary>WordPress gönderi kimliği — <b>kayıt eşleştirmesinin tek anahtarı</b> (unique).</summary>
    /// <remarks>
    /// Slug ya da başlık üzerinden eşleştirme denenmedi ve denenmemeli: ikisi de kaynakta
    /// değişebilir ve değiştiği an aynı haber <b>ikinci kez</b> iner (mükerrer kayıt),
    /// üstelik hiçbir hata oluşmaz.
    /// </remarks>
    public int WpId { get; init; }

    public string SourceTitle { get => _sourceTitle; init => _sourceTitle = value; }
    public string? SourceExcerpt { get => _sourceExcerpt; init => _sourceExcerpt = value; }

    /// <summary>
    /// <b>Alım anında temizlenmiş</b> gövde HTML'i (<c>INewsHtmlSanitizer</c>).
    /// Gösterim anında değil alım anında temizlenir; yine de panelde <c>@Html.Raw</c> ile
    /// basılmaz — sanitizasyon bir kapı, tek kapı değil.
    /// </summary>
    public string SourceContentHtml { get => _sourceContentHtml; init => _sourceContentHtml = value; }

    /// <summary>Etiketsiz metin: arama ve <b>özet yedeği</b> (WP <c>excerpt</c>'i HTML parçalı gelir).</summary>
    public string SourcePlainText { get => _sourcePlainText; init => _sourcePlainText = value; }

    public string SourceUrl { get => _sourceUrl; init => _sourceUrl = value; }
    public DateTime SourcePublishedAt { get => _sourcePublishedAt; init => _sourcePublishedAt = value; }

    /// <summary>Kaynaktaki son değişiklik damgası — <b>UTC</b> (<c>modified_gmt</c>'den).</summary>
    /// <remarks>
    /// ⚠️ İleri imleç bu alandan türer ama sorguya <b>site-yerel</b> gider
    /// (<c>WordPressTimeWindow</c>). Ham UTC damgasını <c>modified_after</c>'a yazmak
    /// her koşuda <b>3 saatlik haberi sessizce atlar</b>.
    /// </remarks>
    public DateTime SourceModifiedAt { get => _sourceModifiedAt; init => _sourceModifiedAt = value; }

    /// <summary>Anlamlı alanların özeti — değişmediyse kayıt <b>hiç yazılmaz</b> (Skipped).</summary>
    public string SourceChecksum { get => _sourceChecksum; init => _sourceChecksum = value; }

    /// <summary><see cref="NewsSourceStates"/>: <c>published</c> · <c>gone</c>.</summary>
    /// <remarks>
    /// <c>gone</c> = "kaynakta artık yok". Kayıt <b>silinmez</b> (yoksa "haber neden gitti?"
    /// sorusunun cevabı hiçbir yerde olmazdı), yalnız public uçtan düşer.
    /// Bunu öğrenmenin tek yolu <c>ReconcileNewsJob</c>'dır — <c>modified_after</c> silmeyi
    /// <b>hiç bildirmez</b>.
    /// </remarks>
    public string SourceState { get => _sourceState; init => _sourceState = value; }
    public DateTime? SourceStateChangedAt { get => _sourceStateChangedAt; init => _sourceStateChangedAt = value; }

    /// <summary>Aynalanmış kapak görselinin <c>files</c> kaydı (uç <b>göreli</b> URL döner — §7 madde 9).</summary>
    public Guid? SourceImageFileId { get => _sourceImageFileId; init => _sourceImageFileId = value; }

    /// <summary>Aynalanan görselin kaynaktaki URL'i — tekilleştirme ve "değişti mi?" anahtarı.</summary>
    public string? SourceImageUrl { get => _sourceImageUrl; init => _sourceImageUrl = value; }
    public int? SourceImageWidth { get => _sourceImageWidth; init => _sourceImageWidth = value; }
    public int? SourceImageHeight { get => _sourceImageHeight; init => _sourceImageHeight = value; }

    /// <summary>Plan dışı ek (12.12): düz metinden türetilen tahmini okuma süresi (dakika).</summary>
    /// <remarks>
    /// Türetilmiş alan, elle yazılmaz — tek sahibi <c>NewsReadingTime</c>. Sunucuda üretiliyor
    /// (§7 madde 43'ün sınıfı): istemcide hesaplansaydı panel ile mobil farklı süre gösterir
    /// ve <b>kimse hata almazdı</b>.
    /// </remarks>
    public int ReadingMinutes { get => _readingMinutes; init => _readingMinutes = value; }

    /// <summary>Haberin kaynaktaki kategorileri (çoklu — bir haber birden çok kategoride olabilir).</summary>
    /// <remarks>
    /// Salt-okunur: bağ kümesi de kaynağın malıdır ve yalnız <see cref="ReplaceCategories"/>
    /// ile değişir. Doğrudan <c>Categories.Clear()</c> yazılabilseydi "iki sahip" ayrımı
    /// koleksiyonda sessizce delinirdi.
    /// </remarks>
    public IReadOnlyCollection<NewsCategory> Categories => _categories;

    // ── 2) YÖNETİCİNİN sahibi (hepsi nullable — "override yok" varsayılan) ──────

    public string? TitleOverride { get => _titleOverride; init => _titleOverride = value; }
    public string? ExcerptOverride { get => _excerptOverride; init => _excerptOverride = value; }
    public Guid? CoverImageFileIdOverride { get => _coverImageFileIdOverride; init => _coverImageFileIdOverride = value; }
    public DateTime? OverrideUpdatedAt { get => _overrideUpdatedAt; init => _overrideUpdatedAt = value; }
    public Guid? OverrideUpdatedBy { get => _overrideUpdatedBy; init => _overrideUpdatedBy = value; }

    // ── 3) BİZİM alanlarımız ────────────────────────────────────────────────────

    /// <summary>Yöneticinin haberi uygulamadan <b>geri alınabilir</b> biçimde kaldırması.</summary>
    /// <remarks>
    /// Bu modülde moderasyon <b>bilinçli olarak yoktur</b>: 5/gün haberi tek tek onaylamak
    /// yayını saatlerce geciktirirdi ve kaynak zaten bizim. Onay yerine "otomatik yayın +
    /// geri alınabilir gizleme" seçildi. ⚠️ Bu yüzden geçişlerin adı <c>Archive</c>/
    /// <c>Unarchive</c>'dır, <c>Approve</c> <b>değil</b> — <c>ModerationSingleOwnerTests</c>
    /// moderasyonlu modül kümesini <c>Approve*.cs</c> varlığından türetiyor ve o adı
    /// kullanmak beş moderasyon kuralını birden zorunlu kılardı.
    /// </remarks>
    public bool IsArchived { get => _isArchived; init => _isArchived = value; }
    public string? ArchivedReason { get => _archivedReason; init => _archivedReason = value; }
    public Guid? ArchivedBy { get => _archivedBy; init => _archivedBy = value; }
    public DateTime? ArchivedAt { get => _archivedAt; init => _archivedAt = value; }

    public bool IsFeatured { get => _isFeatured; init => _isFeatured = value; }

    /// <summary>Öne çıkarmanın bitiş anı; <c>null</c> = süresiz.</summary>
    public DateTime? FeaturedUntil { get => _featuredUntil; init => _featuredUntil = value; }

    /// <summary>Faz 12.15'te dolacak: haberin bildirimi bir <b>duyuru</b> olarak açılır.</summary>
    public Guid? AnnouncementId { get; set; }

    // ⚠️ Gezinme özellikleri de `init`: açık bir setter, ayrımın <b>üçüncü</b> kapısını
    // aralardı — `article.SourceImage = başkaDosya` yazmak kaydedildiğinde FK'yı da değiştirir,
    // yani senkronun malı olan bir kolon panel kodundan yazılabilir hâle gelirdi. Yansıma
    // testi bunu (adı "Source" ile başladığı için) kendiliğinden yakaladı.
    // EF materyalizasyonda arka alanı kullanır.
    private File? _sourceImage;
    private File? _coverImageOverrideFile;

    public File? SourceImage { get => _sourceImage; init => _sourceImage = value; }
    public File? CoverImageOverrideFile { get => _coverImageOverrideFile; init => _coverImageOverrideFile = value; }

    // ── Kaynağın yazdığı geçişler ───────────────────────────────────────────────

    /// <summary>
    /// Kaynaktan gelen anlık görüntüyü uygular. <b>Senkronun kayda dokunabildiği tek yol.</b>
    /// </summary>
    /// <remarks>
    /// Override alanlarına ve <see cref="IsArchived"/>'a <b>dokunmaz</b> — bu metodun asıl
    /// varlık sebebi budur: yöneticinin düzelttiği başlık bir sonraki senkronda ezilirse
    /// panel yalan söyler ve kimse hata almaz (§7 madde 23'ün sınıfı).
    /// <para>
    /// 🔑 <see cref="SourceState"/> burada <c>published</c>'a çekilir: kaynak akışında yeniden
    /// görünen bir haber, tanımı gereği kaynakta duruyordur. <c>ReconcileNewsJob</c>'ın ters
    /// yönü (geri gelen haber) bu sayede <b>bedava</b> ve idempotenttir.
    /// </para>
    /// </remarks>
    public void ApplySourceSnapshot(NewsArticleSnapshot snapshot, DateTime now)
    {
        _sourceTitle = snapshot.Title;
        _sourceExcerpt = snapshot.Excerpt;
        _sourceContentHtml = snapshot.ContentHtml;
        _sourcePlainText = snapshot.PlainText;
        _sourceUrl = snapshot.Url;
        _sourcePublishedAt = snapshot.PublishedAtUtc;
        _sourceModifiedAt = snapshot.ModifiedAtUtc;
        _sourceChecksum = snapshot.Checksum;
        _sourceImageFileId = snapshot.ImageFileId;
        _sourceImageUrl = snapshot.ImageUrl;
        _sourceImageWidth = snapshot.ImageWidth;
        _sourceImageHeight = snapshot.ImageHeight;
        _readingMinutes = snapshot.ReadingMinutes;

        if (_sourceState != NewsSourceStates.Published)
        {
            _sourceState = NewsSourceStates.Published;
            _sourceStateChangedAt = now;
        }
    }

    /// <summary>Kaynakta bulunamadı — kayıt silinmez, public listeden düşer.</summary>
    public void MarkSourceGone(DateTime now)
    {
        if (_sourceState == NewsSourceStates.Gone) return;
        _sourceState = NewsSourceStates.Gone;
        _sourceStateChangedAt = now;
    }

    /// <summary>Kaynağa geri döndü (idempotent — mutabakat işinin ters yönü).</summary>
    public void MarkSourcePublished(DateTime now)
    {
        if (_sourceState == NewsSourceStates.Published) return;
        _sourceState = NewsSourceStates.Published;
        _sourceStateChangedAt = now;
    }

    /// <summary>Kaynaktaki kategori bağlarını olduğu gibi yansıtır.</summary>
    public void ReplaceCategories(IEnumerable<NewsCategory> categories)
    {
        _categories.Clear();
        foreach (var category in categories)
            if (!_categories.Contains(category))
                _categories.Add(category);
    }

    // ── Yöneticinin yazdığı geçişler ────────────────────────────────────────────

    /// <summary>
    /// Yöneticinin özelleştirmelerini yazar. Boş/whitespace değer <b>override'ı kaldırır</b>
    /// (kayıt kaynağa döner) — "boş metin" diye bir override yoktur.
    /// </summary>
    public void SetOverrides(string? title, string? excerpt, Guid? coverImageFileId, Guid? adminId, DateTime now)
    {
        _titleOverride = Blank(title) ? null : title!.Trim();
        _excerptOverride = Blank(excerpt) ? null : excerpt!.Trim();
        _coverImageFileIdOverride = coverImageFileId;

        var hasAny = _titleOverride is not null || _excerptOverride is not null || _coverImageFileIdOverride is not null;
        _overrideUpdatedAt = hasAny ? now : null;
        _overrideUpdatedBy = hasAny ? adminId : null;
    }

    /// <summary>Tüm override'ları kaldırır — kayıt <b>deterministik</b> biçimde kaynağa döner.</summary>
    public void ClearOverrides() => SetOverrides(null, null, null, null, default);

    public void Archive(string? reason, Guid? adminId, DateTime now)
    {
        _isArchived = true;
        _archivedReason = Blank(reason) ? null : reason!.Trim();
        _archivedBy = adminId;
        _archivedAt = now;
    }

    /// <summary>Arşivden çıkarır ve <b>bayat gerekçeyi temizler</b> (§ checklist: onay/red izi simetrisi).</summary>
    public void Unarchive()
    {
        _isArchived = false;
        _archivedReason = null;
        _archivedBy = null;
        _archivedAt = null;
    }

    public void SetFeatured(bool featured, DateTime? until)
    {
        _isFeatured = featured;
        _featuredUntil = featured ? until : null;
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);
}

/// <summary>Kaydın kaynaktaki durumu. Metin — enum sırası değil (§7 madde 47'nin dersi).</summary>
public static class NewsSourceStates
{
    public const string Published = "published";
    public const string Gone = "gone";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Published, Gone };
}

/// <summary>
/// Kaynaktan gelen ve <see cref="NewsArticle.ApplySourceSnapshot"/>'a verilen anlık görüntü.
/// </summary>
/// <remarks>
/// Sanitizasyon, düz metin, sağlama ve görsel aynalama bu kaydı üretmeden <b>önce</b> yapılır;
/// varlık ne HTTP ne HTML bilir.
/// </remarks>
public sealed record NewsArticleSnapshot(
    string Title,
    string? Excerpt,
    string ContentHtml,
    string PlainText,
    string Url,
    DateTime PublishedAtUtc,
    DateTime ModifiedAtUtc,
    string Checksum,
    string? ImageUrl,
    Guid? ImageFileId,
    int? ImageWidth,
    int? ImageHeight,
    int ReadingMinutes);
