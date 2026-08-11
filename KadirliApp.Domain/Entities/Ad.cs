using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class Ad : BaseEntity, ISoftDeletable
{
    /// <summary>
    /// Süresi dolmuş bir ilan onaylandığında verilen yeni yayın süresi.
    /// <c>CreateAdCommandHandler</c>'ın ilk yayın süresi ve <c>ExtendMyAdCommand</c>'in
    /// uzatma süresiyle aynı: 30 gün.
    /// </summary>
    public const int PublishDays = 30;

    private string _status = "pending";
    private Guid? _approvedBy;
    private DateTime? _approvedAt;
    private string? _rejectedReason;
    private DateTime? _rejectedAt;

    public Guid CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public Guid UserId { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;

    /// <inheritdoc cref="Approve"/>
    public string Status { get => _status; init => _status = value; }
    public Guid? ApprovedBy { get => _approvedBy; init => _approvedBy = value; }
    public DateTime? ApprovedAt { get => _approvedAt; init => _approvedAt = value; }
    public string? RejectedReason { get => _rejectedReason; init => _rejectedReason = value; }
    public DateTime? RejectedAt { get => _rejectedAt; init => _rejectedAt = value; }

    public DateTime ExpiresAt { get; set; }
    public int ExtensionCount { get; set; }
    public int MaxExtensions { get; set; } = 3;
    public int ViewCount { get; set; }
    public int PhoneClickCount { get; set; }
    public int WhatsappClickCount { get; set; }
    public DateTime? DeletedAt { get; set; }

    public AdCategory Category { get; set; } = default!;
    public ICollection<AdImage> Images { get; set; } = new List<AdImage>();
    public ICollection<AdPropertyValue> PropertyValues { get; set; } = new List<AdPropertyValue>();
    public ICollection<AdFavorite> Favorites { get; set; } = new List<AdFavorite>();
    public ICollection<AdExtension> Extensions { get; set; } = new List<AdExtension>();

    // ── Moderasyon geçişleri ───────────────────────────────────────────────────
    //
    // Faz 12.11: bu metotlar bir ilanın moderasyon durumunu değiştirmenin **tek yolu**.
    // 12.10'da aynı kurallar `Application/Features/Ads/AdModeration.cs`'te saf bir sınıfta
    // yaşıyordu ve tek sahiplik **yapısal bir testle** korunuyordu. Test dosya adı desenine
    // (`Update*` / `Approve*` / `Reject*` / `Archive*`) baktığı için `ExtendMyAdCommand`'i
    // hiç görmüyordu — o dosya `ad.Status = "approved"` yazıyordu ve **hiçbir test kırılmıyordu**.
    // Alanlar `init`'e çevrilince aynı satır **derlenmez** hâle geldi: kural artık bir
    // taramanın kapsamına değil, derleyiciye dayanıyor.
    //
    // ⚠️ `init` bilinçli bir seçim: nesne başlatıcıdan yazmaya (kayıt oluşturma, seed, test
    // kurulumu) izin verir, **yüklenmiş bir varlığa** yazmaya izin vermez. `private set`
    // olsaydı `new Ad { Status = "pending" }` yazan ~40 çağrı yeri fabrika metoduna
    // dönüşecekti; kazanç aynı, bedel çok daha büyük olurdu.

    /// <summary>
    /// İlanı yayına alır.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Görünmez sözleşme #25 — onay, ilanı GERÇEKTEN görünür kılmalı.</b>
    /// Canlı denetimde görülen çelişki: süresi geçmiş (<c>expired</c>) bir ilan panelden
    /// onaylandığında "İlan başarıyla onaylandı." yazıyor, ama <c>ExpiresAt</c> geçmişte
    /// kaldığı için mobil listede <b>hiç görünmüyor</b> (<c>GetAdsQueryHandler:32</c>) ve
    /// saatlik <c>ExpireAdsJob</c> durumu sessizce yeniden <c>expired</c> yapıyor.
    /// <para>
    /// Aynı sessiz hata <c>expired</c> olmayan ilanlarda da vardı: onay kuyruğunda 30 günden
    /// fazla bekleyen bir <c>pending</c> ilan onaylandığı anda süresi dolmuş oluyordu.
    /// Bu yüzden koşul <b>duruma değil TARİHE</b> bakar.
    /// </para>
    /// Karar: yayın penceresi ilanın gönderildiği an değil, <b>görünür olduğu an</b> başlar.
    /// </remarks>
    public void Approve(Guid adminId, DateTime now)
    {
        if (ExpiresAt <= now)
            ExpiresAt = now.AddDays(PublishDays);

        _status = "approved";
        _approvedBy = adminId;
        _approvedAt = now;

        // Faz 10.14(1): reddedilmiş bir ilan sonradan onaylanırsa bayat red gerekçesi kalmasın —
        // yoksa panelde "Onaylandı" rozetiyle "Reddedilme sebebi: …" satırı yan yana görünür.
        _rejectedReason = null;
        _rejectedAt = null;
    }

    /// <summary>
    /// İlanı reddeder.
    /// </summary>
    /// <remarks>
    /// Faz 10.14(1): red gerekçesi <c>RejectedReason</c>/<c>RejectedAt</c>'e yazılır
    /// (<c>MyAdDto</c> sahibe bunu döner). "Kim reddetti" izi <c>ApprovedBy</c>'ı ezerek
    /// <b>değil</b>, <c>IAuditableCommand</c> üzerinden tutulur. Bir ilan aynı anda hem
    /// onaylı hem reddedilmiş olamaz → onay izleri temizlenir.
    /// </remarks>
    public void Reject(string? reason, DateTime now)
    {
        _status = "rejected";
        _rejectedReason = reason;
        _rejectedAt = now;
        _approvedBy = null;
        _approvedAt = null;
    }

    /// <summary>
    /// İlanı <b>sahibinin</b> düzenlemesi üzerine yeniden moderasyon kuyruğuna alır.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🐛 <b>Bu geçiş 12.10'un yapısal testi tarafından bulundu.</b> Kural
    /// <c>UpdateMyAdCommandHandler</c>'ın içinde yazılıydı ve onay/red izlerini
    /// <i>elle</i> temizliyordu — yani <see cref="Approve"/> ve <see cref="Reject"/>
    /// ile aynı bilginin **üçüncü kopyası**. Kopya olduğu için de sessizce ayrışabilirdi:
    /// ilana yarın bir onay izi alanı eklendiğinde iki yer güncellenip üçüncüsü unutulurdu
    /// ve kayıt "pending ama onaylayanı dolu" hâline düşerdi.
    /// </para>
    /// <para>
    /// ⚠️ <c>ExpiresAt</c>'e <b>dokunulmaz</b> — süre işi <see cref="Extend"/>'in.
    /// Düzenleme bir uzatma yolu olsaydı vatandaş ilanını başlığına nokta ekleyerek
    /// sonsuza kadar yayında tutabilirdi.
    /// </para>
    /// <para>
    /// 📌 Bu, moderasyon durumunu yazan ama <i>yönetici kararı olmayan</i> iki geçişten biri
    /// (diğeri <see cref="Extend"/>) — yönü de tek: her zaman <c>pending</c>'e <b>iner</b>.
    /// Reddedilmiş bir ilanı düzeltip yeniden gönderme yolu da budur.
    /// </para>
    /// </remarks>
    public void Resubmit()
    {
        _status = "pending";
        _approvedBy = null;
        _approvedAt = null;
        _rejectedReason = null;
        _rejectedAt = null;
    }

    /// <summary>
    /// İlanın yayın süresini uzatır; süresi dolmuş bir ilanı <b>yeniden yayına alır</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Faz 12.11'de bu metot buraya taşındı ve sebebi bir denetim bulgusudur.</b>
    /// Geçiş <c>ExtendMyAdCommandHandler</c>'ın içinde ham <c>ad.Status = "approved"</c>
    /// olarak yazılıydı: moderasyon durumunu yazan <b>beşinci</b> yol ve 12.10'un yapısal
    /// testinin <b>göremediği</b> yol (test yalnız <c>Update*</c>/<c>Approve*</c>/
    /// <c>Reject*</c>/<c>Archive*</c> dosyalarını tarıyordu, <c>ExtendMyAd*</c> hiçbirine
    /// uymuyordu). Kayıt bozulmuyordu — ama koruma <i>tesadüfen</i> çalışıyordu, kurala
    /// dayanarak değil.
    /// </para>
    /// <para>
    /// ⚠️ <c>ApprovedBy</c>/<c>ApprovedAt</c>'e <b>dokunulmaz ve bu bilinçli.</b> Uzatma bir
    /// moderasyon kararı <i>değildir</i>: içerik zaten onaydan geçmişti, yalnız süresi doldu.
    /// Yeni bir onay izi yazmak "bu ilanı falanca yönetici onayladı" diye <b>yalan söylerdi</b>;
    /// izi silmek ise gerçek onay bilgisini kaybettirirdi. Bir ilan <c>expired</c>'a yalnız
    /// <c>ExpireAdsJob</c> üzerinden (<c>approved</c> iken) düşebildiği için onay izi zaten
    /// dolu gelir.
    /// </para>
    /// <para>
    /// 📌 Süresi geçmişse bugünden, geçmemişse mevcut bitişten itibaren uzar — erken uzatan
    /// gün kaybetmez. <c>MaxExtensions</c> denetimi ve sahiplik kontrolü <b>komutta</b> kalır:
    /// biri yetkilendirme, diğeri kota — ikisi de kaydın iç tutarlılığı değil.
    /// </para>
    /// </remarks>
    public void Extend(int days, DateTime now)
    {
        var baseDate = ExpiresAt > now ? ExpiresAt : now;
        ExpiresAt = baseDate.AddDays(days);
        ExtensionCount++;

        if (_status == "expired")
            _status = "approved";
    }
}
