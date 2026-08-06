namespace KadirliApp.Application.Common.Interfaces;

/// <summary>Giriş denemesinin geldiği kapı.</summary>
public static class LoginChannels
{
    /// <summary>Yönetim paneli — kullanıcı adı + parola.</summary>
    public const string Panel = "panel";

    /// <summary>Mobil uygulama — telefon + OTP.</summary>
    public const string MobileOtp = "mobile_otp";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Panel, MobileOtp };
}

/// <summary>
/// Başarısızlık sebepleri. ⚠️ Bunlar <b>ekrana basılmaz</b> — panelde Türkçe karşılıkları
/// kullanılır (<c>PanelDisplay.LoginFailureReason</c>, Değişmez Kural #6).
/// </summary>
public static class LoginFailureReasons
{
    /// <summary>Hesap var, parola yanlış.</summary>
    public const string BadPassword = "bad_password";

    /// <summary>OTP geçersiz veya süresi dolmuş.</summary>
    public const string BadOtp = "bad_otp";

    /// <summary>Böyle bir kullanıcı yok (ya da parolası olmayan bir vatandaş hesabı).</summary>
    public const string UnknownUser = "unknown_user";

    /// <summary>Hesap 11.18 kilidinde — parola doğru olsa bile reddedilir.</summary>
    public const string LockedOut = "locked_out";

    /// <summary>OTP hatalı deneme tavanı aşıldı, telefon 5 dakika bloklu.</summary>
    public const string OtpBlocked = "otp_blocked";

    public const string Banned = "banned";
    public const string Inactive = "inactive";

    /// <summary>Kimlik doğru ama rol panele girmeye yetmiyor (vatandaş hesabı).</summary>
    public const string RoleDenied = "role_denied";

    /// <summary>
    /// IP hız sınırı denemeyi <b>controller'a hiç ulaştırmadan</b> reddetti (9.2, <c>panel-login</c>).
    /// </summary>
    /// <remarks>
    /// 🐛 <b>Faz 12.2'nin canlı doğrulamasında bulundu.</b> Hız sınırı ara katmanı
    /// controller'dan ÖNCE çalışıyor; yani dakikada 5'i aşan denemeler <c>AccountController</c>'a
    /// hiç girmiyordu ve <c>login_attempts</c>'e <b>tek satır bile</b> düşmüyordu. Sonuç tam da
    /// bu fazın savaştığı sınıftı: saldırgan dakikada 500 deneme yapıyor, panel "5 deneme"
    /// gösteriyor ve <b>iki taraf farklı gerçeklik görüyor</b> — üstelik kısma ne kadar iyi
    /// çalışırsa tablo o kadar çok yalan söylüyordu.
    /// </remarks>
    public const string RateLimited = "rate_limited";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal)
        {
            BadPassword, BadOtp, UnknownUser, LockedOut, OtpBlocked, Banned, Inactive, RoleDenied,
            RateLimited
        };
}

/// <summary>Şüphe kurallarının adları. <c>PanelDisplay.SuspicionRule</c> Türkçeleştirir.</summary>
public static class SuspicionRules
{
    /// <summary>Aynı hesapta kısa sürede yoğun başarısız deneme (kilidi tetikleyen eşik).</summary>
    public const string RepeatedAccountFailure = "R1";

    /// <summary>Aynı IP'den çok sayıda <b>farklı</b> hesaba başarısız deneme (kimlik bilgisi doldurma).</summary>
    public const string CredentialStuffing = "R2";

    /// <summary>Panel kullanıcısının daha önce hiç görülmemiş bir IP'sinden başarılı girişi.</summary>
    public const string NewIpForPanelUser = "R3";

    /// <summary>Kilit biter bitmez gelen başarılı giriş — kaba kuvvetin tuttuğu senaryo.</summary>
    public const string SuccessRightAfterLockout = "R4";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal)
        {
            RepeatedAccountFailure, CredentialStuffing, NewIpForPanelUser, SuccessRightAfterLockout
        };
}

/// <summary>
/// Çağıranın bildiği her şey. IP ve tarayıcı bilgisi <b>bilinçli olarak yok</b>:
/// onları gerçekleme <c>IHttpContextAccessor</c>'dan kendisi okur.
/// </summary>
/// <remarks>
/// 🔑 Neden: aksi hâlde her çağıran (panel controller'ı, OTP handler'ı, ileride sosyal giriş)
/// IP'yi kendi okur ve <c>ForwardedHeaders</c>'ı doğru yorumlama sorumluluğu <b>üç yere</b>
/// dağılırdı. Biri <c>X-Forwarded-For</c>'u elle ayrıştırmaya kalksa güvenlik kaydı
/// zehirlenebilirdi. Tek okuma noktası = tek doğru cevap.
/// </remarks>
public sealed record LoginAttemptRecord(
    string Channel,
    string RawIdentifier,
    Guid? UserId,
    bool Succeeded,
    string? FailureReason = null,
    // Hesap panele girebilen bir rolde mi — R3 yalnız onlar için yanar.
    bool IsPanelUser = false,
    // Bu denemenin HEMEN ÖNCESİNDE geçerli olan kilit bitiş anı (R4 için).
    DateTime? LockedOutUntil = null);

/// <summary>
/// Faz 12.2 — giriş denemesini kaydeder ve şüphe kurallarını uygular.
///
/// ⚠️ <b>Sözleşme: bu metot ASLA fırlatmaz.</b> Kimlik doğrulama, kendisini gözleyen
/// katman yüzünden başarısız olamaz — bir kullanıcının panele girememesi, o girişin
/// kaydedilememesinden çok daha ağır bir arızadır. Kayıt hatası yutulur ve
/// <c>ILogger</c>'a düşer (12.1'in <c>IErrorLogSink</c> sözleşmesiyle aynı aile).
///
/// ⚠️ Hata günlüğünden farklı olarak yazma <b>senkron</b>: bu satır bir güvenlik kanıtı,
/// kuyruk taşmasında düşürülebilecek bir telemetri değil. Hacim de karşılaştırılamaz —
/// 500 döngüsü dakikada on binlerce olay üretir, giriş denemesi üretmez.
/// </summary>
public interface ILoginAttemptRecorder
{
    Task RecordAsync(LoginAttemptRecord record, CancellationToken cancellationToken = default);
}
