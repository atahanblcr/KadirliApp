using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.10/12.11 — görünmez sözleşme <b>#52</b>'nin <b>yapısal</b> ayağı:
/// moderasyon durumu yalnız <c>Approve</c>/<c>Reject</c>/<c>Archive</c> komutlarından yazılır.
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
/// 🔴 <b>12.11 — bu dosyanın kendi dersi: TARAMANIN KAPSAMI DA ELLE TUTULAN BİR LİSTEDİR.</b>
/// 12.10 modül listesini türetip elle listeyi ortadan kaldırdı, ama taranacak <i>dosya adı
/// desenini</i> (<c>Update*</c>/<c>Approve*</c>/<c>Reject*</c>/<c>Archive*</c>) elle tuttu.
/// Sonuç, 12.9'un dersinin birebir tekrarı oldu: <c>ExtendMyAdCommand</c> hiçbir desene
/// uymadığı için <b>hiç taranmadı</b> ve içinde ham <c>ad.Status = "approved"</c> satırı
/// duruyordu — bu dosya yeşilken. Kayıt bozulmuyordu, yani hasar yoktu; ama <b>koruma
/// tesadüfen çalışıyordu</b>, kurala dayanarak değil.
/// <br/>
/// Çözüm testi genişletmek <i>değil</i>, korumayı taramanın erişemeyeceği bir yere taşımaktı:
/// moderasyon alanları <c>init</c> oldu ve geçişler varlığın metotlarına indi. Aynı satır
/// artık <b>derlenmiyor</b> (<c>CS8852</c>). Aşağıdaki taramalar kaldı ama artık
/// <i>ikinci</i> savunma hattı; <b>birinci hat derleyici</b>, onun bekçisi de
/// <see cref="EveryModeratedEntity_ExposesItsModerationFieldsAsInitOnly"/>.
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
    /// <c>Ad.Resubmit</c>'e taşındı.
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
            "Durumu yazan Update* komutları: {0}. Kural taşınacaksa varlığın geçiş metoduna " +
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

    /// <summary>
    /// 🔴 <b>Faz A bozma turunun bulgusu (13 Ağu 2026): 12.11'in dersi bu dosyada HÂLÂ ayaktaydı.</b>
    ///
    /// <para>
    /// Yukarıdaki iki tarama modül kümesini <b>türetiyor</b> ✅ ama taradıkları dosyaları
    /// <c>Update*.cs</c> <b>deseniyle</b> buluyor ❌ — yani kapsam yine elle tutuluyor.
    /// Ölçüldü: <c>Features/Ads/Commands/</c> altına durum alanı taşıyan ve guard
    /// çağırmayan bir <c>ReviseAdCommand.cs</c> konduğunda <b>hiçbir test kırılmadı</b>.
    /// Bu, 12.11'in <c>ExtendMyAdCommand</c> bulgusunun birebir aynısı — o zaman koruma
    /// derleyiciye taşınmıştı, ama <i>taramanın kendisi</i> aynı kalmıştı.
    /// </para>
    ///
    /// <para>
    /// 🔑 Bu test kapsamı <b>yansımayla</b> kurar (dosya adına bakmaz): moderasyonlu
    /// modüllerin <c>Commands</c> ad alanındaki <b>her</b> MediatR isteği taranır ve
    /// <c>Status</c> özelliği taşıyan her biri için guard çağrısı aranır. Yarın eklenen
    /// <c>ReviseAdCommand</c> kendiliğinden kapsama girer — ada değil <b>tipe</b> bakıldığı için.
    /// </para>
    ///
    /// <para>
    /// ⚠️ Guard çağrısı komutun <b>kendi klasöründe</b> aranır (komut ve handler ayrı
    /// dosyalarda olabiliyor), modül genelinde değil: modül genelinde aramak, aynı modüldeki
    /// <i>başka</i> bir komutun guard'ını bu komuta sayardı — deliğin ikinci yüzü tam buydu.
    /// </para>
    /// </summary>
    [Fact]
    public void EveryStatusCarryingCommand_CallsTheGuard_RegardlessOfItsFileName()
    {
        var featuresRoot = FeaturesRoot();
        var modules = ModeratedModules();

        var commands = typeof(KadirliApp.Application.Common.Moderation.ModerationStatusGuard).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } or { IsValueType: true })
            .Where(t => t.Namespace is not null
                        && modules.Any(m => t.Namespace!.Contains($".Features.{m}.Commands", StringComparison.Ordinal)))
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(MediatR.IRequest<>)))
            .Where(t => t.GetProperty("Status")?.PropertyType == typeof(string)
                        || t.GetProperty("Status")?.PropertyType == typeof(string))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        commands.Should().NotBeEmpty(
            "durum alanı taşıyan komut YANSIMAYLA bulunamadıysa test hiçbir şey denetlemiyor " +
            "(§5 gereği alan DTO'da duruyor olmalı)");

        var offenders = new List<string>();

        foreach (var command in commands)
        {
            var sourceFile = Directory
                .GetFiles(featuresRoot, "*.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => Regex.IsMatch(
                    File.ReadAllText(f), $@"(record|class)\s+{Regex.Escape(command.Name)}\b"));

            if (sourceFile is null)
            {
                offenders.Add($"{command.Name} (kaynak dosyası bulunamadı)");
                continue;
            }

            // Guard, komutun kendi klasöründe çağrılmalı (komut + handler aynı yerde).
            var guarded = Directory
                .GetFiles(Path.GetDirectoryName(sourceFile)!, "*.cs", SearchOption.TopDirectoryOnly)
                .Any(f => File.ReadAllText(f).Contains("ModerationStatusGuard.EnsureUnchanged"));

            if (!guarded) offenders.Add(command.Name);
        }

        offenders.Should().BeEmpty(
            "durum alanı taşıyan HER komut guard'ı çağırmalı (§7 madde 52) — dosya adı ne olursa " +
            "olsun. 12.11'de `ExtendMyAdCommand` tam bu yüzden taranmamıştı; Faz A'da aynı deliğin " +
            "hâlâ açık olduğu ölçüldü (`ReviseAdCommand.cs` eklendi, hiçbir test kırılmadı). " +
            "Guard çağırmayan komutlar: {0}", string.Join(", ", offenders));
    }

    // ── 3) Faz 12.11: geçiş VARLIĞIN kendisinde ve alanlar kapalı ──────────────

    /// <summary>
    /// Moderasyon geçişi tanımlayan varlıklar — <b>türetilir, elle tutulmaz.</b>
    /// Ölçüt: <c>Domain/Entities/</c> altında <c>public void Approve(</c> bildiren dosya.
    /// </summary>
    private static IReadOnlyList<FileInfo> ModeratedEntities()
    {
        var entities = new DirectoryInfo(Path.Combine(RepositoryRoot(), "KadirliApp.Domain", "Entities"))
            .GetFiles("*.cs")
            .Where(f => Regex.IsMatch(StripComments(File.ReadAllText(f.FullName)), @"public\s+void\s+Approve\s*\("))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToList();

        entities.Should().NotBeEmpty("moderasyon geçişi olan varlık bulunamadıysa test hiçbir şey denetlemiyor");
        return entities;
    }

    /// <summary>
    /// 🔴 <b>12.11'in kalbi ve bu dosyadaki en önemli iddia.</b> Moderasyon alanları
    /// <c>init</c> olmak zorunda: <c>set</c>'e çevrildikleri anda tek sahiplik derleyicinin
    /// güvencesi olmaktan çıkıp yeniden bir <i>dosya taramasının</i> kapsamına düşer — ve
    /// 12.11 tam olarak o kapsamın delik olduğunu kanıtladı (<c>ExtendMyAdCommand</c>
    /// yıllarca <c>ad.Status = "approved"</c> yazdı, hiçbir test kırılmadı).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Bu testin var olma sebebi çok somut:</b> <c>init</c>'i bozan biri
    /// <c>CS8852</c> derleme hatası alır. O hatayı çözmenin <i>kolay</i> yolu geçişi
    /// varlığa taşımak değil, alanı <c>set</c>'e geri çevirmektir — ve o an her şey
    /// derlenir, bütün testler yeşil kalır, koruma sessizce kaybolur.
    /// <para>
    /// 📌 Alan listesi de türetilir: varlıkta hangi moderasyon alanı <i>varsa</i> o denetlenir.
    /// <c>Event</c>'te yalnız <c>Status</c> var (onay izi <c>audit_logs</c>'ta),
    /// <c>Campaign</c>/<c>DeathNotice</c>'ta <c>RejectedAt</c> yok — elle liste yazılsaydı
    /// test bu farkları "eksik alan" sanıp haksız yere kırılırdı.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryModeratedEntity_ExposesItsModerationFieldsAsInitOnly()
    {
        string[] moderationFields = ["Status", "ApprovedBy", "ApprovedAt", "RejectedReason", "RejectedAt"];

        var offenders = new List<string>();
        var checkedFields = 0;

        foreach (var entity in ModeratedEntities())
        {
            var source = StripComments(File.ReadAllText(entity.FullName));

            foreach (var field in moderationFields)
            {
                // `public <tip> <Alan> { … }` — özelliğin gövdesini yakala.
                var property = Regex.Match(source, $@"public\s+[\w?<>]+\s+{field}\s*\{{([^}}]*)\}}");
                if (!property.Success) continue;   // o varlıkta bu kolon yok (bkz. remarks)

                checkedFields++;

                if (Regex.IsMatch(property.Groups[1].Value, @"\bset\b"))
                    offenders.Add($"{entity.Name}.{field}");
            }
        }

        checkedFields.Should().BeGreaterThan(moderationFields.Length,
            "her moderasyonlu varlıkta en az birkaç alan denetlenmeli — sayı düştüyse özellik " +
            "biçimi değişmiş ve regex hiçbir şey bulamıyor demektir (test sessizce anlamsızlaşır)");

        offenders.Should().BeEmpty(
            "moderasyon alanları `init` olmalı, `set` DEĞİL (§7 madde 52). `set` olanlar: {0}. " +
            "CS8852 aldıysanız çözüm alanı açmak değil, geçişi varlığın bir metoduna taşımaktır.",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// İki kümenin bağı: moderasyonu olan her <b>modülün</b> karşılığında geçiş metotları
    /// olan bir <b>varlık</b> olmalı. Beşinci bir moderasyonlu modül eklenip varlığı
    /// açık setter'la bırakılırsa, o modül 12.11'in korumasının <b>tümüyle dışında</b> kalır
    /// ve bunu başka hiçbir test söylemez.
    /// </summary>
    [Fact]
    public void EveryModeratedModule_HasAnEntityThatOwnsItsTransitions()
    {
        ModeratedEntities().Count.Should().Be(ModeratedModules().Count,
            "moderasyonlu modül sayısı ({0}: {1}) ile geçiş metodu tanımlayan varlık sayısı " +
            "({2}: {3}) ayrıştı — yeni bir moderasyonlu modül eklendiyse varlığına da " +
            "Approve/Reject metotlarını ekleyin ve alanlarını `init` yapın",
            ModeratedModules().Count, string.Join(", ", ModeratedModules()),
            ModeratedEntities().Count, string.Join(", ", ModeratedEntities().Select(e => e.Name)));
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
