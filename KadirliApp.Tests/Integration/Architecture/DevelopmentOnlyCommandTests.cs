using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using KadirliApp.Application;
using KadirliApp.Application.Common.Behaviors;
using KadirliApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.19a — görünmez sözleşme <b>#78</b>'in <b>yapısal</b> ayağı:
/// <i>"panelde, Production'da açık kalan ve veritabanına toplu yazan bir aksiyon var mı?"</i>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu dosya, denetimin bulduğu deliğin ikinci yarısıdır.</b> Birinci yarı hatanın
/// kendisiydi (<c>/Dashboard/Seed</c> Production'da açık, GET, antiforgery dışı). İkinci
/// yarı daha rahatsız edici: <c>CODE_REVIEW_CHECKLIST</c> §4'te <b>kardeş kural zaten
/// vardı</b> (<i>"hassas bilgi <c>IsDevelopment()</c> koşulu olmadan ekrana basılıyor mu?"</i>)
/// ve <c>ProductionReadinessGuard</c> da vardı — <b>ikisi de bu aksiyonu kapsamıyordu</b>.
/// Yani hata bilgi eksikliğinden değil <b>kapsam deliğinden</b> doğdu; Faz A'nın dersinin
/// (<i>"kapsam dizinden mi, tipten mi, elden mi?"</i>) yedinci tekrarı.
/// </para>
/// <para>
/// 🔑 <b>Bu yüzden kapsam burada ELLE TUTULMUYOR.</b> Denetlenecek komut kümesi
/// <see cref="IDevelopmentOnlyCommand"/>'ı uygulayan tiplerden <b>yansımayla</b> türer;
/// panel tarafındaki tarama da o <i>tip adlarından</i> üretilir. Yarın yazılacak ikinci bir
/// seed/bakım aksiyonu kendiliğinden kapsama girer — kimsenin bu dosyaya bir satır
/// eklemesi gerekmez.
/// </para>
/// <para>
/// 📌 Container gerektirmez — yansıma + kaynak taraması
/// (<c>ModerationSingleOwnerTests</c> / <c>PanelExternalOriginTests</c> deseni).
/// </para>
/// </remarks>
public class DevelopmentOnlyCommandTests
{
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    /// <summary>Yalnız geliştirmeye açık komutlar — <b>yansımayla türetilir.</b></summary>
    private static IReadOnlyList<Type> DevelopmentOnlyCommands()
    {
        var commands = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IDevelopmentOnlyCommand).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        commands.Should().NotBeEmpty(
            "IDevelopmentOnlyCommand uygulayan komut bulunamadıysa bu dosyadaki hiçbir test " +
            "bir şey denetlemiyor demektir (işaretleyici arayüz sessizce ölmüş olurdu)");

        return commands;
    }

    // ── 1) Kapının kendisi boru hattında mı? ───────────────────────────────────

    /// <summary>
    /// 🔴 <b>En önemli iddia ve tek başına yeterli değil, ama olmazsa hepsi anlamsız.</b>
    /// İşaretleyici arayüz tek başına hiçbir şey yapmaz: kapı
    /// <c>AddApplication</c>'daki <c>AddOpenBehavior</c> satırıdır. O satır silinirse
    /// bütün dev-only komutlar <b>her ortamda</b> koşmaya başlar ve <b>hiçbir şey hata
    /// vermez</b> — komutlar çalışır, testler yeşil kalır, panel normal görünür.
    /// </summary>
    [Fact]
    public void ThePipeline_RegistersTheDevelopmentOnlyGuard()
    {
        var behaviors = PipelineBehaviors();

        behaviors.Should().Contain(typeof(DevelopmentOnlyBehavior<,>),
            "ortam kapısı MediatR boru hattında kayıtlı olmalı; kayıt düşerse " +
            "IDevelopmentOnlyCommand işaretleyicisi TAMAMEN etkisiz kalır ve bunu " +
            "başka hiçbir test söylemez");
    }

    /// <summary>
    /// Sıra bir tercih değil, kuralın kendisi: <c>AuditBehavior</c> izi handler
    /// <b>döndükten sonra</b> yazar. Kapı ondan sonra dursaydı reddedilen komut çoktan
    /// koşmuş olurdu — yani reddetme <i>yalnızca kâğıt üzerinde</i> kalırdı.
    /// </summary>
    [Fact]
    public void TheGuard_RunsBeforeEveryOtherBehavior()
    {
        var behaviors = PipelineBehaviors();

        behaviors.IndexOf(typeof(DevelopmentOnlyBehavior<,>)).Should().Be(0,
            "ortam kapısı boru hattının EN BAŞINDA olmalı. Bugünkü sıra: {0}",
            string.Join(" → ", behaviors.Select(b => b!.Name)));
    }

    private static List<Type?> PipelineBehaviors()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        return services
            .Where(d => d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();
    }

    // ── 2) Panel tarafı: POST + ortam kapısı ───────────────────────────────────

    /// <summary>
    /// 🔴 <b>Denetimin bulduğu bileşik hasarın kilidi.</b> Aksiyonun <c>[HttpGet]</c> olması
    /// tek başına bir stil sorunu değildi: <c>AutoValidateAntiforgeryToken</c> global filtresi
    /// <b>yalnız POST/PUT/DELETE</b> doğrular, yani GET aksiyon CSRF korumasının <i>tamamen
    /// dışındaydı</i>. Sonucu somut: yöneticinin ziyaret ettiği kötü niyetli bir sayfadaki
    /// tek bir <c>&lt;img src="…/Dashboard/Seed"&gt;</c> etiketi, <b>onun oturumuyla</b>
    /// canlıda sahte vefat ilanı yayınlardı.
    /// </summary>
    /// <remarks>
    /// ⚠️ Kapsam <b>komut adlarından</b> türetilir, controller listesinden değil: yarın
    /// başka bir controller ikinci bir dev-only komut gönderirse kendiliğinden taranır.
    /// </remarks>
    [Fact]
    public void EveryPanelActionThatSendsADevelopmentOnlyCommand_IsPostOnly()
    {
        var offenders = new List<string>();

        foreach (var (file, action) in PanelActionsSendingDevelopmentOnlyCommands())
        {
            if (!action.Attributes.Contains("[HttpPost]", StringComparison.Ordinal))
                offenders.Add($"{Path.GetFileName(file)} → {action.Name} (öznitelikler: {action.Attributes.Trim()})");
        }

        offenders.Should().BeEmpty(
            "yalnız geliştirmeye açık bir komutu gönderen panel aksiyonu [HttpPost] olmalı — " +
            "GET, global AutoValidateAntiforgeryToken filtresinin KAPSAMI DIŞINDADIR ve " +
            "bir <img> etiketinden bile tetiklenebilir. İhlaller: {0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// İkinci hat: aksiyonun kendisi Production'da <b>404</b> dönmeli. Boru hattı zaten
    /// reddediyor, ama reddedilen bir yol hâlâ <i>var olan</i> bir yoldur — panelde
    /// "tıklayınca hata veren buton" bırakmıyoruz (§5) ve mobildeki
    /// <c>/gelistirici/ag</c> dersinin (11.16) sunucu tarafındaki karşılığı bu:
    /// menüyü/butonu gizlemek yetmez, <b>yolun kendisi</b> koşullu olmalı.
    /// </summary>
    [Fact]
    public void EveryPanelActionThatSendsADevelopmentOnlyCommand_IsGatedByTheEnvironment()
    {
        var offenders = new List<string>();

        foreach (var (file, action) in PanelActionsSendingDevelopmentOnlyCommands())
        {
            if (!Regex.IsMatch(action.Body, @"IsDevelopment\s*\(\s*\)"))
                offenders.Add($"{Path.GetFileName(file)} → {action.Name}");
        }

        offenders.Should().BeEmpty(
            "yalnız geliştirmeye açık bir komutu gönderen panel aksiyonu, Production'da " +
            "adresin KENDİSİNİ kapatmalı (`if (!_env.IsDevelopment()) return NotFound();`). " +
            "Ortam kontrolü bulunamayan aksiyonlar: {0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// 🔑 <b>Ters yön ve gerçekten kırılabilir olan yön.</b> Yukarıdaki iki tarama, hiçbir
    /// panel aksiyonu bulunamadığında da <b>yeşil kalır</b> (boş küme üzerinde her iddia
    /// doğrudur). Komut adı değişip tarama sessizce hiçbir şey bulamaz hâle geldiğinde
    /// bunu söyleyecek tek şey bu testtir.
    /// </summary>
    [Fact]
    public void TheScan_ActuallyFindsThePanelActions()
    {
        PanelActionsSendingDevelopmentOnlyCommands().Should().NotBeEmpty(
            "dev-only komutları gönderen hiçbir panel aksiyonu BULUNAMADI. Ya komutlar " +
            "panelden çağrılmıyor (o zaman bu dosyanın panel ayağı ölü koddur) ya da " +
            "tarama kırıldı — ikisi de sessizdir ve ikincisi tehlikelidir");
    }

    private sealed record PanelAction(string Name, string Attributes, string Body);

    /// <summary>
    /// Dev-only komut gönderen panel aksiyonlarını bulur: kapsam <b>komut tiplerinden</b>
    /// (yansıma), aksiyonun sınırı ise kaynaktan çıkarılır.
    /// </summary>
    private static List<(string File, PanelAction Action)> PanelActionsSendingDevelopmentOnlyCommands()
    {
        var commandNames = DevelopmentOnlyCommands().Select(t => t.Name).ToList();
        var controllersRoot = Path.Combine(RepositoryRoot(), "KadirliApp.Web", "Controllers");
        var found = new List<(string, PanelAction)>();

        foreach (var file in Directory.EnumerateFiles(controllersRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            if (!commandNames.Any(name => Regex.IsMatch(source, $@"new\s+{Regex.Escape(name)}\s*\(")))
                continue;

            foreach (var action in ActionMethods(source))
            {
                if (commandNames.Any(name => Regex.IsMatch(action.Body, $@"new\s+{Regex.Escape(name)}\s*\(")))
                    found.Add((file, action));
            }
        }

        return found;
    }

    /// <summary>
    /// Controller kaynağını aksiyon metotlarına böler: <b>öznitelik bloğu + gövde</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Kaba ama yeterli bir ayrıştırma: bir sonraki <c>public</c> üyeye kadar okur.
    /// Roslyn'e bağlanmak daha doğru olurdu; bedeli, derleme zamanı denetimi olan bu
    /// dosyaya bir analiz paketi bağımlılığı eklemek olurdu ve tarama zaten
    /// <see cref="TheScan_ActuallyFindsThePanelActions"/> ile kendi kendini kanıtlıyor.
    /// </remarks>
    private static IEnumerable<PanelAction> ActionMethods(string source)
    {
        var signature = new Regex(
            @"(?<attrs>(?:^[ \t]*\[[^\]]*\][ \t]*\r?\n)*)[ \t]*public\s+(?:async\s+)?[\w<>,\?\[\] ]+\s+(?<name>\w+)\s*\([^)]*\)",
            RegexOptions.Multiline);

        var matches = signature.Matches(source).ToList();

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : source.Length;

            yield return new PanelAction(
                matches[i].Groups["name"].Value,
                matches[i].Groups["attrs"].Value,
                source[start..end]);
        }
    }

    // ── 3) Kapıyı BAYPAS eden bir yol kalmadı mı? ──────────────────────────────

    /// <summary>
    /// 🔴 <b>Kapı komuta bağlandı; komutun ARKASINDAKİ sınıfa doğrudan erişim kalırsa
    /// kapının hiçbir hükmü olmaz.</b> 12.19a öncesinde <c>DashboardController</c> tam
    /// bunu yapıyordu: <c>MockDataSeeder.SeedAsync(_db)</c>. Katman olarak yasaldı
    /// (<c>Web → Infrastructure</c>, §1) — yani <b>derleyici bunu asla söylemez</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Denetim <c>MockDataSeeder</c> tipinin <i>adından</i> türetiliyor ve yalnız
    /// Infrastructure'ın kendi içinde (sarmalayıcı <c>MockDataSeederService</c>) serbest.
    /// </remarks>
    [Fact]
    public void MockDataSeeder_IsUnreachableFromTheHosts()
    {
        var seeder = typeof(KadirliApp.Infrastructure.Persistence.MockDataSeeder).Name;
        var offenders = new List<string>();

        foreach (var host in new[] { "KadirliApp.Web", "KadirliApp.Api" })
        {
            var root = Path.Combine(RepositoryRoot(), host);
            if (!Directory.Exists(root)) continue;

            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                    file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                    continue;

                // `MockDataSeederService` eşleşmesin diye kelime sınırı + ardından '.' ya da ')' aranır.
                if (Regex.IsMatch(StripComments(File.ReadAllText(file)), $@"\b{seeder}\s*\."))
                    offenders.Add(Path.GetRelativePath(RepositoryRoot(), file));
            }
        }

        offenders.Should().BeEmpty(
            "MockDataSeeder'a host katmanından DOĞRUDAN erişim, ortam kapısını da denetim " +
            "izini de baypas eder (12.19a öncesi DashboardController tam bunu yapıyordu ve " +
            "derleyici hiçbir şey söylemiyordu). Tek yol SeedMockDataCommand. İhlaller: {0}",
            string.Join(", ", offenders));
    }

    private static string StripComments(string source) =>
        Regex.Replace(
            Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline),
            @"^\s*//.*$", string.Empty, RegexOptions.Multiline);
}
