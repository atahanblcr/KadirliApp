using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class DeathNotice : BaseEntity, ISoftDeletable
{
    private string _status = DeathStatuses.Pending;
    private Guid? _approvedBy;
    private DateTime? _approvedAt;
    private string? _rejectedReason;

    public string DeceasedName { get; set; } = default!;
    public Guid? PhotoFileId { get; set; }
    public DateTime FuneralDate { get; set; }
    public TimeSpan FuneralTime { get; set; }
    public Guid? CemeteryId { get; set; }
    public Guid? MosqueId { get; set; }
    public Guid? NeighborhoodId { get; set; }
    public string? CondolenceAddress { get; set; }
    public decimal? CondolenceLatitude { get; set; }
    public decimal? CondolenceLongitude { get; set; }
    public Guid AddedBy { get; set; }

    // Faz 12.11 — moderasyon alanları `init`: yüklenmiş bir varlığa yazılamaz, geçişler aşağıda.
    public string Status { get => _status; init => _status = value; }
    public Guid? ApprovedBy { get => _approvedBy; init => _approvedBy = value; }
    public DateTime? ApprovedAt { get => _approvedAt; init => _approvedAt = value; }
    public string? RejectedReason { get => _rejectedReason; init => _rejectedReason = value; }

    public DateTime? AutoArchiveAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Cemetery? Cemetery { get; set; }
    public Mosque? Mosque { get; set; }

    // ── Moderasyon geçişleri (12.10'da doğdu, 12.11'de varlığa taşındı) ────────
    //
    // 🔑 **Bu modülde 12.10 yalnız bir yolu KAPATMADI, iki yolu AÇTI.** Vefat modülünde
    // Reddet ve Arşivle komutları **hiç yoktu**: her ikisinin de tek yolu Düzenle formundaki
    // durum menüsüydü. Menü kaldırılıp karşılığı yazılmasaydı, "reddet" ve "arşivle" panelden
    // **tamamen kaybolurdu** — bir hatayı düzeltirken bir işlevi silmek olurdu.
    //
    // **Arşiv neden moderasyon:** `archived` kaydı public listeden düşürür
    // (`GetDeathNoticesQuery` yalnız `approved` döner), yani "içeriği yayından kaldırma"
    // kararıdır — `ArchiveDeathsJob` aynı geçişi `AutoArchiveAt` dolduğunda kendiliğinden
    // yapıyor. Elle arşivlemenin gerçek kullanımı ailenin erken kaldırma talebidir;
    // geri almanın yolu `Approve`'dur.

    /// <summary>Vefat ilanını yayına alır (arşivlenmiş bir kaydı geri getirmenin de yolu budur).</summary>
    public void Approve(Guid adminId, DateTime now)
    {
        _status = DeathStatuses.Approved;
        _approvedBy = adminId;
        _approvedAt = now;

        // 12.10: ilan/kampanyayla aynı kural — bayat red gerekçesi "Onaylandı" rozetinin
        // yanında durmamalı. Vefatta red bugüne kadar hiç yazılamadığı için bu alan boştu;
        // RejectDeathNoticeCommand ile birlikte anlamlı hâle geldi.
        _rejectedReason = null;
    }

    /// <summary>Vefat ilanını reddeder.</summary>
    /// <remarks>
    /// Vefat ilanını <b>vatandaş da gönderebiliyor</b> (<c>POST /v1/deaths</c>), yani onay
    /// kuyruğunda gerçekten reddedilmesi gereken kayıtlar oluşuyor. 12.10 öncesinde bunun
    /// tek yolu Düzenle formuydu ve o yol ne izi ne gerekçeyi tutuyordu.
    /// <para>
    /// 📌 12.11: <c>now</c> parametresi <b>düştü</b> — vefatta <c>RejectedAt</c> kolonu yok
    /// ve parametre 12.10'dan beri kullanılmıyordu (kampanyayla aynı gerekçe).
    /// </para>
    /// </remarks>
    public void Reject(string? reason)
    {
        _status = DeathStatuses.Rejected;
        _rejectedReason = reason;
        _approvedBy = null;
        _approvedAt = null;
    }

    /// <summary>Yayındaki bir vefat ilanını erken arşivler.</summary>
    /// <remarks>
    /// ⚠️ <c>AutoArchiveAt</c>'e <b>dokunulmaz</b>: o alan "ne zaman kendiliğinden
    /// arşivlenecekti" bilgisidir ve elle arşivleme onu geçersiz kılmaz — ilan sonradan
    /// tekrar onaylanırsa iş yine doğru tarihte devreye girmelidir. <c>ArchiveDeathsJob</c>
    /// yalnız <c>approved</c> satırlara dokunduğu için burada bir çakışma da doğmaz.
    /// <para>
    /// 📌 Diğer geçişlerin aksine <c>now</c> almaz — yazacağı bir zaman damgası yok.
    /// Simetri için boş bir parametre taşımak, ilk okuyana "bir yere yazılıyor olmalı"
    /// dedirtirdi.
    /// </para>
    /// </remarks>
    public void Archive()
    {
        _status = DeathStatuses.Archived;
    }
}
