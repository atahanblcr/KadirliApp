using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class Event : BaseEntity, ISoftDeletable
{
    private string _status = EventStatuses.Pending;

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// ☠️ <b>ÖLÜ KOLON (Faz 12.4'ten beri).</b> Okunmuyor, yazılmıyor, hiçbir DTO'da yok —
    /// konum artık <see cref="DistrictId"/> üzerinden taşınıyor.
    /// </summary>
    /// <remarks>
    /// Kolon <b>bilerek düşürülmedi</b> (<c>ARCHITECTURE.md</c> §6: tablo/kolon düşürmüyoruz),
    /// ama bir sonraki oturum onu "gerçek" sanmasın diye ölü olduğu burada yazılı: panelde
    /// formu hiç olmadı, bu yüzden veritabanındaki <b>her satırda <c>null</c></b>.
    /// Yeni kod bu alana dokunmamalıdır.
    /// </remarks>
    public string? City { get; set; }

    /// <summary>
    /// Faz 12.4: etkinliğin sözlükteki ilçesi. <c>null</c> = konumu bilinmeyen (12.4 öncesinden
    /// kalan ve geri doldurmaya girmemiş) kayıt.
    /// </summary>
    public Guid? DistrictId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Organizer { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }
    public int? AgeRestriction { get; set; }
    public int? Capacity { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TicketUrl { get; set; }
    public Guid? CoverImageId { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }

    /// <summary>
    /// 🔴 <b>TÜRETİLMİŞ ALAN (Faz 12.4'ten beri):</b> "ilçesi Kadirli mi?" —
    /// yazma anında <see cref="DistrictId"/>'den hesaplanır (<c>EventDistrictResolver</c>),
    /// panelden elle işaretlenmez.
    /// </summary>
    /// <remarks>
    /// Alan 10.x'ten beri DTO'da duruyor ve mobil onu ayrıştırıyor — <b>silmek kırıcı olurdu</b>
    /// (<c>ARCHITECTURE.md</c> §5). 12.4 öncesinde panel hiç yazmıyordu, yani her kayıtta
    /// <c>false</c>'tu ve mobilde hiçbir widget kullanmıyordu: yarım kalmış bir alan modelinin
    /// ölü yarısı. Türetmek additive: eski istemci aynı alanı görmeye devam eder, üstelik
    /// değeri artık <b>doğrudur</b>.
    /// </remarks>
    public bool IsLocal { get; set; }

    // Faz 12.11 — moderasyon alanı `init`: yüklenmiş bir varlığa yazılamaz, geçişler aşağıda.
    public string Status { get => _status; init => _status = value; }

    public Guid CreatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public EventCategory Category { get; set; } = default!;
    public District? District { get; set; }
    public File? CoverImage { get; set; }
    public ICollection<EventImage> Images { get; set; } = new List<EventImage>();

    // ── Moderasyon geçişleri (12.10'da doğdu, 12.11'de varlığa taşındı) ────────
    //
    // ⚠️ **Bu modülün geçişleri bilerek en yalın olanı.** `Event` varlığında
    // `ApprovedBy`/`ApprovedAt`/`RejectedReason` kolonları **yok**; onay izi tümüyle
    // `IAuditableCommand` üzerinden (`audit_logs`) tutuluyor. Kolon eklemek bir migration
    // demek olurdu ve 12.10/12.11'in kapsam sözü net: **şema değişikliği yok**.
    //
    // 🔑 Metotların "tek satır yazıyor" olması onları gereksiz yapmıyor — **tek sahiplik**
    // yapının kendisi: yarın etkinliğe bir onay izi kolonu eklendiğinde dokunulacak yer
    // burasıdır ve alan `init` olduğu için başka bir yere yazmak **derlenmez**.
    //
    // 📌 Diğer üç modülün aksine `adminId`/`now` almazlar. Simetri için kullanılmayan
    // parametre taşımak, ilk okuyana "bir yere yazılıyor olmalı" dedirtir ve yalan söyler —
    // etkinlikte yazılacak kolon yok. Kolon eklendiği gün imza da eklenir.

    /// <summary>Etkinliği yayına alır.</summary>
    public void Approve() => _status = EventStatuses.Approved;

    /// <summary>Etkinliği reddeder.</summary>
    public void Reject() => _status = EventStatuses.Rejected;
}
