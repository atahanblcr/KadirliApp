using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class User : BaseEntity, ISoftDeletable
{
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Username { get; set; }
    public int? Age { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public Guid? PrimaryNeighborhoodId { get; set; }
    public string? LocationType { get; set; }
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    public string? FcmToken { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? UsernameLastChangedAt { get; set; }
    public DateTime? NeighborhoodLastChangedAt { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Faz 11.18: parola **başkası tarafından** belirlendiyse (seed, personel oluşturma,
    /// parola sıfırlama) true olur; sahibi kendi parolasını seçene kadar panelde
    /// hiçbir sayfa açılmaz. <c>ChangeMyPasswordCommand</c> temizler.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// Faz 11.18: parolanın en son ne zaman değiştiği. <c>OnValidatePrincipal</c> bunu
    /// çerezin düzenlenme anıyla karşılaştırır — parola değişimi **açık oturumları düşürür**
    /// (öncesinde çalınmış bir çerez, parola değiştirilse bile 8 saat yaşamaya devam ederdi).
    /// </summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>Faz 11.18: art arda hatalı giriş sayacı; başarılı girişte sıfırlanır.</summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>Faz 11.18: bu ana kadar giriş reddedilir (hesap kilidi). Doğru parola bile kabul edilmez.</summary>
    public DateTime? LockedOutUntil { get; set; }

    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public Guid? BannedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Neighborhood? PrimaryNeighborhood { get; set; }
    public ICollection<UserNeighborhood> Neighborhoods { get; set; } = new List<UserNeighborhood>();
    public ICollection<AdminPermission> AdminPermissions { get; set; } = new List<AdminPermission>();

    /// <summary>Faz 12.7 — bağlı sosyal hesaplar (Google/Apple). Boş liste = yalnız telefon+OTP.</summary>
    public ICollection<UserIdentity> Identities { get; set; } = new List<UserIdentity>();
}

public class NotificationPreferences
{
    public bool Announcements { get; set; } = true;
    public bool Deaths { get; set; } = true;
    public bool Pharmacy { get; set; } = true;
    public bool Events { get; set; } = true;
    public bool Ads { get; set; } = false;
    public bool Campaigns { get; set; } = false;

    /// <summary>
    /// Faz 12.15b — <b>haber bildirimleri.</b> Varsayılan <c>true</c>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Bu eksen 12.15'te AÇILMADI ve eksikliği sessiz bir hataydı.</b> Gönderim
    /// <c>NotificationDispatcher</c>'dan geçiyor, o da <b>her kaynağı</b>
    /// <see cref="Announcements"/>'a bağlıyordu. İki yönlü hasar veriyordu:
    /// <list type="number">
    ///   <item>"Duyurular"ı kapatan kullanıcı <b>haber bildirimlerini de</b> kaybediyordu ve
    ///         ayar ekranı bunu hiçbir yerde söylemiyordu.</item>
    ///   <item>Daha kötüsü tersi: haber push'u istemeyen kullanıcının <b>tek çıkışı</b>
    ///         "Duyurular"ı kapatmaktı — o da §7 madde 41 gereği <b>kesinti bildirimini</b>
    ///         öldürüyordu. Yani 12.15'in "otomatik değil, elle gönderim" gerekçesinin
    ///         (<i>bildirim yorgunluğu → kullanıcı hepsini kapatır → kesintiyi de almaz</i>)
    ///         tam olarak korktuğu senaryo <b>tek anahtarla</b> ulaşılabilir durumdaydı.</item>
    /// </list>
    ///
    /// ⚠️ <b>Varsayılan <c>true</c> ve bu ölçüye dayanıyor:</b> tercihler JSON kolonda
    /// (<c>OwnsOne(...).ToJson()</c>) saklanıyor ve <b>mevcut satırlarda bu anahtar YOK</b>.
    /// Alanın yokluğu, alan eklenmeden önceki davranışı vermeli (checklist §5): 12.15'te
    /// haber bildirimi <see cref="Announcements"/> açık olan herkese gidiyordu, yani
    /// "hiç seçim yapmamış kullanıcı alır". <c>false</c> olsaydı bugün bildirim alan
    /// <b>bütün kullanıcılar</b> bir migration olmadan sessizce susardı.
    /// 🔬 Anahtarsız JSON'un gerçekten <c>true</c> materyalize olduğu <b>ölçüldü</b>
    /// (<c>NotificationPreferenceTests.MissingJsonKey_DefaultsToOptedIn</c>) — varsayılan
    /// başlatıcının EF'in JSON materyalizasyonunda çalıştığı bir <i>varsayım</i> olarak
    /// bırakılamazdı: yanılsaydık hasarın hiçbir belirtisi olmazdı.
    /// </remarks>
    public bool News { get; set; } = true;
}
