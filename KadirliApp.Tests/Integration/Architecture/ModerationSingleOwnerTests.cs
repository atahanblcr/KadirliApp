using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.10 — görünmez sözleşme <b>#52</b>'nin <b>yapısal</b> ayağı:
/// moderasyon durumu yalnız <c>Approve</c>/<c>Reject</c> komutlarından yazılır.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden yapısal test şart (davranış testi yetmez).</b> Davranış testi bugünkü dört
/// modülü kilitler; <b>beşinci</b> modül eklendiğinde hiçbir şey kırılmaz ve kural sessizce
/// delinir. Aynı ders 12.9'da öğrenildi: elle liste tutan bir denetim, beşinci CDN
/// eklendiği gün yeşil kalıyordu (<c>PanelExternalOriginTests</c>).
/// </para>
/// <para>
/// ⚠️ Bu yüzden aşağıdaki testler <b>elle modül listesi tutmaz</b>: dosya sistemini tarar.
/// Yeni bir <c>Update*CommandHandler</c> ya da yeni bir Düzenle görünümü kendiliğinden
/// kapsama girer.
/// </para>
/// <para>
/// 📌 Container gerektirmez — saf dosya taraması, derleme zamanı denetimi
/// (<c>PanelExternalOriginTests</c> deseni).
/// </para>
/// </remarks>
public class ModerationSingleOwnerTests
{
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    private static string FeaturesRoot() =>
        Path.Combine(RepositoryRoot(), "KadirliApp.Application", "Features");

    /// <summary>
    /// Moderasyonu olan modüller — <b>türetilir, elle tutulmaz.</b>
    /// </summary>
    /// <remarks>
    /// 🔑 Ölçüt: modülün bir <c>Approve*</c> komutu var mı. Elle liste yazılsaydı beşinci
    /// bir moderasyonlu modül eklendiğinde denetim onu <b>hiç görmez</b> ve sessizce yeşil
    /// kalırdı (12.9'un dersi: elle liste tutan bir kapı, listeye girmeyeni korumaz).
    /// Ölçütün yan faydası kapsamı <i>doğru</i> tutması: duyurunun <c>draft/active/scheduled</c>
    /// durumu bir moderasyon kararı değil, modülün kendi yayın yaşam döngüsüdür — ve
    /// duyuruda <c>Approve</c> komutu yok, yani kapsam dışı kalır.
    /// </remarks>
    private static IReadOnlyList<string> ModeratedModules()
    {
        var modules = new DirectoryInfo(FeaturesRoot())
            .GetDirectories()
            .Where(d => d.GetFiles("Approve*.cs", SearchOption.AllDirectories).Length > 0)
            .Select(d => d.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        modules.Should().NotBeEmpty("moderasyonlu modül bulunamadıysa test hiçbir şey denetlemiyor");
        return modules;
    }

    /// <summary>Modül klasör adı → panel controller adı (<c>Ads</c> → <c>AdsAdmin</c>).</summary>
    private static string PanelControllerFor(string module) => module + "Admin";

    // ── 1) Düzenle görünümü durumu YAZAMAZ ─────────────────────────────────────

    /// <summary>
    /// 🔴 12.10'un kapattığı ikinci yolun <b>giriş kapısı</b>: dört Düzenle formundaki
    /// durum açılır menüsü. Geri konursa panel yine moderasyon kuralı uygulamayan bir
    /// yazma yolu kazanır ve <b>hiçbir şey hata vermez</b>.
    /// </summary>
    /// <remarks>
    /// Denetim <c>&lt;select&gt;</c>'e değil <c>asp-for="Status"</c>'a bakıyor ve bu bilinçli:
    /// radyo düğmesi, gizli alan ya da metin kutusu da aynı yolu açardı. Form durumu
    /// <b>hiçbir yönde</b> taşımaz — hata sonrası yeniden çizimde durum controller'da
    /// veritabanından tazeleniyor (<c>RedisplayEditAsync</c>).
    /// </remarks>
    [Fact]
    public void NoModeratedEditView_BindsAnInputToStatus()
    {
        var offenders = new List<string>();

        foreach (var controller in ModeratedModules().Select(PanelControllerFor))
        {
            var view = Path.Combine(RepositoryRoot(), "KadirliApp.Web", "Views", controller, "Edit.cshtml");
            File.Exists(view).Should().BeTrue($"{controller}/Edit.cshtml bulunamadı — test hiçbir şey denetlemiyor olurdu");

            if (Regex.IsMatch(File.ReadAllText(view), "asp-for\\s*=\\s*\"Status\"", RegexOptions.IgnoreCase))
                offenders.Add($"{controller}/Edit.cshtml");
        }

        offenders.Should().BeEmpty(
            "moderasyon durumu Düzenle formundan yazılamaz (§7 madde 52). Bulunanlar: {0}. " +
            "Durumu göstermek için _ModerationStatusField partial'ını kullanın (salt-okunur rozet + " +
            "Onayla/Reddet).",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Ters yön: menü kaldırıldı ama yerine <b>hiçbir şey</b> konmadıysa yönetici kaydın
    /// durumunu artık göremez — bir hatayı düzeltirken bilgi silmek olurdu.
    /// </summary>
    [Fact]
    public void EveryModeratedEditView_ShowsTheStatusReadOnly()
    {
        foreach (var controller in ModeratedModules().Select(PanelControllerFor))
        {
            var view = Path.Combine(RepositoryRoot(), "KadirliApp.Web", "Views", controller, "Edit.cshtml");

            File.ReadAllText(view).Should().Contain("_ModerationStatusField",
                $"{controller}/Edit.cshtml durumu salt-okunur göstermeli — menüyü kaldırıp yerine " +
                "hiçbir şey koymamak yöneticiyi kör bırakır");
        }
    }

    // ── 2) Update handler'ları durumu YAZAMAZ ──────────────────────────────────

    /// <summary>
    /// 🔴 Kuralın kalbi. Bir <c>Update*</c> handler'ı <c>.Status =</c> yazdığı anda üç şey
    /// birden atlanır ve <b>üçü de sessizdir</b>: modülün onay kuralları (#25 taze pencere,
    /// bayat gerekçe temizliği), denetim izinin kararı <c>approve</c> olarak yazması ve
    /// yetki matrisi (<c>Edit</c> → <c>update</c>, #19).
    /// </summary>
    /// <remarks>
    /// ⚠️ Kapsam <b>türetilir</b> (<see cref="ModeratedModules"/>), elle liste tutulmaz:
    /// beşinci bir moderasyonlu modül eklendiğinde kendiliğinden kapsanır. Arka plan işleri
    /// (<c>ExpireAdsJob</c>, <c>ArchiveDeathsJob</c>) ve <c>Approve</c>/<c>Reject</c>/
    /// <c>Archive</c> komutları kendi geçişlerinin meşru sahibidir, bu yüzden yalnız
    /// <c>Update*</c> dosyaları taranır.
    /// <para>
    /// 🐛 <b>Bu test yazıldığı gün kırmızıydı ve haklıydı:</b> <c>UpdateMyAdCommandHandler</c>
    /// (vatandaşın kendi ilanını düzenlemesi) durumu <c>pending</c>'e çekiyor ve onay/red
    /// izlerini <i>elle</i> temizliyordu — aynı bilginin üçüncü kopyası. Geçiş
    /// <c>AdModeration.Resubmit</c>'e taşındı.
    /// </para>
    /// </remarks>
    [Fact]
    public void NoUpdateCommandHandler_WritesToStatus()
    {
        var featuresRoot = FeaturesRoot();

        var updateCommandFiles = ModeratedModules()
            .SelectMany(m => Directory.GetFiles(Path.Combine(featuresRoot, m), "Update*.cs", SearchOption.AllDirectories))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        updateCommandFiles.Should().NotBeEmpty(
            "Update* komut dosyası bulunamadıysa test hiçbir şey denetlemiyor demektir");

        // `x.Status = …` — ama `x.Status == …` (karşılaştırma) değil.
        var write = new Regex(@"\.Status\s*=\s*(?!=)", RegexOptions.Compiled);

        var offenders = updateCommandFiles
            .Where(f => write.IsMatch(StripComments(File.ReadAllText(f))))
            .Select(f => Path.GetRelativePath(featuresRoot, f).Replace('\\', '/'))
            .ToList();

        offenders.Should().BeEmpty(
            "moderasyon durumunun tek sahibi Approve/Reject komutlarıdır (§7 madde 52). " +
            "Durumu yazan Update* komutları: {0}. Kural taşınacaksa ilgili …Moderation sınıfına " +
            "taşıyın; komut yalnız ModerationStatusGuard.EnsureUnchanged çağırmalı.",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Ters yön ve en az diğeri kadar önemli: yazmayı bırakmak yetmez, gelen değeri
    /// <b>sessizce yutmamak</b> da gerekir (#37: hiçbir şey yapmayan buton, işlevsiz
    /// butondan kötüdür). Durum alanı taşıyan her <c>Update*</c> komutu guard'ı çağırmalı.
    /// </summary>
    [Fact]
    public void EveryModeratedModuleWithAStatusCarryingUpdate_CallsTheGuard()
    {
        var featuresRoot = FeaturesRoot();

        // Bir Update* dosyası "Status" alanı taşıyorsa (DTO'da duruyor, §5) guard şart.
        // ⚠️ Alan ile handler farklı dosyalarda olabilir (UpdateAdCommand + …Handler) ve DTO
        // ayrı bir klasörde (Deaths/Dtos/) — bu yüzden hem alan hem guard MODÜL genelinde aranır.
        var carriesStatus = new Regex(@"\bstring\??\s+Status\b", RegexOptions.Compiled);

        var checkedModules = 0;
        var offenders = new List<string>();

        foreach (var module in ModeratedModules())
        {
            var moduleRoot = Path.Combine(featuresRoot, module);
            var updateFiles = Directory.GetFiles(moduleRoot, "Update*.cs", SearchOption.AllDirectories);

            if (!updateFiles.Any(f => carriesStatus.IsMatch(StripComments(File.ReadAllText(f)))))
                continue;

            checkedModules++;

            var guarded = Directory
                .GetFiles(moduleRoot, "*.cs", SearchOption.AllDirectories)
                .Any(f => File.ReadAllText(f).Contains("ModerationStatusGuard.EnsureUnchanged"));

            if (!guarded) offenders.Add(module);
        }

        checkedModules.Should().Be(ModeratedModules().Count,
            "dört moderasyonlu modülün dördü de §5 gereği Status alanını DTO'sunda TUTUYOR olmalı — " +
            "alan sessizce silinmişse mağazadaki eski sürümler ve admin API istemcileri kırılmıştır");

        offenders.Should().BeEmpty(
            "durum alanı taşıyan komut, gelen değeri sessizce yutamaz — farklıysa REDDETMELİ " +
            "(ModerationStatusGuard.EnsureUnchanged). Guard çağırmayan modüller: {0}",
            string.Join(", ", offenders));
    }

    // ── 3) Onayla/Reddet komutları geçiş sınıfına delege eder ──────────────────

    /// <summary>
    /// Kuralı bir sınıfa taşıyıp handler'da <b>ayrıca</b> yazmak, tek sahipliği ilk gün
    /// bozar. Bu yüzden <c>Approve*</c>/<c>Reject*</c>/<c>Archive*</c> komutları da ham
    /// <c>.Status =</c> yazmaz — <c>…Moderation</c> sınıfını çağırır.
    /// </summary>
    [Fact]
    public void ModerationCommands_DelegateToTheTransitionClass()
    {
        var featuresRoot = FeaturesRoot();

        var commandFiles = ModeratedModules()
            .SelectMany(m => new[] { "Approve*.cs", "Reject*.cs", "Archive*.cs" }
                .SelectMany(pattern => Directory.GetFiles(Path.Combine(featuresRoot, m), pattern, SearchOption.AllDirectories)))
            .Distinct()
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        commandFiles.Should().NotBeEmpty("moderasyon komutu bulunamadıysa test hiçbir şey denetlemiyor");

        var write = new Regex(@"\.Status\s*=\s*(?!=)", RegexOptions.Compiled);

        var offenders = commandFiles
            .Where(f => write.IsMatch(StripComments(File.ReadAllText(f))))
            .Select(f => Path.GetRelativePath(featuresRoot, f).Replace('\\', '/'))
            .ToList();

        offenders.Should().BeEmpty(
            "moderasyon geçişi …Moderation sınıfında yaşamalı; handler yalnız delege eder. " +
            "Ham yazanlar: {0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Yorum satırları ve XML belgeleri taramadan düşürülür — bu projede bir kuralın
    /// <i>anlatıldığı</i> yorumlar kodun kendisinden uzun olabiliyor ve
    /// <c>"ad.Status = \"approved\""</c> yazan bir açıklama testi haksız yere kırardı.
    /// </summary>
    private static string StripComments(string source) =>
        Regex.Replace(
            Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline),
            @"^\s*//.*$", string.Empty, RegexOptions.Multiline);
}
