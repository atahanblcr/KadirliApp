// `extern alias WebPanel` şart: Api ve Web'in ikisi de global namespace'te `Program`
// üretiyor (ARCHITECTURE.md §8 "bilinen test tuzakları").
extern alias WebPanel;

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.9 — **panel dış origine bağlı olamaz.**
/// </summary>
/// <remarks>
/// <para>
/// 12.9 öncesinde panel dört üçüncü taraf origin'den kod ve stil çekiyordu
/// (<c>cdn.tailwindcss.com</c>, <c>cdnjs.cloudflare.com</c>, <c>fonts.googleapis.com</c>,
/// <c>unpkg.com</c>). Bu bir "kozmetik" sorun değildi: <c>unpkg</c>'den gelen Leaflet
/// <b>10 formda</b> kullanılıyor ve gelmediğinde <c>L.</c> çağrıları <c>undefined</c>
/// üzerinde patlıyordu — yönetici <b>boş bir kutu</b> görüyor, koordinat seçemiyor ve
/// ekranda <b>hiçbir hata mesajı çıkmıyordu</b>.
/// </para>
/// <para>
/// 🔑 <b>Neden elle liste tutulmuyor.</b> "Şu dört origin olmasın" diyen bir test,
/// beşinci bir CDN eklendiği gün sessizce yeşil kalırdı. Test bunun yerine
/// <c>Views/**/*.cshtml</c> dosyalarını <b>tarar</b>: kaynak yükleyen hiçbir
/// öznitelikte mutlak URL olmamalı.
/// </para>
/// <para>
/// ⚠️ Bu denetim <b>derleme zamanına</b> ait, çalışma anına değil — ve bu bilinçli:
/// Razor görünümleri derlenip assembly'ye gömülüyor, yani yayında <c>.cshtml</c>
/// dosyalarının bulunması garanti değil. Çalışma anında dosya tarayan bir kapı
/// yayında <b>sıfır dosya bulur ve yeşil geçer</b>. Çalışma anındaki karşılığı
/// <c>PanelAssetGuard</c>: o, <i>gözlenebilir</i> olanı denetler (türetilmiş
/// varlıklar yerinde mi).
/// </para>
/// <para>
/// 📌 <b>Tek bilinçli istisna harita kareleri</b> (<c>tile.openstreetmap.org</c>):
/// bir dünya haritasının görüntüsü self-host edilemez. Ama fark kritik ve testin
/// istisnayı <b>dar</b> tutmasının sebebi bu: Leaflet gelmezse seçici <b>tamamen
/// ölür</b>; kareler gelmezse harita gri kalır ve <b>koordinat seçimi çalışır</b>.
/// Bu yüzden istisna yalnız JS <i>içindeki</i> tile URL'sinde geçerli, bir
/// <c>src=</c>/<c>href=</c> özniteliğinde değil.
/// </para>
/// </remarks>
public class PanelExternalOriginTests
{
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    private static string ViewsRoot() =>
        Path.Combine(RepositoryRoot(), "KadirliApp.Web", "Views");

    private static IReadOnlyList<string> ViewFiles() =>
        Directory.GetFiles(ViewsRoot(), "*.cshtml", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

    private static string Relative(string path) =>
        Path.GetRelativePath(ViewsRoot(), path).Replace('\\', '/');

    /// <summary>
    /// <b>Alt kaynak</b> yükleyen öznitelikler — yani tarayıcının sayfayı çizerken
    /// indirmek zorunda olduğu şeyler.
    /// </summary>
    /// <remarks>
    /// 🐛 <b>İlk yazımda bu ifade her <c>href</c>'i yakalıyordu ve test yanlış bir şeyi
    /// kırmızıya çeviriyordu:</b> <c>Home/Index.cshtml</c>'deki
    /// <c>&lt;a href="https://learn.microsoft.com/…"&gt;</c> bağlantısı. Ama bir
    /// <c>&lt;a&gt;</c> bağlantısı alt kaynak <b>değildir</b>: tıklanana kadar hiçbir
    /// şey indirilmez, ağ kesikken sayfa yine çalışır ve CSP de onu engellemez.
    /// Ayrımı kaybetmek testi gürültüye boğardı ve gürültülü bir yapısal test,
    /// bir sonraki kişinin gevşetmeye çalışacağı testtir.
    ///
    /// Bu yüzden <c>href</c> yalnız <c>&lt;link&gt;</c> üzerinde, <c>src</c> ise her
    /// etikette denetleniyor. <c>action</c>/<c>formaction</c> de bilerek yok: onlar
    /// gönderim hedefidir ve CSP'de <c>form-action 'self'</c> zaten kapatıyor.
    /// </remarks>
    private static readonly Regex SubresourceAttribute = new(
        """(?:<link\b[^>]*?\bhref|\bsrc)\s*=\s*(?<quote>["'])(?<value>[^"']*)\k<quote>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── 1) Hiçbir görünüm dış origin'den kaynak yüklemez ──────────────────────

    /// <summary>
    /// 🔴 <b>12.9'un kalıcı kilidi.</b> Bu test kırmızıya döndüğünde yapılacak şey
    /// istisna listesine satır eklemek DEĞİL: varlığı <c>wwwroot/lib</c>'e almak.
    /// </summary>
    [Fact]
    public void NoView_LoadsAResourceFromAnExternalOrigin()
    {
        var offenders = new List<string>();

        foreach (var file in ViewFiles())
        {
            var text = File.ReadAllText(file);

            foreach (Match match in SubresourceAttribute.Matches(text))
            {
                var value = match.Groups["value"].Value.Trim();

                // Protokol-göreli (`//cdn…`) de dış origin'dir ve gözden kaçmaya
                // en açık biçimidir — "https" aramayan bir test onu görmez.
                var isAbsolute =
                    value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("//", StringComparison.Ordinal);

                if (isAbsolute)
                {
                    offenders.Add($"{Relative(file)} → {value}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "panel dış origine bağlı olamaz: bozulursa hata VERMEZ, yalnız ağın iyi " +
            "olduğu her yerde çalışmaya devam eder ve tam olarak kötü koşulda " +
            "(belediyenin kısıtlı ağı, CDN kesintisi) kırılır. Harita seçicide " +
            "belirti bile yok: boş kutu, log yok, hata yok. " +
            "Çözüm varlığı wwwroot/lib'e almaktır, buraya istisna yazmak değil.");
    }

    /// <summary>
    /// 🔴 <b>Faz A bozma turunun bulgusu (13 Ağu 2026): taramanın KAPSAMI delikti.</b>
    ///
    /// <para>
    /// Yukarıdaki tarama yalnız <c>Views/**/*.cshtml</c>'i okuyor. Ölçüldü:
    /// <c>wwwroot/css/panel.css</c>'in başına
    /// <c>@import url(https://fonts.googleapis.com/…)</c> eklendiğinde <b>üç ayak da yeşil
    /// kaldı</b> — kaynak taraması (görünüm değil), canlı CSP testi (başlık doğru, ihlal
    /// yalnız tarayıcı konsolunda) ve varlık kapısı (dosya var ve boş değil). Yani panel,
    /// 12.9'un <b>tam olarak yerelleştirdiği</b> yazı tipine sessizce geri bağlanabilirdi.
    /// </para>
    ///
    /// <para>
    /// 🔑 12.11'in dersi burada da geçerli — çözüm "bir dosya adı daha ekle" değil,
    /// <b>kapsamı türetmek</b>: <c>wwwroot</c> altındaki <b>bizim yazdığımız</b> her
    /// varlık taranır (dizinden okunur, elle liste yok).
    /// </para>
    ///
    /// <para>
    /// 📌 <c>wwwroot/lib</c> **bilinçli olarak dışarıda**: orası üçüncü taraf dosyaların
    /// <i>vendor</i> kopyası ve içeriğini biz yazmıyoruz (lisans başlıkları, kaynak harita
    /// yorumları dış adres içerir). Risk *bizim yazdığımız bir başvuru*; oradaki dosyaların
    /// yerinde ve boş olmadığı ayrıca denetleniyor.
    /// ⚠️ Yorum satırları eleniyor: <c>panel.css</c> Tailwind'in lisans başlığında
    /// <c>https://tailwindcss.com</c> taşıyor ve o bir **yükleme** değil.
    /// </para>
    /// </summary>
    [Fact]
    public void NoCommittedPanelAsset_LoadsAResourceFromAnExternalOrigin()
    {
        var wwwroot = Path.Combine(RepositoryRoot(), "KadirliApp.Web", "wwwroot");
        var lib = Path.Combine(wwwroot, "lib") + Path.DirectorySeparatorChar;

        var assets = Directory
            .GetFiles(wwwroot, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.StartsWith(lib, StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        assets.Should().NotBeEmpty(
            "kapsam dizinden TÜRETİLİYOR — türetme çalışmıyorsa bu test hiçbir şey denetlemiyor");

        // CSS: `url(https://…)` ve `@import "https://…"` · JS: dize hâlinde dış origin.
        var externalReference = new Regex(
            @"(?<css>url\(\s*['""]?\s*(https?:)?//)|(?<import>@import\s+(url\(\s*)?['""]?\s*(https?:)?//)|(?<js>['""`](https?:)?//[a-z0-9.-]+\.[a-z]{2,})",
            RegexOptions.IgnoreCase);

        var offenders = new List<string>();

        foreach (var file in assets)
        {
            // Yorumları at: lisans başlıkları ve kaynak harita satırları dış adres taşır
            // ama hiçbiri bir YÜKLEME değildir.
            var text = Regex.Replace(File.ReadAllText(file), @"/\*.*?\*/", " ", RegexOptions.Singleline);
            text = Regex.Replace(text, @"(?m)^\s*//.*$", " ");

            foreach (Match match in externalReference.Matches(text))
                offenders.Add($"{Path.GetRelativePath(wwwroot, file).Replace('\\', '/')} → {match.Value.Trim()}");
        }

        offenders.Should().BeEmpty(
            "panelin KENDİ varlıkları da dış origine bağlanamaz (§7 madde 51). Görünümlerdeki " +
            "başvuruyu kesip aynı bağımlılığı panel.css'e bir @import olarak yazmak, 12.9'un " +
            "kapattığı deliği geri açar — üstelik daha sinsi biçimde: yönetici hiçbir hata " +
            "görmez, yalnız yazı tipi/harita sessizce gelmez. Çözüm varlığı wwwroot/lib'e " +
            "almaktır. Bulunanlar: {0}", string.Join(", ", offenders));
    }

    // ── 2) Yerel varlık başvuruları gerçekten var ─────────────────────────────

    /// <summary>
    /// Dış origin'i kesmek tek başına yetmez: <c>~/lib/leaflet/leaflet.js</c> yazıp
    /// dosyayı depoya koymamak <b>aynı hasarı</b> üretir — üstelik bu kez CDN kesintisi
    /// beklemeye bile gerek kalmadan, her zaman.
    /// </summary>
    [Fact]
    public void EveryLocalAssetReference_ExistsOnDisk()
    {
        var wwwroot = Path.Combine(RepositoryRoot(), "KadirliApp.Web", "wwwroot");
        var missing = new List<string>();

        foreach (var file in ViewFiles())
        {
            var text = File.ReadAllText(file);

            foreach (Match match in SubresourceAttribute.Matches(text))
            {
                var value = match.Groups["value"].Value.Trim();

                // Yalnız `~/…` biçimindeki statik varlık başvuruları. Rota
                // bağlantıları (`/Dashboard/Index`), tag-helper'la üretilenler
                // ve Razor ifadeleri kapsam dışı.
                if (!value.StartsWith("~/", StringComparison.Ordinal)) continue;
                if (value.Contains('@')) continue;

                var relative = value[2..].Replace('/', Path.DirectorySeparatorChar);
                if (!File.Exists(Path.Combine(wwwroot, relative)))
                {
                    missing.Add($"{Relative(file)} → {value}");
                }
            }
        }

        missing.Should().BeEmpty(
            "görünümün başvurduğu her yerel varlık depoda olmalı — yoksa tarayıcı " +
            "404 alır, sayfa yine açılır ve eksiklik yalnız davranıştan anlaşılır");
    }

    // ── 3) Satır içi olay işleyicisi kalmamalı ────────────────────────────────

    /// <summary>
    /// 🔴 <b>CSP'nin bedeli burada korunuyor.</b> Panelin CSP'sinde
    /// <c>script-src</c> için <c>'unsafe-inline'</c> <b>yok</b>; nonce ise yalnız
    /// <c>&lt;script&gt;</c> <b>bloklarını</b> kapsar, <c>onclick=</c> gibi
    /// <b>öznitelikleri kapsamaz</b>.
    ///
    /// Yani bugün eklenecek tek bir <c>onclick=</c>, o buton <b>sessizce çalışmayan</b>
    /// bir butona döner: tıklanır, hiçbir şey olmaz, sunucuda iz kalmaz ve yalnız
    /// tarayıcı konsolunda bir CSP ihlali görünür — yöneticinin bakmadığı yer.
    /// ("İşlevsiz buton yok" kuralının panel karşılığı.)
    ///
    /// 12.9'da <b>47</b> işleyici taşındı; bu test sayının geri tırmanmasını engeller.
    /// </summary>
    [Fact]
    public void NoView_UsesAnInlineEventHandlerAttribute()
    {
        // Yalnız ÖZNİTELİK biçimi (`onclick="…"`). JS içindeki `el.onclick = …`
        // ataması CSP'yi ilgilendirmez ve bilinçli olarak kapsam dışı.
        var inlineHandler = new Regex(
            """<[^>]*?\son(?:click|change|submit|input|load|focus|blur|keyup|keydown|mouseover)\s*=\s*["']""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var offenders = new List<string>();

        foreach (var file in ViewFiles())
        {
            if (inlineHandler.IsMatch(File.ReadAllText(file)))
            {
                offenders.Add(Relative(file));
            }
        }

        offenders.Should().BeEmpty(
            "CSP `script-src`'ında 'unsafe-inline' YOK — satır içi bir olay işleyicisi " +
            "tarayıcı tarafından çalıştırılmaz ve buton sessizce ölür. " +
            "Davranışı wwwroot/js/panel.js'teki delege dinleyicilere ya da görünümün " +
            "kendi nonce'lu <script> bloğuna taşıyın.");
    }

    // ── 4) Her satır içi <script> bloğu nonce taşır ───────────────────────────

    /// <summary>
    /// Nonce'suz bir satır içi blok da aynı sınıf hasarı verir: sayfa açılır,
    /// blok <b>hiç çalışmaz</b>. Bu, üçüncü maddenin aynası — biri özniteliği,
    /// diğeri bloğu kapatıyor.
    /// </summary>
    [Fact]
    public void EveryInlineScriptBlock_CarriesTheCspNonce()
    {
        var openingTag = new Regex("<script(?<attrs>[^>]*)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var offenders = new List<string>();

        foreach (var file in ViewFiles())
        {
            var text = File.ReadAllText(file);

            foreach (Match match in openingTag.Matches(text))
            {
                var attrs = match.Groups["attrs"].Value;

                // Harici dosya (src=…) nonce istemez: CSP onu `'self'` üzerinden geçirir.
                if (attrs.Contains("src=", StringComparison.OrdinalIgnoreCase)) continue;
                if (attrs.Contains("nonce", StringComparison.OrdinalIgnoreCase)) continue;

                offenders.Add($"{Relative(file)} → <script{attrs}>");
            }
        }

        offenders.Should().BeEmpty(
            "satır içi her <script> bloğu nonce=\"@Context.Items[\"csp-nonce\"]\" taşımalı — " +
            "taşımazsa CSP onu engeller ve blok sessizce hiç çalışmaz");
    }

    // ── 5) Türetilmiş varlıklar depoda ve boş değil ───────────────────────────

    /// <summary>
    /// <c>panel.css</c> ve <c>wwwroot/lib</c> altındaki üçüncü taraf dosyalar
    /// <b>commit edilir</b>: depoyu klonlayan biri <c>npm install</c> çalıştırmadan
    /// paneli açabilmeli. Bu testin ölçüsü <c>PanelAssetGuard.RequiredAssets</c> —
    /// yani çalışma anındaki kapı ile derleme zamanındaki denetim <b>aynı listeden</b>
    /// besleniyor; ayrı listeler tutulsaydı biri güncellenip diğeri unutulurdu.
    /// </summary>
    [Fact]
    public void DerivedPanelAssets_AreCommittedAndNonEmpty()
    {
        var wwwroot = Path.Combine(RepositoryRoot(), "KadirliApp.Web", "wwwroot");

        foreach (var (path, breaks) in WebPanel::KadirliApp.Web.Common.PanelAssetGuard.RequiredAssets)
        {
            var full = Path.Combine(wwwroot, path.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(full);

            info.Exists.Should().BeTrue($"wwwroot/{path} depoda olmalı — yoksa {breaks}");
            info.Length.Should().BeGreaterThan(0, $"wwwroot/{path} boş olmamalı — boşsa {breaks}");
        }
    }

    /// <summary>
    /// 🐛 <b>Tarama sırasında bulunan tuzak.</b> Tailwind yalnız <b>gördüğü</b>
    /// sınıfları üretir, CDN'in tarayıcı-içi JIT'i ise çalışma anında DOM'a bakıyordu —
    /// yani sınıfın nerede yazıldığı 12.9'a kadar hiç önemli değildi.
    ///
    /// Bu projede rozet/buton renkleri <b>.cshtml'de değil C#'ta</b> yaşıyor
    /// (<c>PanelDisplay</c>, <c>PowerOutagePhase</c>, <c>BulkToolbarViewModel</c>).
    /// <c>content</c> taraması yalnız <c>Views/**</c> olsaydı panelin <b>bütün durum
    /// rozetleri renksiz</b> kalırdı — ve ne derleyici, ne test, ne log bunu söylerdi.
    /// </summary>
    [Fact]
    public void CompiledCss_ContainsBadgeClassesThatLiveInCSharpNotInViews()
    {
        var web = Path.Combine(RepositoryRoot(), "KadirliApp.Web");
        var css = File.ReadAllText(Path.Combine(web, "wwwroot", "css", "panel.css"));

        // Tailwind'in renk yardımcıları: `bg-amber-100`, `text-slate-700`…
        //
        // 🐛 Baştaki `(?<![:\w-])` bir kırmızı testten doğdu: `\b` ile başlayan ilk
        // yazım `hover:bg-amber-700` içindeki `bg-amber-700`'ü de yakalıyordu, çünkü
        // `:` bir sözcük sınırı. Ama VARYANTLI bir yardımcı CSS'e `.bg-amber-700`
        // olarak DEĞİL, kaçırılmış seçici olarak (`.hover\:bg-amber-700:hover`)
        // çıkıyor — yani test var olan ve doğru üretilmiş bir sınıfı "yok" sanıyordu.
        // Varyantlılar bilerek kapsam dışı: düz metin araması onlarda kırılgan olur.
        var utility = new Regex(@"(?<![:\w-])(?:bg|text|border|ring)-[a-z]+-\d{2,3}\b", RegexOptions.Compiled);

        var inCSharp = Directory
            .GetFiles(web, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(p => utility.Matches(File.ReadAllText(p)).Select(m => m.Value))
            .ToHashSet(StringComparer.Ordinal);

        var inViews = ViewFiles()
            .SelectMany(p => utility.Matches(File.ReadAllText(p)).Select(m => m.Value))
            .ToHashSet(StringComparer.Ordinal);

        // 🔑 YALNIZ C#'ta geçenler — liste elle tutulmuyor, TÜRETİLİYOR.
        var exclusiveToCSharp = inCSharp.Except(inViews).OrderBy(c => c, StringComparer.Ordinal).ToList();

        // 🐛 Bu iddia bir bozma denemesinde bulundu. İlk yazımda dört sınıf ELLE
        // seçilmişti ve ikisi (`bg-amber-100`, `bg-red-200`) meğer görünümlerde de
        // geçiyordu — yani o ikisi, `content`ten `**/*.cs` düşse bile YEŞİL kalırdı.
        // Test "kural sağlam" der ama kuralı tutmuyordu. Aşağıdaki satır o boşluğun
        // kapısı: küme boşalırsa test hiçbir şey kanıtlamıyor demektir.
        exclusiveToCSharp.Should().NotBeEmpty(
            "bu test yalnızca C# dosyalarında yaşayan sınıflar VARSA bir şey kanıtlar; " +
            "küme boşsa denetim sessizce anlamsızlaşmıştır");

        foreach (var cls in exclusiveToCSharp)
        {
            css.Should().Contain($".{cls}",
                $"'{cls}' hiçbir .cshtml'de geçmiyor, yalnız C#'ta (PanelDisplay / " +
                "PowerOutagePhase / BulkToolbarViewModel). tailwind.config.js'in content " +
                "listesinden Common/**/*.cs ya da Models/**/*.cs düşerse bu sınıf ÜRETİLMEZ " +
                "ve ilgili rozet SESSİZCE renksiz kalır — ne derleyici, ne test, ne log söyler");
        }
    }
}
