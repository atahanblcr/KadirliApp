using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Security;

/// <summary>
/// Faz 12.2 — **şüphe kurallarının dördü de burada kilitli.**
///
/// Kurallar bilerek <b>saf</b> (veritabanına dokunmuyorlar) — bu yüzden container'sız,
/// milisaniyeler içinde koşuyorlar ve eşik değişikliği bir SQL sorgusunu yeniden okumadan
/// doğrulanabiliyor (<c>ARCHITECTURE.md</c> §8 "saf iş kuralı" satırı).
///
/// 🔴 Bu dosyanın en önemli testi <see cref="R1Threshold_MatchesThePanelLockoutThreshold"/>:
/// kural eşiği ile kilit eşiği ayrışırsa hesap kilitlenir ama <b>kimseye haber gitmez</b> —
/// ve bu hiçbir hata vermeden olur.
/// </summary>
public class SuspiciousLoginRulesTests
{
    private static readonly DateTime Now = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    private static LoginAttemptRecord Failed(string? reason = null, string identifier = "adm***") =>
        new(LoginChannels.Panel, identifier, Guid.NewGuid(), Succeeded: false, FailureReason: reason ?? LoginFailureReasons.BadPassword);

    private static LoginAttemptRecord Succeeded(bool isPanelUser = true, DateTime? lockedOutUntil = null) =>
        new(LoginChannels.Panel, "adm***", Guid.NewGuid(), Succeeded: true,
            IsPanelUser: isPanelUser, LockedOutUntil: lockedOutUntil);

    // ────────────────────────────────────────────────────────────────────────
    // Eşiklerin sözleşmesi
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <b>Görünmez sözleşme:</b> R1 eşiği <c>PanelLockoutPolicy.MaxFailedAttempts</c> ile
    /// AYNI olmak zorunda. Yüksek olsaydı kilitlenen hesap için uyarı hiç doğmazdı
    /// (hesap kilitli olduğu için eşiğe ulaşacak deneme de gelemez); düşük olsaydı
    /// kilitlenmeyen hesaplar için uyarı yağardı. İki taraf farklı gerçeklik görür.
    /// </summary>
    [Fact]
    public void R1Threshold_MatchesThePanelLockoutThreshold()
    {
        SuspicionThresholds.Default.AccountFailureThreshold
            .Should().Be(PanelLockoutPolicy.MaxFailedAttempts,
                "kilidi tetikleyen eşikle uyarıyı tetikleyen eşik ayrışırsa hesap kilitlenir " +
                "ama kimseye haber gitmez (görünmez sözleşme #23 sınıfı)");
    }

    /// <summary>
    /// 🐛 <b>Bu test, "kuralı bilerek boz" denetiminde açılan bir boşluğu kapatıyor.</b>
    /// </summary>
    /// <remarks>
    /// Üstteki test yalnız <b>koddaki varsayılanı</b> kilitliyordu. Eşiği <c>appsettings</c>
    /// ezebiliyor ve <c>LoginAttemptRecorder</c> yapılandırmayı okuyor — yani biri
    /// <c>Security:Suspicion:AccountFailureThreshold</c>'ı 7 yapsa kilit yine 5'te kapanır,
    /// uyarı ise <b>hiç doğmaz</b> (hesap kilitli olduğu için 6. ve 7. deneme zaten gelemez)
    /// ve <b>tek bir test bile kırılmaz</b>. Denemede tam olarak bu görüldü: eşik ayrıştırıldığında
    /// saf kural testi kırmızıya döndü ama uçtan uca panel testi <b>yeşil kaldı</b>, çünkü
    /// yapılandırma hâlâ 5 diyordu.
    ///
    /// 🔑 Deseni <c>ProductionReadinessGuardTests.Commit_edilmis_sirlar_appsettings_ile_AYNI_olmali</c>
    /// zaten kurmuştu: koddaki sabit ile dosyadaki değer <b>eşitlenmezse</b> koruma sessizce kaybolur.
    ///
    /// ⚠️ İki dosya birden denetleniyor: giriş iki ayrı süreçte kaydediliyor (Api → OTP,
    /// Web → panel). Yalnız biri denetlenseydi diğerinin ayrışması görünmez kalırdı.
    /// </remarks>
    [Theory]
    [InlineData("KadirliApp.Api")]
    [InlineData("KadirliApp.Web")]
    public void ConfiguredThreshold_MatchesTheLockoutThreshold(string project)
    {
        var path = Path.Combine(SolutionRoot(), project, "appsettings.json");
        var cfg = new ConfigurationBuilder().AddJsonFile(path).Build();

        var configured = cfg["Security:Suspicion:AccountFailureThreshold"];
        configured.Should().NotBeNullOrWhiteSpace(
            "{0}/appsettings.json eşiği yazmalı — yoksa iki süreç farklı değer kullanabilir", project);

        int.Parse(configured!).Should().Be(PanelLockoutPolicy.MaxFailedAttempts,
            "{0}/appsettings.json'daki eşik kilit eşiğinden ayrılmış: hesap kilitlenir " +
            "ama uyarı doğmaz ve hiçbir yerde belirti olmaz", project);
    }

    /// <summary>Test derlemesinden çözüm köküne çıkar (appsettings.json'u okumak için).</summary>
    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("çözüm kökü bulunamazsa test hiçbir şey denetlemez");
        return dir!.FullName;
    }

    /// <summary>
    /// Varsayılanlar koda yazılı ve kuralı KAPATMIYOR. Sıfır olsaydı yapılandırma bölümü
    /// olmayan bir ortamda her deneme şüpheli sayılırdı; negatif/boş olsaydı hiçbiri.
    /// </summary>
    [Fact]
    public void Defaults_AreUsableWithoutAnyConfiguration()
    {
        var d = SuspicionThresholds.Default;

        d.Window.Should().BeGreaterThan(TimeSpan.Zero);
        d.AccountFailureThreshold.Should().BeGreaterThan(1);
        d.DistinctAccountsFromIpThreshold.Should().BeGreaterThan(1);
        d.IpFailureThreshold.Should().BeGreaterThan(d.DistinctAccountsFromIpThreshold);
    }

    // ────────────────────────────────────────────────────────────────────────
    // R1 — aynı hesaba yoğun başarısız deneme
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void R1_FiresExactlyAtTheThreshold()
    {
        var atThreshold = new LoginHistorySnapshot(
            RecentAccountFailures: SuspicionThresholds.Default.AccountFailureThreshold);

        SuspiciousLoginRules.Evaluate(Failed(), atThreshold, utcNow: Now)
            .Should().Be(SuspicionRules.RepeatedAccountFailure);
    }

    [Fact]
    public void R1_DoesNotFireOneBelowTheThreshold()
    {
        var below = new LoginHistorySnapshot(
            RecentAccountFailures: SuspicionThresholds.Default.AccountFailureThreshold - 1);

        SuspiciousLoginRules.Evaluate(Failed(), below, utcNow: Now).Should().BeNull();
    }

    /// <summary>Başarılı giriş R1'i tetiklemez — kural başarısızlıkları sayıyor.</summary>
    [Fact]
    public void R1_NeverFiresForASuccessfulLogin()
    {
        var many = new LoginHistorySnapshot(RecentAccountFailures: 50, IpSeenBeforeForUser: true);

        SuspiciousLoginRules.Evaluate(Succeeded(), many, utcNow: Now).Should().BeNull();
    }

    // ────────────────────────────────────────────────────────────────────────
    // R2 — kimlik bilgisi doldurma (tek IP, çok hesap)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void R2_FiresWhenManyAccountsAndManyFailuresComeFromOneIp()
    {
        var t = SuspicionThresholds.Default;
        var stuffing = new LoginHistorySnapshot(
            RecentAccountFailures: 1,
            RecentIpFailures: t.IpFailureThreshold,
            DistinctIdentifiersFromIp: t.DistinctAccountsFromIpThreshold);

        SuspiciousLoginRules.Evaluate(Failed(), stuffing, utcNow: Now)
            .Should().Be(SuspicionRules.CredentialStuffing);
    }

    /// <summary>
    /// 🔑 İki koşul <b>birlikte</b> aranır. Yalnız "çok deneme" tek hesaba kaba kuvvettir
    /// (R1'in işi); yalnız "çok hesap" paylaşımlı bir çıkışta (okul, işyeri, kurum ağı)
    /// tamamen normaldir. Tek koşulla yanan bir R2, ilk haftada susturulurdu.
    /// </summary>
    [Fact]
    public void R2_DoesNotFireForManyAccountsWithFewFailures()
    {
        var sharedOffice = new LoginHistorySnapshot(
            RecentAccountFailures: 1,
            RecentIpFailures: 4,
            DistinctIdentifiersFromIp: 4);

        SuspiciousLoginRules.Evaluate(Failed(), sharedOffice, utcNow: Now).Should().BeNull();
    }

    [Fact]
    public void R2_DoesNotFireForManyFailuresAgainstASingleAccount()
    {
        var t = SuspicionThresholds.Default;
        var singleTarget = new LoginHistorySnapshot(
            RecentAccountFailures: 2,
            RecentIpFailures: t.IpFailureThreshold,
            DistinctIdentifiersFromIp: 1);

        SuspiciousLoginRules.Evaluate(Failed(), singleTarget, utcNow: Now)
            .Should().BeNull("tek hesaba yoğun deneme R1'in işi; R2 bir KAMPANYAYI tarif eder");
    }

    /// <summary>
    /// 🔑 Sıra bilinçli: bir kimlik bilgisi doldurma saldırısında altındaki tek tek
    /// hesapları R1 de yakalar. R1 önce gelseydi yönetici "20 ayrı R1 uyarısı" görür ve
    /// asıl olayı (tek IP'den koordineli kampanya) <b>kaçırırdı</b>.
    /// </summary>
    [Fact]
    public void R2_WinsOverR1_WhenBothWouldMatch()
    {
        var t = SuspicionThresholds.Default;
        var both = new LoginHistorySnapshot(
            RecentAccountFailures: t.AccountFailureThreshold,
            RecentIpFailures: t.IpFailureThreshold,
            DistinctIdentifiersFromIp: t.DistinctAccountsFromIpThreshold);

        SuspiciousLoginRules.Evaluate(Failed(), both, utcNow: Now)
            .Should().Be(SuspicionRules.CredentialStuffing);
    }

    // ────────────────────────────────────────────────────────────────────────
    // R3 — panel kullanıcısının hiç görülmemiş IP'si
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void R3_FiresForAPanelUserFromANeverSeenIp()
    {
        var newIp = new LoginHistorySnapshot(IpSeenBeforeForUser: false);

        SuspiciousLoginRules.Evaluate(Succeeded(isPanelUser: true), newIp, utcNow: Now)
            .Should().Be(SuspicionRules.NewIpForPanelUser);
    }

    /// <summary>
    /// ⚠️ Vatandaş için ASLA yanmaz. Mobil şebekede IP her gün değişir; bu kural orada
    /// açık olsaydı her kullanıcının her girişi şüpheli olur ve liste ilk günde
    /// kullanılamaz hâle gelirdi.
    /// </summary>
    [Fact]
    public void R3_NeverFiresForACitizenAccount()
    {
        var newIp = new LoginHistorySnapshot(IpSeenBeforeForUser: false);

        SuspiciousLoginRules.Evaluate(Succeeded(isPanelUser: false), newIp, utcNow: Now)
            .Should().BeNull();
    }

    [Fact]
    public void R3_DoesNotFireForAKnownIp()
    {
        var knownIp = new LoginHistorySnapshot(IpSeenBeforeForUser: true);

        SuspiciousLoginRules.Evaluate(Succeeded(isPanelUser: true), knownIp, utcNow: Now)
            .Should().BeNull();
    }

    // ────────────────────────────────────────────────────────────────────────
    // R4 — kilit biter bitmez gelen başarılı giriş
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔑 Bu kural, sistemin körlüğünü kapatan kural: bir saldırı başarısız denemelerle
    /// başlar ama <b>başarılı</b> bir girişle biter. Yalnız başarısızlıklara bakan bir
    /// gözlem katmanı tam da işe yarayacağı anda kör kalır.
    /// </summary>
    [Fact]
    public void R4_FiresWhenSuccessArrivesRightAfterTheLockoutExpired()
    {
        var justExpired = Now.AddMinutes(-1);

        SuspiciousLoginRules.Evaluate(
                Succeeded(lockedOutUntil: justExpired),
                new LoginHistorySnapshot(IpSeenBeforeForUser: true),
                utcNow: Now)
            .Should().Be(SuspicionRules.SuccessRightAfterLockout);
    }

    [Fact]
    public void R4_DoesNotFireLongAfterTheLockoutExpired()
    {
        var longAgo = Now - SuspicionThresholds.Default.JustAfterLockoutWindow - TimeSpan.FromMinutes(1);

        SuspiciousLoginRules.Evaluate(
                Succeeded(lockedOutUntil: longAgo),
                new LoginHistorySnapshot(IpSeenBeforeForUser: true),
                utcNow: Now)
            .Should().BeNull("saatler sonra gelen giriş sıradan bir giriştir");
    }

    /// <summary>Kilit HÂLÂ sürüyorsa bu bir "kilit sonrası giriş" değildir (zaten olamaz).</summary>
    [Fact]
    public void R4_DoesNotFireWhileTheLockoutIsStillActive()
    {
        SuspiciousLoginRules.Evaluate(
                Succeeded(lockedOutUntil: Now.AddMinutes(5)),
                new LoginHistorySnapshot(IpSeenBeforeForUser: true),
                utcNow: Now)
            .Should().BeNull();
    }

    /// <summary>
    /// R4, R3'ten önce gelir: "kilit sonrası başarılı giriş" daha spesifik ve daha ciddi
    /// bir olay; saldırgan zaten yeni bir IP'den gelmiş olacaktır ve iki kural birden
    /// eşleşirse yöneticinin görmesi gereken kural R4'tür.
    /// </summary>
    [Fact]
    public void R4_WinsOverR3_WhenBothWouldMatch()
    {
        SuspiciousLoginRules.Evaluate(
                Succeeded(isPanelUser: true, lockedOutUntil: Now.AddMinutes(-1)),
                new LoginHistorySnapshot(IpSeenBeforeForUser: false),
                utcNow: Now)
            .Should().Be(SuspicionRules.SuccessRightAfterLockout);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Sessizlik — sıradan giriş şüpheli olmamalı
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bir gözlem katmanının en kolay bozulma biçimi "her şeyi işaretlemek"tir: uyarı
    /// listesi gürültüye boğulur, yönetici bakmayı bırakır ve sistem çalışıyor görünürken
    /// hiçbir işe yaramaz.
    /// </summary>
    [Fact]
    public void OrdinaryLogin_IsNeverSuspicious()
    {
        SuspiciousLoginRules.Evaluate(
                Succeeded(isPanelUser: true),
                new LoginHistorySnapshot(RecentAccountFailures: 0, IpSeenBeforeForUser: true),
                utcNow: Now)
            .Should().BeNull();
    }

    [Fact]
    public void SingleFailedAttempt_IsNeverSuspicious()
    {
        SuspiciousLoginRules.Evaluate(
                Failed(),
                new LoginHistorySnapshot(RecentAccountFailures: 1, RecentIpFailures: 1, DistinctIdentifiersFromIp: 1),
                utcNow: Now)
            .Should().BeNull("herkes bir kez parolasını yanlış yazar");
    }

    /// <summary>Kural adları panelde ve e-postada karşılığı olan sabitlerden gelmeli.</summary>
    [Fact]
    public void EveryProducibleRuleName_IsAKnownConstant()
    {
        var t = SuspicionThresholds.Default;

        var produced = new[]
        {
            SuspiciousLoginRules.Evaluate(Failed(), new LoginHistorySnapshot(RecentAccountFailures: t.AccountFailureThreshold), utcNow: Now),
            SuspiciousLoginRules.Evaluate(Failed(), new LoginHistorySnapshot(RecentIpFailures: t.IpFailureThreshold, DistinctIdentifiersFromIp: t.DistinctAccountsFromIpThreshold), utcNow: Now),
            SuspiciousLoginRules.Evaluate(Succeeded(), new LoginHistorySnapshot(IpSeenBeforeForUser: false), utcNow: Now),
            SuspiciousLoginRules.Evaluate(Succeeded(lockedOutUntil: Now.AddMinutes(-1)), new LoginHistorySnapshot(), utcNow: Now)
        };

        produced.Should().OnlyContain(rule => rule != null && SuspicionRules.All.Contains(rule));
        produced.Distinct().Should().HaveCount(4, "dört kural dört farklı ad üretmeli");
    }
}
