using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 11.15c — **`CODE_REVIEW_CHECKLIST.md` çürüme önlemi.**
///
/// Checklist, projenin tekrarlayan hata sınıflarını (RenderFlex taşması, timezone kayması,
/// cache invalidator eksikliği, `PanelPermission` unutulması…) PR aşamasına çeken tek belge.
/// Ama <c>ARCHITECTURE.md</c>'nin aksine **hiçbir şey onu gerçekle karşılaştırmıyordu**:
/// bir maddenin işaret ettiği test sınıfı yeniden adlandırılsa ya da silinse, checklist
/// sessizce yalan söylemeye başlar ve o günden sonra kimse ona güvenmez —
/// <c>ArchitectureDocTests</c>'in yazılma gerekçesinin birebir aynısı.
///
/// ⚠️ Denetlenen şey **maddelerin doğruluğu değil, referanslarının gerçekliği.** Bir kuralın
/// hâlâ iyi bir kural olup olmadığı insan kararıdır; ama "şuna bak" dediği yerin var olması
/// mekanik olarak denetlenebilir. Bu ayrım bilinçli: checklist bir insan belgesi, kod üretimi
/// girdisi değil.
///
/// Kırıldığında yapılacak şey testi gevşetmek değil, **checklist'i güncellemektir**
/// (ya da gerçekten kaldırılan bir şeye atıf varsa satırı düşürmek).
/// </summary>
public class CodeReviewChecklistDocTests
{
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    private static string ChecklistPath() => Path.Combine(RepositoryRoot(), "CODE_REVIEW_CHECKLIST.md");

    private static string ChecklistText() => File.ReadAllText(ChecklistPath());

    [Fact]
    public void Checklist_Exists()
        => File.Exists(ChecklistPath()).Should().BeTrue(
            "CODE_REVIEW_CHECKLIST.md CLAUDE.md'nin doküman tablosunda listeli — dosya kaybolursa " +
            "o tablo kırık bağlantı gösterir");

    /// <summary>
    /// Checklist'in bölümleri, projenin katman haritasıyla aynı yapıda olmalı. Bir bölüm
    /// tamamen kaybolursa (ör. "Panel") o katman review'da kapsanmaz hâle gelir.
    /// </summary>
    [Fact]
    public void Checklist_CoversEveryLayerOfTheProject()
    {
        var doc = ChecklistText();

        foreach (var heading in new[]
                 {
                     "## 1. Genel / Mimari",
                     "## 2. Backend — Domain / Application (CQRS)",
                     "## 3. Backend — API / Controllers",
                     "## 4. Panel (KadirliApp.Web — Razor/MVC)",
                     "## 5. Mobil (Flutter)",
                     "## 6. Database / Migration",
                     "## 7. Security",
                     "## 8. Performans / Cache",
                     "## 9. Test"
                 })
            doc.Should().Contain(heading, "checklist'in bir katman bölümü kaybolmamalı");
    }

    /// <summary>
    /// 🔑 **Asıl denetim.** Checklist'in "Referans" sütunu test sınıflarına atıf yapıyor
    /// (<c>CacheContractTests</c>, <c>PanelDisplayTests</c>…). Bir sınıf yeniden adlandırılıp
    /// checklist güncellenmezse, madde "bunu şu test yakalar" diye **var olmayan bir emniyet
    /// ağına** güven telkin eder — checklist'in en tehlikeli çürüme biçimi budur.
    ///
    /// Sınıf adları kaynaktan taranır, elle liste tutulmaz (elle liste de çürürdü).
    /// </summary>
    [Fact]
    public void Checklist_ReferencedTestClasses_AllExist()
    {
        var doc = ChecklistText();

        // Checklist'te geçen "…Tests" biçimindeki her tanımlayıcı.
        var referenced = Regex.Matches(doc, @"\b([A-Z][A-Za-z0-9]*Tests)\b")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        referenced.Should().NotBeEmpty(
            "checklist test sınıflarına atıf yapmıyorsa bu test hiçbir şey denetlemiyor demektir");

        // Gerçekte var olan test sınıfları: hem C# (dosya adı) hem Dart (test dosyaları).
        var testProject = new DirectoryInfo(Path.Combine(RepositoryRoot(), "KadirliApp.Tests"));
        var existing = testProject
            .GetFiles("*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetFileNameWithoutExtension(f.Name))
            .ToHashSet(StringComparer.Ordinal);

        var missing = referenced.Where(r => !existing.Contains(r)).ToList();

        missing.Should().BeEmpty(
            "checklist'in atıf yaptığı test sınıfları gerçekte yok: {0}. " +
            "Sınıf yeniden adlandırıldıysa checklist'i güncelleyin; kaldırıldıysa o satırın " +
            "hangi emniyet ağına dayandığını yeniden düşünün.",
            string.Join(", ", missing));
    }

    /// <summary>
    /// Checklist'in atıf yaptığı **mobil** test dosyaları da gerçek olmalı
    /// (`turkish_ui_test.dart`, `app_modules_test.dart`…). Mobil tarafta sınıf değil
    /// dosya adı kullanılıyor, o yüzden ayrı denetim.
    /// </summary>
    [Fact]
    public void Checklist_ReferencedDartTestFiles_AllExist()
    {
        var doc = ChecklistText();

        var referenced = Regex.Matches(doc, @"\b([a-z0-9_]+_test\.dart)\b")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        referenced.Should().NotBeEmpty("checklist mobil testlere de atıf yapıyor olmalı");

        var mobileTests = new DirectoryInfo(Path.Combine(RepositoryRoot(), "mobile", "test"));
        mobileTests.Exists.Should().BeTrue();

        var existing = mobileTests
            .GetFiles("*_test.dart", SearchOption.AllDirectories)
            .Select(f => f.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = referenced.Where(r => !existing.Contains(r)).ToList();

        missing.Should().BeEmpty(
            "checklist'in atıf yaptığı mobil test dosyaları yok: {0}",
            string.Join(", ", missing));
    }

    /// <summary>
    /// Checklist'in atıf yaptığı **yardımcı sınıf/dosyalar** (ör. `PanelDisplay`,
    /// `SlugHelper`, `PagedListFooter`) gerçekten var olmalı. Checklist'in kendi bakım
    /// notu "dosya:satır yerine sınıf adı yaz" diyor — bu test o kuralın karşılığı:
    /// sınıf adı yazmanın değeri, ancak adın **doğrulanabilir** olmasıyla ortaya çıkar.
    /// </summary>
    [Theory]
    [InlineData("PanelDisplay", "KadirliApp.Web/Common/PanelDisplay.cs")]
    [InlineData("PanelMenu", "KadirliApp.Web/Common/PanelMenu.cs")]
    [InlineData("SlugHelper", null)]
    [InlineData("PagedListFooter", "mobile/lib/core/paging/paged_list_footer.dart")]
    [InlineData("ScrollableStateBody", null)]
    [InlineData("_Pagination.cshtml", "KadirliApp.Web/Views/Shared/_Pagination.cshtml")]
    [InlineData("_StatusBadge", "KadirliApp.Web/Views/Shared/_StatusBadge.cshtml")]
    [InlineData("_MenuLinks.cshtml", "KadirliApp.Web/Views/Shared/_MenuLinks.cshtml")]
    public void Checklist_ReferencedHelpers_StillExist(string symbol, string? expectedPath)
    {
        var doc = ChecklistText();
        doc.Should().Contain(symbol,
            "'{0}' checklist'te geçmiyor — madde silindiyse bu satırı da testten düşürün", symbol);

        if (expectedPath is null) return; // yalnız "checklist'te geçiyor mu" denetlenir

        File.Exists(Path.Combine(RepositoryRoot(), expectedPath)).Should().BeTrue(
            "checklist '{0}' diyor ama {1} yok — taşındıysa checklist'i güncelleyin",
            symbol, expectedPath);
    }

    /// <summary>
    /// Checklist'in kendi bakım kuralı: satırlarda <c>dosya.cshtml:129</c> gibi **satır
    /// numarası** verilmemeli — bir sonraki düzenlemede yanlış olur ve okuyucuyu yanlış
    /// yere gönderir. (Bakım bölümünde yazılı olan kuralın mekanik karşılığı.)
    /// </summary>
    [Fact]
    public void Checklist_DoesNotPinLineNumbers()
    {
        var doc = ChecklistText();

        var pinned = Regex.Matches(doc, @"\b[A-Za-z_][\w/\.]*\.(cs|cshtml|dart)\s*:\s*\d+")
            .Select(m => m.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        pinned.Should().BeEmpty(
            "checklist satır numarasına çivilenmiş atıf içeriyor: {0}. " +
            "Satır numaraları ilk düzenlemede çürür — sınıf/yardımcı adı yazın " +
            "(bkz. checklist'in kendi 'Bakım' bölümü).",
            string.Join(", ", pinned));
    }

    /// <summary>
    /// Bakım bölümü, checklist'in **testle kilitli olmayan** kısmının nasıl korunacağını
    /// anlatıyor. Kaybolursa bir sonraki düzenleyen kişi kuralları bilmez.
    /// </summary>
    [Fact]
    public void Checklist_KeepsItsMaintenanceSection()
        => ChecklistText().Should().Contain("## Bakım",
            "checklist'in bakım kuralları (satır numarası yazma, ortak bileşene dönen maddeyi silme) " +
            "belgede kalmalı");
}
