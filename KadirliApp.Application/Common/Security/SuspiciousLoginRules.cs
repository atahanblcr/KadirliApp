using KadirliApp.Application.Common.Interfaces;

namespace KadirliApp.Application.Common.Security;

/// <summary>
/// Faz 12.2 — şüphe kurallarının eşikleri. <c>appsettings</c> → <c>Security:Suspicion:*</c>.
///
/// 🔴 <b>Varsayılanlar koda yazılı ve kuralı KAPATMAZ.</b> Yapılandırma bölümü hiç yoksa
/// kurallar bu değerlerle çalışır. Bu bilinçli bir karar: "bayrakla kapalı yol = hiç test
/// edilmemiş yol" (görünmez sözleşmeler, kod dışı). Eşikleri yapılandırmadan okuyup boşsa
/// <c>0</c> kabul etseydik, <c>appsettings</c>'e dokunmayan bir ortamda güvenlik uyarıları
/// <b>hiç yanmaz</b> ve bunu kimse fark etmezdi — tam olarak bu fazın kapatmaya çalıştığı
/// sessiz hasar sınıfı.
/// </summary>
public sealed record SuspicionThresholds
{
    /// <summary>
    /// R1/R2'nin baktığı geçmiş penceresi. 15 dakika, <c>PanelLockoutPolicy.LockoutDuration</c>
    /// ile aynı — kilit süresi boyunca biriken denemeler tek bir olay sayılmalı.
    /// </summary>
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// R1 eşiği. 🔴 <b><see cref="PanelLockoutPolicy.MaxFailedAttempts"/> ile aynı olmak
    /// ZORUNDA.</b> Ayrışırsa iki taraf farklı gerçeklik görür: hesap kilitlenir ama kimseye
    /// haber gitmez (eşik yüksekse), ya da kilitlenmeyen bir hesap için uyarı yağar
    /// (eşik düşükse). Görünmez sözleşme #23'ün aynı sınıfı — testle kilitli.
    /// </summary>
    public int AccountFailureThreshold { get; init; } = PanelLockoutPolicy.MaxFailedAttempts;

    /// <summary>R2 — aynı IP'den denenmiş <b>farklı</b> hesap sayısı.</summary>
    public int DistinctAccountsFromIpThreshold { get; init; } = 3;

    /// <summary>R2 — aynı IP'den gelen toplam başarısız deneme sayısı.</summary>
    public int IpFailureThreshold { get; init; } = 20;

    /// <summary>
    /// R4 — kilit bittikten sonra "hemen" sayılan süre. Kilit 15 dakika sürüyor; saldırgan
    /// bekleyip tam bitiminde geldiyse bu, tahminin <b>tuttuğu</b> anlamına gelir.
    /// </summary>
    public TimeSpan JustAfterLockoutWindow { get; init; } = TimeSpan.FromMinutes(5);

    public static SuspicionThresholds Default { get; } = new();
}

/// <summary>
/// Kuralların karar verebilmesi için gereken geçmiş — <b>sorgusu çağırana ait</b>.
/// </summary>
/// <remarks>
/// 🔑 Kurallar bilerek <b>saf</b>: veritabanına dokunmuyorlar, yalnız sayılara bakıyorlar.
/// Böylece dört kural da container'sız birim testiyle kilitlenebiliyor
/// (<c>ARCHITECTURE.md</c> §8 "saf iş kuralı" satırı) ve eşik değişikliği bir SQL
/// sorgusunu yeniden okumadan doğrulanabiliyor.
/// </remarks>
public sealed record LoginHistorySnapshot(
    // Bu hesapta pencere içindeki başarısız deneme sayısı — DEĞERLENDİRİLEN DENEME DÂHİL.
    int RecentAccountFailures = 0,
    // Bu IP'den pencere içindeki toplam başarısız deneme (değerlendirilen dâhil).
    int RecentIpFailures = 0,
    // Bu IP'den pencere içinde denenmiş FARKLI kimlik sayısı (değerlendirilen dâhil).
    int DistinctIdentifiersFromIp = 0,
    // Bu kullanıcı bu IP'den daha önce BAŞARIYLA giriş yapmış mı.
    bool IpSeenBeforeForUser = true,
    // Kullanıcının bu denemeden önce geçerli olan kilit bitişi (varsa).
    DateTime? LockedOutUntil = null);

/// <summary>
/// Faz 12.2 — **hangi giriş denemesi şüpheli?**
///
/// Dört kural, dört farklı saldırı şeklini yakalar. Hepsi <b>gözlem</b>: bu sınıf hiçbir
/// girişi engellemez, hiçbir kilidi tetiklemez — var olan 11.18 kilidi ve 9.2 hız sınırı
/// aynen çalışmaya devam eder. Buradaki tek çıktı bir <b>işaret</b>.
///
/// ⚠️ Sıra önemli: ilk eşleşen kural kazanır. Kayıt tek bir <c>SuspicionRule</c> taşıyor,
/// çünkü panelde "bu satır neden şüpheli?" sorusunun <b>tek</b> cevabı olmalı; liste
/// tutulsaydı süzgeç de rozet de ikirciklenirdi. Sıra "en dar kapsamlıdan en genişe" değil,
/// <b>en açıklayıcıdan</b> gider: R2 (bir kampanya) R1'den (tek hesap) önce gelir, çünkü
/// kimlik bilgisi doldurma altındaki tek tek hesapların hepsini R1 de yakalar ve o zaman
/// yönetici "20 ayrı R1 uyarısı" görüp asıl olayı kaçırırdı.
/// </summary>
public static class SuspiciousLoginRules
{
    /// <summary>
    /// Denemeyi değerlendirir. Şüphe yoksa <c>null</c> döner.
    /// </summary>
    /// <param name="record">Değerlendirilen deneme.</param>
    /// <param name="history">Pencere içindeki geçmiş (değerlendirilen deneme dâhil sayılır).</param>
    /// <param name="thresholds">Eşikler; <c>null</c> ise koda yazılı varsayılanlar.</param>
    /// <param name="utcNow">Şimdi (test edilebilirlik için dışarıdan).</param>
    public static string? Evaluate(
        LoginAttemptRecord record,
        LoginHistorySnapshot history,
        SuspicionThresholds? thresholds = null,
        DateTime? utcNow = null)
    {
        var t = thresholds ?? SuspicionThresholds.Default;
        var now = utcNow ?? DateTime.UtcNow;

        if (!record.Succeeded)
        {
            // R2 — kimlik bilgisi doldurma: tek IP, çok hesap, çok deneme.
            // İki koşul BİRLİKTE aranır: yalnız "çok deneme" tek hesaba kaba kuvvettir (R1),
            // yalnız "çok hesap" ise paylaşımlı bir çıkışta (okul, işyeri) normaldir.
            if (history.DistinctIdentifiersFromIp >= t.DistinctAccountsFromIpThreshold &&
                history.RecentIpFailures >= t.IpFailureThreshold)
            {
                return SuspicionRules.CredentialStuffing;
            }

            // R1 — tek hesaba yoğun başarısız deneme. Eşik kilit eşiğiyle AYNI:
            // kilitlenen her hesap için tam olarak bir uyarı doğar.
            if (history.RecentAccountFailures >= t.AccountFailureThreshold)
                return SuspicionRules.RepeatedAccountFailure;

            return null;
        }

        // ── Buradan aşağısı BAŞARILI girişler ────────────────────────────────
        //
        // 🔑 Başarılı girişin şüpheli olabilmesi bu tasarımın can alıcı noktası: bir
        // saldırı başarısız denemelerle başlar ama **başarılı** bir girişle biter, ve
        // yalnız başarısızlıklara bakan bir sistem tam da işe yarayacağı anda kör kalır.

        // R4 — kilit biter bitmez gelen başarılı giriş. Bekleyip tekrar denemek, tahminin
        // TUTTUĞU anlamına gelir; sıradan bir kullanıcı parolasını hatırlayıp 15 dakika
        // beklemez, sıfırlatır.
        if (record.LockedOutUntil is { } lockedUntil &&
            lockedUntil <= now &&
            now - lockedUntil <= t.JustAfterLockoutWindow)
        {
            return SuspicionRules.SuccessRightAfterLockout;
        }

        // R3 — panel kullanıcısının hiç görülmemiş IP'sinden başarılı giriş.
        // ⚠️ Yalnız PANEL kullanıcıları: vatandaş mobil şebekede her gün IP değiştirir,
        // orada bu kural %100 yanlış alarm makinesi olurdu.
        if (record.IsPanelUser && !history.IpSeenBeforeForUser)
            return SuspicionRules.NewIpForPanelUser;

        return null;
    }
}
