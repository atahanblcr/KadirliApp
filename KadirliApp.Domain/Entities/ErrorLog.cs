using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.1 — **panelden görülebilen hata kaydı.**
///
/// Bu tablo <see cref="AuditLog"/>'un tamamlayıcısıdır, kopyası değil: denetim izi
/// yöneticinin <b>başarılı</b> yazma eylemlerini tutar ("kim sildi"), bu tablo ise
/// <b>başarısız</b> olanı tutar ("vatandaş ne gördü"). 12.1 öncesinde ikincisinin
/// tek adresi Seq'ti (<c>localhost:5341</c>) — yani panelden bakan yöneticinin
/// erişemediği bir yer.
///
/// 🔑 <b>Satır başına bir HATA değil, bir PARMAK İZİ.</b> Aynı <see cref="Fingerprint"/>
/// tekrar geldiğinde yeni satır açılmaz; <see cref="OccurrenceCount"/> artar ve
/// <see cref="LastSeenAt"/> güncellenir. Bu bir iyileştirme değil zorunluluk: tek bir
/// 500 döngüsü tekilleştirme olmadan tabloyu dakikada on binlerce satırla doldurur.
///
/// ⚠️ <see cref="FirstSeenAt"/> bilerek ayrı bir kolon — <c>BaseEntity.CreatedAt</c>'e
/// güvenilmiyor, çünkü <c>SaveChanges</c> ekleme sırasında onu eziyor (11.18 dersi).
/// </summary>
public class ErrorLog : BaseEntity
{
    /// <summary>Hatanın nereden geldiği: <c>api</c> · <c>web</c> · <c>mobile</c>.</summary>
    /// <remarks>
    /// ⚠️ İstemciden gelen istekte bu değer <b>sunucuda sabitlenir</b>. İstemci "api"
    /// diyebilseydi, kendi hatasını sunucu hatası gibi gösterip listeyi zehirlerdi.
    /// </remarks>
    public string Source { get; set; } = default!;

    /// <summary><c>error</c> · <c>fatal</c>.</summary>
    public string Level { get; set; } = default!;

    /// <summary>Hata kodu: API'de zarf kodu (<c>INTERNAL_ERROR</c>), mobilde istisna tipi.</summary>
    public string Code { get; set; } = default!;

    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }

    /// <summary>İstek yolu — <b>maskelenmiş</b> (bkz. <c>SensitiveDataMasker</c>).</summary>
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }

    /// <summary>
    /// W3C trace kimliği. 🔑 Bu kolon, panel kaydını Seq'teki ayrıntılı olaya bağlayan
    /// <b>tek anahtar</b>: hata zarfının <c>meta.traceId</c>'si (görünmez sözleşme #10)
    /// ile aynı değer. Boş bırakılırsa "panelde hata var ama ayrıntısına ulaşılamıyor".
    /// </summary>
    public string? TraceId { get; set; }

    public Guid? UserId { get; set; }
    public System.Net.IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // ── Mobil cihaz bilgisi (yalnız Source=mobile) ──────────────────────────────
    public string? AppVersion { get; set; }
    public string? Platform { get; set; }
    public string? OsVersion { get; set; }

    /// <summary>
    /// Tekilleştirme anahtarı — <c>ErrorFingerprint.Compute</c> üretir. **Benzersiz indeksli.**
    /// </summary>
    public string Fingerprint { get; set; } = default!;

    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    // ── Çözüm izi ───────────────────────────────────────────────────────────────
    public bool IsResolved { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedNote { get; set; }
}
