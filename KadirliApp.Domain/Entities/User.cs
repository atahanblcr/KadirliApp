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
}

public class NotificationPreferences
{
    public bool Announcements { get; set; } = true;
    public bool Deaths { get; set; } = true;
    public bool Pharmacy { get; set; } = true;
    public bool Events { get; set; } = true;
    public bool Ads { get; set; } = false;
    public bool Campaigns { get; set; } = false;
}
