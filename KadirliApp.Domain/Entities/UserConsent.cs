using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.16 — bir kullanıcının <b>belirli bir sürüme</b> verdiği (ya da vermediği) rıza.
/// KVKK'nın "neye, ne zaman, nasıl rıza verildi?" sorusunun cevabı.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Rıza kullanıcının GÖRDÜĞÜ sürüme yazılır</b> (§7 madde 71): istemci
/// <c>versionId</c>'yi gönderir, sunucu "o anki yayında sürüm"ü <b>kendi başına seçmez</b>.
/// Seçseydi bir yarış doğardı: kullanıcı v1'i okurken yönetici v2'yi yayınlar ve kayıt,
/// kullanıcının <b>hiç görmediği</b> bir metne rıza verdiğini söylerdi. Hiçbir hata oluşmaz;
/// kanıt <b>sessizce</b> yanlış olur.
/// </para>
/// <para>
/// ⚠️ <b><see cref="Granted"/> = <c>false</c> satırı da yazılır.</b> <i>"Sormadık"</i> ile
/// <i>"sorduk, hayır dedi"</i> farkı KVKK'da anlamlıdır ve yalnız <c>true</c> yazılsaydı bu
/// fark <b>hiçbir yerde durmazdı</b>: ikisi de "satır yok" olurdu.
/// </para>
/// <para>
/// 🔴 <b>Hesap silinince bu satır KALIR</b> (§7 madde 74) — ve bu, 12.7'nin
/// <see cref="UserIdentity"/> kararının <b>bilinçli tersidir</b>. Fark kaydın <i>cinsinde</i>:
/// sosyal kimlik <b>kanıt değeri olmayan kişisel veridir</b> → silinir; rıza kaydı
/// <b>işlemenin hukuki dayanağının kanıtıdır</b> → silinirse geçmişte yapılmış işlemenin
/// dayanağı kaybolur. Hesap silme zaten <b>anonimleştirme</b>dir (10.8: soft-delete + kişisel
/// alanların temizlenmesi), yani satır <b>anonim bir kullanıcıya</b> bağlı kalır —
/// <c>DeleteMyAccountCommand</c>'a bu tabloyu silen bir satır <b>eklenmemelidir</b>.
/// </para>
/// <para>
/// 📌 <b>Saklama süresi işi (<c>Purge…Job</c>) BİLİNÇLİ OLARAK YAZILMADI.</b> Projedeki her
/// yeni tabloya saklama süresi işi yazma refleksi (<c>CODE_REVIEW_CHECKLIST</c> §11) burada
/// <b>yanlış</b> olurdu: kanıtı süreyle silmek, kanıtı hiç tutmamakla aynı kapıya çıkar.
/// </para>
/// </remarks>
public class UserConsent : BaseEntity
{
    private bool _granted;
    private DateTime _decidedAt;
    private DateTime? _revokedAt;
    private string _source = default!;

    public Guid UserId { get; set; }

    /// <summary>🔴 Belgeye değil <b>sürüme</b> bağlıdır — bu tablonun var olma sebebi (§7 madde 71).</summary>
    public Guid DocumentVersionId { get; set; }

    /// <summary>Şu anki durum: onaylı mı? (Geri alınmış bir rızada <c>false</c>.)</summary>
    public bool Granted { get => _granted; init => _granted = value; }

    /// <summary>Kararın verildiği an (onay ya da ret). Geri alma <see cref="RevokedAt"/>'e yazılır.</summary>
    public DateTime DecidedAt { get => _decidedAt; init => _decidedAt = value; }

    /// <summary>
    /// Rıza geri alındıysa ne zaman. ⚠️ <b>Yalnız isteğe bağlı belgelerde</b> dolabilir;
    /// zorunlu bir rızayı geri almanın karşılığı <b>hesap silmedir</b> (var olan
    /// <c>DELETE /v1/users/me</c> — bu blokta ikinci bir yol açılmadı).
    /// </summary>
    public DateTime? RevokedAt { get => _revokedAt; init => _revokedAt = value; }

    /// <summary>
    /// İstek anındaki IP. Kanıtın bağlamı — "nereden" sorusunun cevabı.
    /// ⚠️ <c>ForwardedHeaders</c> 12.2'de kuruldu, yani proxy arkasında da gerçek adres gelir.
    /// </summary>
    public System.Net.IPAddress? IpAddress { get; set; }

    /// <summary>İstek anındaki tarayıcı/uygulama imzası. Kanıtın bağlamı — "nasıl" sorusunun cevabı.</summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// <see cref="Enums.ConsentSources"/> — hangi ekrandan alındı.
    /// ⚠️ <b>Sunucuda sabitlenir</b>, istemciden gelmez.
    /// </summary>
    public string Source { get => _source; init => _source = value; }

    // Navigation
    public User? User { get; set; }
    public LegalDocumentVersion? DocumentVersion { get; set; }

    /// <summary>
    /// Rızayı (yeniden) verir. Geri alınmış bir satırda <see cref="RevokedAt"/>'i temizler —
    /// kullanıcı fikrini değiştirebilir.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Bilinen sınır:</b> aynı sürüm için karar değiştirme <b>geçmişi</b> tutulmaz —
    /// satır <c>(user_id, document_version_id)</c> ile <b>benzersizdir</b> ve son durumu
    /// taşır. Kanıt için gereken *"bu kullanıcı bu metne şu an rıza veriyor mu, ne zamandan
    /// beri?"*dir; ver-al-ver döngüsünün ara adımları bilinçli olarak kapsam dışı.
    /// </remarks>
    public void Grant(DateTime now, string source)
    {
        _granted = true;
        _decidedAt = now;
        _revokedAt = null;
        _source = source;
    }

    /// <summary>
    /// Rızayı geri alır. ⚠️ Satır <b>silinmez</b>: "hiç sorulmadı" ile "verildi, sonra geri
    /// alındı" farkı kaybolurdu.
    /// </summary>
    public void Revoke(DateTime now)
    {
        _granted = false;
        _revokedAt = now;
    }

    /// <summary>"Sorduk, hayır dedi" — kayıt anında reddedilen isteğe bağlı rıza.</summary>
    public void Deny(DateTime now, string source)
    {
        _granted = false;
        _decidedAt = now;
        _revokedAt = null;
        _source = source;
    }
}
