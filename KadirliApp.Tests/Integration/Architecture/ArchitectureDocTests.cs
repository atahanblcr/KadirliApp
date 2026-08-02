using System.Text.RegularExpressions;
using FluentAssertions;
using KadirliApp.Api.Controllers.Admin;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 11.14 — **Doküman çürüme önlemi.** <c>ARCHITECTURE.md</c>'nin modül tablosu, projeye
/// ilk kez bakan birinin "neyin nerede" sorusunu cevapladığı yer. Bir modül eklenip tabloya
/// yazılmazsa (ya da kaldırılıp tablodan silinmezse) doküman **sessizce yalan söylemeye**
/// başlar ve o günden sonra kimse ona güvenmez.
///
/// Bu testler dokümanı gerçek klasörlerle karşılaştırır: uyumsuzluk <c>dotnet test</c>'i
/// kırar. Kırıldığında yapılacak şey testi gevşetmek değil, **tabloyu güncellemektir**.
///
/// ⚠️ Bilinçli olarak "harf harf aynı" denetimi yapılmıyor — doküman insan için yazılmış
/// bir tablo, kod üretimi girdisi değil. Denetlenen tek şey: **her modül tabloda geçiyor mu,
/// tabloda gerçekte olmayan bir modül var mı.**
/// </summary>
public class ArchitectureDocTests
{
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    private static string DocText() => File.ReadAllText(Path.Combine(RepositoryRoot(), "ARCHITECTURE.md"));

    private static IReadOnlyList<string> BackendModules() =>
        new DirectoryInfo(Path.Combine(RepositoryRoot(), "KadirliApp.Application", "Features"))
            .GetDirectories()
            .Select(d => d.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

    [Fact]
    public void ArchitectureDoc_Exists_AndCoversItsPromisedSections()
    {
        var doc = DocText();

        foreach (var heading in new[]
                 {
                     "## 1. Sistem bir bakışta",
                     "## 3. Modül envanteri",
                     "## 4. 🔑 Reçete: **Modül EKLE**",
                     "## 5. 🔑 Reçete: **Modül DEĞİŞTİR**",
                     "## 6. 🔑 Reçete: **Modül KALDIR**",
                     "## 7. 🔑 GÖRÜNMEZ SÖZLEŞMELER",
                     "## 8. Test haritası"
                 })
            doc.Should().Contain(heading, "ARCHITECTURE.md'nin vaat ettiği bölüm kaybolmamalı");
    }

    /// <summary>
    /// Yeni bir <c>Features/X/</c> klasörü açıp tabloya yazmayı unutmak, dokümanın
    /// çürümesinin **en yaygın** yoludur.
    /// </summary>
    [Fact]
    public void EveryBackendFeatureFolder_IsMentionedInTheModuleInventory()
    {
        var doc = DocText();
        var modules = BackendModules();

        modules.Should().HaveCountGreaterThan(15, "proje 20 civarı backend modülü barındırıyor");

        var missing = modules.Where(m => !doc.Contains($"`{m}/`", StringComparison.Ordinal)).ToList();

        missing.Should().BeEmpty(
            "ARCHITECTURE.md §3 modül tablosunda `<Modül>/` biçiminde geçmeyen backend modülleri: {0}. " +
            "Yeni modül eklediyseniz tabloya satır ekleyin (§4 reçetesi 18. adım).",
            string.Join(", ", missing));
    }

    /// <summary>
    /// Ters yön: tabloda yazan ama artık var olmayan modül. Modül kaldırma reçetesinin
    /// (§6) son adımı unutulduğunda ortaya çıkar.
    /// </summary>
    [Fact]
    public void ModuleInventory_DoesNotMentionRemovedBackendModules()
    {
        var doc = DocText();
        var modules = BackendModules().ToHashSet(StringComparer.Ordinal);

        // Yalnız §3 envanter tablosunun numaralı satırlarına bak ("| 1 | **İlanlar** | `Ads/` …").
        // Doküman gövdesindeki `Jobs/`, `Entities/` gibi yol örnekleri kapsam dışı kalsın.
        var mentioned = doc.Split('\n')
            .Where(line => Regex.IsMatch(line, @"^\|\s*\d+\s*\|"))
            .Select(line => Regex.Match(line, @"`([A-Z][A-Za-z]*)/`"))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        mentioned.Should().NotBeEmpty("envanter tablosu okunabilmeli — okunamıyorsa test hiçbir şey denetlemiyor");

        var stale = mentioned.Where(m => !modules.Contains(m)).ToList();

        stale.Should().BeEmpty(
            "ARCHITECTURE.md artık var olmayan backend modüllerinden söz ediyor: {0}. " +
            "Modül kaldırdıysanız tablodan da silin (§6 reçetesi 7. adım).",
            string.Join(", ", stale));
    }

    /// <summary>
    /// Mobil modül kaydı (<c>kAppModules</c>) ile dokümanın mobil sütunu birlikte yürümeli.
    /// Mobil tarafta "ölü buton yok" testi zaten var; buradaki denetim dokümanı bağlar.
    /// </summary>
    [Fact]
    public void EveryMobileFeatureFolder_IsMentionedInTheModuleInventory()
    {
        var root = RepositoryRoot();
        var featuresDir = new DirectoryInfo(Path.Combine(root, "mobile", "lib", "features"));
        if (!featuresDir.Exists)
            return; // mobil klasörü olmayan bir checkout'ta (ör. yalnız backend CI) test anlamsız

        var doc = DocText();
        var folders = featuresDir.GetDirectories().Select(d => d.Name).ToList();
        folders.Should().HaveCountGreaterThan(15);

        var missing = folders.Where(f => !doc.Contains($"`{f}/`", StringComparison.Ordinal)).ToList();

        missing.Should().BeEmpty(
            "ARCHITECTURE.md'de geçmeyen mobil feature klasörleri: {0}",
            string.Join(", ", missing));
    }

    /// <summary>
    /// Panel controller sayısı tabloda yazılı; modül eklenip panel satırı unutulursa
    /// ya da panel kaldırılıp tablo güncellenmezse burası uyarır.
    /// </summary>
    [Fact]
    public void AdminControllerCount_MatchesTheNumberWrittenInTheDoc()
    {
        var doc = DocText();

        var actual = typeof(AdminApiControllerBase).Assembly.GetTypes()
            .Count(t => t.Namespace == "KadirliApp.Api.Controllers.Admin"
                        && t is { IsClass: true, IsAbstract: false, IsPublic: true }
                        && t.Name.EndsWith("Controller", StringComparison.Ordinal));

        var match = Regex.Match(doc, @"\*\*(\d+) admin controller\*\*");
        match.Success.Should().BeTrue("ARCHITECTURE.md §2 admin controller sayısını yazıyor");

        int.Parse(match.Groups[1].Value).Should().Be(actual,
            "dokümandaki admin controller sayısı gerçekle uyuşmalı (gerçek: {0})", actual);
    }
}
