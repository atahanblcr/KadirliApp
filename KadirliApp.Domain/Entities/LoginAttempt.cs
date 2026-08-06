using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.2 — **"kim, nereden, ne zaman girmeye çalıştı?"**
///
/// 12.2 öncesinde bu sorunun cevabı hiçbir yerde yoktu. 11.18 hesap kilidini getirdi ama
/// yalnız iki <b>sayaç</b> tutuyor (<c>User.FailedLoginAttempts</c> + <c>LockedOutUntil</c>):
/// kaç kez denendiğini biliyoruz, <b>kimin</b> denediğini ve <b>nereden</b> geldiğini
/// bilmiyoruz. Vatandaş tarafında (OTP) ise hiçbir iz yoktu.
///
/// 🔑 <b>Bu tablo <see cref="AuditLog"/>'un ve <see cref="ErrorLog"/>'un üçüncü kardeşi:</b>
/// denetim izi yöneticinin <i>başarılı yazma</i> eylemlerini, hata günlüğü <i>patlayan</i>
/// istekleri, bu tablo da <i>kimlik doğrulamanın</i> sonucunu tutar. Üçü birlikte panelin
/// "ne yapıldı / ne bozuldu / kim girmeye çalıştı" sorularını kapatır.
///
/// 🔴 <b><see cref="Identifier"/> MASKELİ saklanır</b> (<c>LoginIdentifierMasker</c>).
/// Ham telefon numarası bir güvenlik tablosunda birikirse tablo <b>kendisi</b> bir sızıntı
/// hedefine döner: kayıtlar panelde görülüyor, CSV olarak dışa aktarılıyor ve 180 gün
/// duruyor. Kimliği zaten <see cref="UserId"/> taşıyor — hesap varsa ona bakılır,
/// yoksa maskeli değer "hangi numara denendi" sorusuna yeterince cevap verir.
/// </summary>
public class LoginAttempt : BaseEntity
{
    /// <summary>Hangi kapı denendi: <c>panel</c> (kullanıcı adı + parola) · <c>mobile_otp</c>.</summary>
    public string Channel { get; set; } = default!;

    /// <summary>
    /// Denenen kullanıcı adı / telefon — <b>maskeli</b> (<c>+90500***0001</c>, <c>adm***</c>).
    /// </summary>
    public string Identifier { get; set; } = default!;

    /// <summary>Eşleşen hesap. Var olmayan bir kullanıcı denendiyse <c>null</c>.</summary>
    public Guid? UserId { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>
    /// Başarısızlığın sebebi — <c>LoginFailureReasons</c> sabitleri.
    /// Başarılı denemede <c>null</c>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu değer <b>kullanıcıya gösterilen mesajdan bağımsızdır</b>: giriş ekranı
    /// "kullanıcı adı veya şifre hatalı" der (hesap sorgulama aracına dönüşmesin diye),
    /// tablo ise <c>unknown_user</c> ile <c>bad_password</c>'ü ayırır. Ayrım burada
    /// olmasaydı "var olmayan hesaba 200 deneme" ile "tek hesaba 200 deneme" aynı
    /// görünürdü — ikisi çok farklı saldırılar.
    /// </remarks>
    public string? FailureReason { get; set; }

    public System.Net.IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>
    /// <c>SuspiciousLoginRules</c> bu denemeyi işaretledi mi. İndeksli — panelin
    /// "yalnız şüpheli" süzgeci ve uyarı işi bu kolondan okur.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>Tetikleyen kuralın adı (<c>R1</c>…<c>R4</c>) — <c>SuspicionRules</c> sabitleri.</summary>
    public string? SuspicionRule { get; set; }

    /// <summary>
    /// <c>SecurityAlertJob</c> bu kaydı bir uyarı e-postasında raporladığı an.
    ///
    /// 🔑 Ayrı kolon olmasının sebebi: iş 5 dakikada bir koşuyor ve "işlenmemiş şüpheli
    /// kayıtlar" kümesini <b>zamana göre</b> seçseydi (son 5 dakika) iş bir kez atladığında
    /// o penceredeki uyarılar <b>sessizce</b> kaybolurdu. İşaret kaydın üstünde durur;
    /// iş gecikse de kaçırılan kayıt bir sonraki turda yakalanır.
    /// </summary>
    public DateTime? AlertedAt { get; set; }
}
