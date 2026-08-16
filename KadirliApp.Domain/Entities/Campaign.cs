using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class Campaign : BaseEntity, ISoftDeletable
{
    private string _status = CampaignStatuses.Pending;
    private Guid? _approvedBy;
    private DateTime? _approvedAt;
    private string? _rejectedReason;

    public Guid BusinessId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountCode { get; set; }
    public string? Terms { get; set; }
    public decimal? MinimumAmount { get; set; }
    public int? StockLimit { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? CoverImageId { get; set; }
    public int CodeViewCount { get; set; }

    // Faz 12.11 — moderasyon alanları `init`: yüklenmiş bir varlığa yazılamaz, geçişler aşağıda.
    public string Status { get => _status; init => _status = value; }
    public Guid? ApprovedBy { get => _approvedBy; init => _approvedBy = value; }
    public DateTime? ApprovedAt { get => _approvedAt; init => _approvedAt = value; }
    public string? RejectedReason { get => _rejectedReason; init => _rejectedReason = value; }

    public DateTime? DeletedAt { get; set; }

    public Business Business { get; set; } = default!;
    public File? CoverImage { get; set; }
    public ICollection<CampaignImage> Images { get; set; } = new List<CampaignImage>();
    public ICollection<CampaignCodeView> CodeViews { get; set; } = new List<CampaignCodeView>();

    // ── Moderasyon geçişleri (12.10'da doğdu, 12.11'de varlığa taşındı) ────────
    //
    // 📌 <b>"Süresi doldu" burada YOK ve bu bilinçli.</b> 12.10 öncesinde Düzenle formu
    // `expired` seçeneği sunuyordu, ama kampanyanın *yayında olup olmadığını* belirleyen şey
    // durum değil **tarih**: `GetCampaignsQuery` ve `GetCampaignByIdQuery`
    // `StartDate <= now <= EndDate` süzüyor ve hiçbir arka plan işi kampanya durumunu
    // `expired` yapmıyor (ilanlardaki `ExpireAdsJob`'ın kampanya karşılığı yok). Yani elle
    // `expired` yazmak "kampanyayı erken bitir" gibi *görünen* ama aslında onu moderasyon
    // dışı bir duruma iten bir yoldu. Kampanyayı erken bitirmenin dürüst yolu **bitiş
    // tarihini** değiştirmektir ve o alan aynı formda duruyor.
    //
    // ⚠️ `expired` yine de **okunabilir** bir durumdur (`PanelDisplay.Status` onu Türkçeye
    // çeviriyor): 12.10 öncesinde elle yazılmış satırlar duruyor ve ham basılmamalı.

    /// <summary>Kampanyayı yayına alır.</summary>
    /// <remarks>
    /// Faz 11.15b: reddedilmiş bir kampanya sonradan onaylanırsa bayat red gerekçesi
    /// kalmasın. Aynı düzeltme ilanlarda 10.14(1)'de yapılmış ama kampanyaya taşınmamıştı:
    /// panelde "Onaylandı" rozetiyle "Reddedilme sebebi: …" satırı yan yana görünüyor,
    /// işletme sahibi kampanyasının durumundan emin olamıyordu.
    /// </remarks>
    public void Approve(Guid adminId, DateTime now)
    {
        _status = CampaignStatuses.Approved;
        _approvedBy = adminId;
        _approvedAt = now;
        _rejectedReason = null;
    }

    /// <summary>Kampanyayı reddeder.</summary>
    /// <remarks>
    /// 🐛 <b>12.10'da düzeltilen simetri hatası:</b> red, onay izlerini <b>temizlemiyordu</b>.
    /// İlanlarda 10.14(1)'de "bir kayıt aynı anda hem onaylı hem reddedilmiş olamaz" diye
    /// karar verilmiş ve <c>ApprovedBy</c>/<c>ApprovedAt</c> sıfırlanmıştı; kampanyada
    /// yapılmamıştı. Sonuç sessizdi: reddedilmiş bir kampanyanın kaydında hâlâ
    /// "onaylayan yönetici" duruyordu — denetim izi doğru, <b>kaydın kendisi yalan</b>.
    /// <para>
    /// 📌 12.11: <c>now</c> parametresi <b>düştü</b> — kampanyada <c>RejectedAt</c> kolonu yok,
    /// yani yazacak bir zaman damgası yoktu ve parametre 12.10'dan beri kullanılmıyordu.
    /// Simetri için taşınan kullanılmayan parametre, ilk okuyana "bir yere yazılıyor olmalı"
    /// dedirtir (<c>EventModeration</c>'da aynı gerekçeyle hiç eklenmemişti). Red zamanı
    /// <c>audit_logs</c>'ta duruyor.
    /// </para>
    /// </remarks>
    public void Reject(string? reason)
    {
        _status = CampaignStatuses.Rejected;
        _rejectedReason = reason;
        _approvedBy = null;
        _approvedAt = null;
    }
}
