using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.19b — <b>yorumlardaki atıflar hâlâ var olan şeylere mi işaret ediyor?</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu dosya bir denetim bulgusundan doğdu (14 Ağu 2026).</b> <c>User.cs</c>'teki bir
/// yorum, <b>var olmayan bir teste</b> atıf yapıyordu — ne o sınıf ne o metot vardı
/// (⚠️ atfın kendisi burada bilerek <b>yazılmadı</b>: bu dosyanın taraması kendi belgesini
/// de okur ve çürük örneği anmak testi kendi kendine kırardı; 12.16'da bir Razor
/// <i>yorumundaki</i> betik etiketinin CSP taramasını kırmasının birebir aynısı) —
/// ve o atfın arkasındaki iddia <b>ölçümün tam tersiydi</b>. Bir migration'ın
/// bütün varlık sebebi o ölçümdü; yorumu okuyan biri migration'ı "gereksiz" sayıp silseydi
/// 13 kullanıcının hepsi bildirimden sessizce düşerdi.
/// </para>
/// <para>
/// ⚠️ <b>BU TESTİN NE YAPAMADIĞINI ÖNCE SÖYLEMEK GEREKİYOR:</b> burada yakalanan şey
/// <i>sarkan işaretçidir</i> — <b>yanlış iddiayı yakalayamaz.</b> Yukarıdaki bulgunun
/// tehlikeli yarısı atfın kırık olması değil, cümlenin yalan söylemesiydi ve bunu hiçbir
/// otomatik denetim göremez. Bu dosyanın dürüstlüğü, madde 67'nin
/// <c>SmokeCheck_…_VacuousOnAFreshDatabase</c> adlandırmasının aynısı: kapsamı adında ve
/// belgesinde açıkça yazılı olsun ki kimse "yorumlar denetleniyor" sanmasın.
/// </para>
/// <para>
/// 📌 <b>Kapsam DİZİNDEN türetilir</b> (<c>**/*.cs</c>), elle dosya listesi tutulmaz —
/// yarın eklenen bir dosya kendiliğinden taranır. <c>bin/</c>, <c>obj/</c> ve
/// <c>Migrations/</c> dışarıda (üretilmiş kod).
/// </para>
/// <para>
/// ➕ <b>İkinci ayak plan dışıdır ve bu projede birincisinden değerlidir:</b> yorumlar
/// tip adından çok <b>dosya yolu</b> anıyor (<i>"tek sahibi
/// <c>core/router/app_nav.dart</c>"</i>, <i>"kilit
/// <c>Integration/Panel/PanelNewsTests.cs</c>"</i>). Bir dosya taşındığında ya da
/// silindiğinde o yollar <b>sessizce</b> çürür ve derleyici hiçbir şey söylemez —
/// üstelik bu projede o yollar "kuralın sahibi kim?" sorusunun tek cevabı.
/// </para>
/// </remarks>
public class CommentReferenceTests
{
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    /// <summary>Taranan kaynak dosyalar — <b>dizinden türetilir.</b></summary>
    private static IReadOnlyList<string> SourceFiles()
    {
        var root = RepositoryRoot();

        var files = Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        files.Should().HaveCountGreaterThan(300,
            "kaynak taraması beklenenden az dosya buldu — tarama kırıldıysa bu dosyadaki " +
            "her iddia sessizce vakum olur");

        return files;
    }

    // ── 1) Test atıfları ────────────────────────────────────────────────────────

    /// <summary>
    /// Yorumlarda geçen <c>&lt;c&gt;BirşeyTests.MetotAdı&lt;/c&gt;</c> biçimindeki atıflar
    /// gerçekten var olan bir test sınıfına ve metoda işaret etmeli.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Neden özellikle test atıfları:</b> bu projede "bu kural nerede kilitli?"
    /// sorusunun cevabı neredeyse her zaman bir test adıdır. O ad çürüdüğünde okuyucu
    /// kuralın kilitli <i>olduğunu</i> sanar ve <b>aramaktan vazgeçer</b> — yani çürük atıf,
    /// atıf olmamasından kötüdür.
    /// </remarks>
    [Fact]
    public void EveryTestReferenceInAComment_PointsAtARealTest()
    {
        var testTypes = typeof(CommentReferenceTests).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("Tests", StringComparison.Ordinal))
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var reference = new Regex(@"<c>(?<type>\w*Tests)\.(?<member>\w+)(?:\(\))?</c>", RegexOptions.Compiled);

        var offenders = new List<string>();
        var checkedReferences = 0;

        foreach (var file in SourceFiles())
        {
            foreach (Match match in reference.Matches(File.ReadAllText(file)))
            {
                checkedReferences++;

                var typeName = match.Groups["type"].Value;
                var memberName = match.Groups["member"].Value;

                if (!testTypes.TryGetValue(typeName, out var candidates))
                {
                    offenders.Add($"{Rel(file)} → {typeName} (BÖYLE BİR TEST SINIFI YOK)");
                    continue;
                }

                var hasMember = candidates.Any(t => t.GetMember(
                    memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length > 0);

                if (!hasMember)
                    offenders.Add($"{Rel(file)} → {typeName}.{memberName} (sınıf var, ÜYE YOK)");
            }
        }

        checkedReferences.Should().BeGreaterThan(0,
            "hiç test atfı bulunamadıysa regex kırılmış demektir — test sessizce anlamsızlaşır");

        offenders.Should().BeEmpty(
            "yorumdaki test atıfları gerçek olmalı: çürük bir atıf, okuyucuya kuralın " +
            "kilitli OLDUĞUNU söyleyip aramaktan vazgeçirir. Kırık atıflar: {0}",
            string.Join(" | ", offenders));
    }

    // ── 2) Tip.Üye atıfları ─────────────────────────────────────────────────────

    /// <summary>
    /// <c>&lt;see cref="Tip.Üye"/&gt;</c> atıfları — <b>derleyici bunları denetlemiyor.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Sezgiye ters ve ölçüldü:</b> <c>cref</c> yalnız XML belge üretimi açıkken
    /// (<c>GenerateDocumentationFile</c>) çözülür; bu çözümde hiçbir projede açık değil,
    /// yani <c>&lt;see cref="OlmayanTip"/&gt;</c> <b>uyarı bile üretmiyor</b>. "Derleyici
    /// zaten bakıyor" varsayımı bu depoda yanlış.
    /// <para>
    /// 📌 Yalnız <b>bizim</b> assembly'lerimizde çözülebilen tipler denetlenir: dış tiplerin
    /// (<c>IHostEnvironment</c>, <c>DateTime</c>) üyelerini burada aramak, çalışma anında
    /// yüklü olmayan bir assembly yüzünden <b>yanlış kırmızı</b> üretirdi.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryQualifiedCrefInAComment_PointsAtARealMember()
    {
        var ourTypes = OurTypesBySimpleName();

        // 🐛 BOZMA TURUNDA BULUNDU (12.19b): ilk yazımda desen `(?<type>\w+)` idi ve
        // `<see cref="Foo{T,U}.Bar"/>` biçimindeki JENERİK atıflara HİÇ uymuyordu —
        // yani kırık bir jenerik atıf eklendiğinde test yeşil kalıyordu. Tip adının
        // ardından gelen `{…}` bloğu artık isteğe bağlı olarak yutuluyor.
        var reference = new Regex(
            @"<see\s+cref\s*=\s*""(?<type>\w+)(?:\{[^}]*\})?\.(?<member>\w+)""",
            RegexOptions.Compiled);

        var offenders = new List<string>();
        var checkedReferences = 0;

        foreach (var file in SourceFiles())
        {
            foreach (Match match in reference.Matches(File.ReadAllText(file)))
            {
                var typeName = match.Groups["type"].Value;
                var memberName = match.Groups["member"].Value;

                if (!ourTypes.TryGetValue(typeName, out var candidates)) continue;   // dış tip

                checkedReferences++;

                var hasMember = candidates.Any(t => t.GetMember(
                    memberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Length > 0);

                if (!hasMember)
                    offenders.Add($"{Rel(file)} → {typeName}.{memberName}");
            }
        }

        checkedReferences.Should().BeGreaterThan(0,
            "hiç nitelikli cref bulunamadıysa regex kırılmış demektir");

        offenders.Should().BeEmpty(
            "yorumdaki `<see cref=\"Tip.Üye\"/>` atıfları gerçek üyelere işaret etmeli " +
            "(derleyici bunları DENETLEMİYOR — XML belge üretimi kapalı). Kırıklar: {0}",
            string.Join(" | ", offenders));
    }

    private static Dictionary<string, List<Type>> OurTypesBySimpleName()
    {
        var assemblies = new[]
        {
            typeof(KadirliApp.Domain.Entities.User).Assembly,
            typeof(KadirliApp.Application.DependencyInjection).Assembly,
            typeof(KadirliApp.Infrastructure.DependencyInjection).Assembly,
            typeof(CommentReferenceTests).Assembly
        };

        // ⚠️ Jenerik tiplerin `Type.Name`'i ariteyi taşır (`DevelopmentOnlyBehavior`2`);
        // yorumda ise `DevelopmentOnlyBehavior{TRequest,TResponse}` yazılır. Ters tik
        // soneki düşürülmezse jenerik tipler sözlükte HİÇ bulunamaz ve o atıflar
        // "dış tip" sanılıp sessizce atlanırdı — deliğin ikinci yarısı buydu.
        return assemblies
            .SelectMany(a => a.GetTypes())
            .GroupBy(t => t.Name.Split('`')[0], StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
    }

    // ── 3) Dosya yolu atıfları (plan dışı ek) ───────────────────────────────────

    /// <summary>
    /// ➕ <b>12.19b'nin plan dışı ayağı:</b> yorumlarda anılan <b>dosya yolları</b> hâlâ
    /// var olmalı.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔑 <b>Neden bu, tip atıflarından değerli:</b> bu projede bir kuralın "tek sahibi"
    /// çoğunlukla bir <i>dosya</i> olarak yazılıyor (<c>core/router/app_nav.dart</c> ·
    /// <c>wwwroot/js/panel.js</c> · <c>features/transport/application/operating_days.dart</c>).
    /// Üstelik bu yolların çoğu <b>mobil tarafta</b>, yani C# derleyicisinin görebileceği
    /// hiçbir şeyle bağlı değil: dosya taşındığında ya da silindiğinde atıf sessizce çürür
    /// ve <b>kuralın sahibini soran bir sonraki kişi onu bulamaz.</b>
    /// </para>
    /// <para>
    /// ⚠️ Ölçüt "yol depodaki bir dosyanın <b>sonekidir</b>": yorumlar çoğunlukla kısmi yol
    /// yazıyor (<c>Integration/Panel/PanelClient.cs</c>). Tam yol araması yanlış kırmızı
    /// üretirdi; yalnız dosya adına bakmak ise taşımaları kaçırırdı.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryFilePathInAComment_StillExists()
    {
        var root = RepositoryRoot();

        var repoFiles = Directory
            .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .ToList();

        // `<c>…</c>` içindeki, en az bir '/' taşıyan ve bilinen bir uzantıyla biten yollar.
        var pathReference = new Regex(
            @"<c>(?<path>[\w./-]*/[\w.-]+\.(?:cs|dart|js|css|cshtml|json|md|yml|yaml))</c>",
            RegexOptions.Compiled);

        var offenders = new List<string>();
        var checkedPaths = 0;

        foreach (var file in SourceFiles())
        {
            foreach (Match match in pathReference.Matches(File.ReadAllText(file)))
            {
                var path = match.Groups["path"].Value.TrimStart('/');
                checkedPaths++;

                if (!repoFiles.Any(f => f.EndsWith(path, StringComparison.Ordinal)))
                    offenders.Add($"{Rel(file)} → {path}");
            }
        }

        checkedPaths.Should().BeGreaterThan(0,
            "hiç dosya yolu atfı bulunamadıysa regex kırılmış demektir");

        offenders.Should().BeEmpty(
            "yorumda anılan dosya yolu depoda YOK. Bu projede bir kuralın 'tek sahibi' " +
            "çoğu zaman bir dosya adıdır; yol çürüdüğünde kuralı arayan kişi onu bulamaz " +
            "ve derleyici hiçbir şey söylemez. Kırık yollar: {0}",
            string.Join(" | ", offenders.Distinct()));
    }

    private static string Rel(string file) =>
        Path.GetRelativePath(RepositoryRoot(), file).Replace('\\', '/');
}
