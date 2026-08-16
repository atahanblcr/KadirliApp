namespace KadirliApp.Domain.Enums;

/// <summary>
/// Faz 12.19c — moderasyon durumlarının <b>ham değerleri</b>. Türkçeleştirme panelde
/// (<c>PanelDisplay.Status</c>), mobilde ise istemcinin kendi sözlüğünde.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Neden <c>enum</c> DEĞİL <c>const string</c>.</b> 14 Ağu 2026 denetimi *"durum
/// alanları string, enum kullanılmalı"* dedi; öneri bu projede <b>kırıcı</b>. Değer
/// veritabanında <c>varchar</c> ve <b>DTO'da metin olarak mobile çıkıyor</b> — tipini
/// değiştirmek §5'in additive kontrat kuralını kırar, yani mağazadaki eski sürümleri.
/// Doğru çözüm projede <b>zaten vardı</b>: <c>PushCampaignStatuses</c> (12.2b) ve
/// <c>TransportVehicleTypes</c> (12.5) aynı deseni kullanıyordu; proje deseni bir modülde
/// bulup dört moderasyonlu modüle uygulamamıştı.
/// </para>
/// <para>
/// 🔑 <b>Kapattığı somut delik:</b> 12.11 moderasyon alanlarını <c>init</c> yapıp geçişleri
/// varlığa taşıdı — yani <i>kimin</i> yazdığını derleyiciye bağladı. Ama <i>ne</i>
/// yazıldığını bağlamadı: <c>ad.Approve()</c> içinde <c>_status = "apprved"</c> yazmak
/// 12.19c'ye kadar <b>derleniyordu</b>. Sonucu sessiz olurdu — kayıt yazılır, panel
/// "Bilinmeyen durum" rozeti çizer (11.15c sayesinde), mobil listede <b>hiç görünmez</b>
/// (§3: public uç yalnız <c>approved</c> döner) ve hiçbir hata oluşmaz.
/// </para>
/// <para>
/// ⚠️ <b>Bu bir yeniden adlandırma DEĞİL.</b> Kolonda duran metinler birebir aynı kaldı;
/// yapılan tek şey literalin <b>tek sahibe</b> taşınmasıdır.
/// </para>
/// <para>
/// 📌 <b>Kapsam bilinçli olarak dar:</b> duyurunun <c>draft</c>/<c>active</c>/<c>scheduled</c>
/// değerleri ve haber durumları burada <b>yok</b> — duyuruda moderasyon yoktur (kendi yayın
/// yaşam döngüsüdür) ve haber durumu türetilir (§7 madde 58). Ölçüt
/// <c>ModerationSingleOwnerTests.ModeratedModules</c> ile aynı: modülün bir
/// <c>Approve</c> komutu var mı.
/// </para>
/// </remarks>
public static class ModerationStatuses
{
    /// <summary>Onay kuyruğunda bekliyor. Dört modülde de kaydın doğduğu durum.</summary>
    public const string Pending = "pending";

    /// <summary>Yönetici onayladı — public uçların döndürdüğü <b>tek</b> durum (§3).</summary>
    public const string Approved = "approved";

    /// <summary>Yönetici reddetti.</summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// Dört moderasyonlu modülün <b>ortak</b> sözlüğü. Modüle özel değerler
    /// (<c>expired</c>, <c>archived</c>) ilgili modülün kendi sınıfındadır.
    /// </summary>
    public static readonly IReadOnlySet<string> Core =
        new HashSet<string>(StringComparer.Ordinal) { Pending, Approved, Rejected };
}

/// <summary>İlan durumları (<c>ads.status</c>).</summary>
/// <remarks>
/// ⚠️ <c>expired</c> bir <b>moderasyon kararı değil</b>: <c>ExpireAdsJob</c> saatlik olarak
/// süresi dolmuş <c>approved</c> ilanları oraya düşürür ve <c>Ad.Extend</c> geri alır.
/// Onay/red ile aynı kolonda yaşıyor olması bir tarih kararıdır, bir eşdeğerlik değil.
/// </remarks>
public static class AdStatuses
{
    public const string Pending = ModerationStatuses.Pending;
    public const string Approved = ModerationStatuses.Approved;
    public const string Rejected = ModerationStatuses.Rejected;

    /// <summary>Yayın süresi doldu — kayıt duruyor, mobil listede görünmüyor.</summary>
    public const string Expired = "expired";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Pending, Approved, Rejected, Expired };
}

/// <summary>Kampanya durumları (<c>campaigns.status</c>).</summary>
public static class CampaignStatuses
{
    public const string Pending = ModerationStatuses.Pending;
    public const string Approved = ModerationStatuses.Approved;
    public const string Rejected = ModerationStatuses.Rejected;

    /// <summary>Kampanyanın bitiş tarihi geçti.</summary>
    public const string Expired = "expired";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Pending, Approved, Rejected, Expired };
}

/// <summary>Vefat ilanı durumları (<c>death_notices.status</c>).</summary>
/// <remarks>
/// ⚠️ <c>archived</c> hem <c>ArchiveDeathsJob</c>'un (<c>AutoArchiveAt</c> dolunca) hem de
/// <c>ArchiveDeathNoticeCommand</c>'in (ailenin erken kaldırma talebi) yazdığı durum;
/// geri alınması <c>ApproveDeathNoticeCommand</c> ile olur — ikinci bir "arşivden çıkar"
/// komutu aynı geçişe ikinci bir sahip vermek olurdu (12.10 kararı).
/// </remarks>
public static class DeathStatuses
{
    public const string Pending = ModerationStatuses.Pending;
    public const string Approved = ModerationStatuses.Approved;
    public const string Rejected = ModerationStatuses.Rejected;

    /// <summary>Cenaze geçti (ya da aile istedi) — public listeden düştü.</summary>
    public const string Archived = "archived";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Pending, Approved, Rejected, Archived };
}

/// <summary>Etkinlik durumları (<c>events.status</c>).</summary>
/// <remarks>
/// 📌 Etkinlikte dördüncü bir durum <b>yok</b>: geçmiş etkinlik <c>expired</c>'a düşmez,
/// <c>EventDate</c>'e bakılarak süzülür. Silinen dört enum'dan <c>EventStatus</c> bir
/// <c>Canceled</c> değeri taşıyordu; o değer <b>hiçbir zaman hiçbir kayda yazılmadı</b>
/// (enum zaten ölüydü) ve burada bilinçli olarak <b>diriltilmedi</b> — var olmayan bir
/// durumu sözlüğe yazmak, panelin ve mobilin onu ele alması gerektiğini ima ederdi.
/// </remarks>
public static class EventStatuses
{
    public const string Pending = ModerationStatuses.Pending;
    public const string Approved = ModerationStatuses.Approved;
    public const string Rejected = ModerationStatuses.Rejected;

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Pending, Approved, Rejected };
}
