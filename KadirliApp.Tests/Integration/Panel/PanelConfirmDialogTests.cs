using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.2b — **onay penceresi gerçekten açılıyor mu?**
/// </summary>
/// <remarks>
/// 🐛 <b>Bu test bir gerçek hatadan doğdu.</b> Panelin geri alınamaz aksiyonları
/// <c>data-confirm</c> özniteliğiyle onay soruyor ve dinleyici <b>tek yerde</b>
/// (12.9'dan beri <c>wwwroot/js/panel.js</c>, öncesinde <c>_Layout</c>'un satır içi bloğu): <c>submit</c> olayında <b>form</b>un özniteliğine bakıyor. Öznitelik yanlışlıkla
/// <c>&lt;button&gt;</c>'a yazıldığında hiçbir şey olmaz — kod doğru görünür, öznitelik
/// yerinde durur, Razor derlenir, hiçbir test kırılmaz ve <b>onay penceresi hiç açılmaz</b>.
/// Yani "geri alınamaz aksiyonda onay al" kuralı sessizce devre dışı kalır.
///
/// 12.2'nin "Uyarı kanalını dene" butonunda tam bu olmuştu ve 12.2b'nin canlı doğrulamasında
/// tesadüfen bulundu. Sessizliği, bu testin var olma sebebi: bir daha tesadüfe bırakılmasın.
///
/// ⚠️ <b>Tek istisna toplu işlem araç çubuğu</b> (<c>_BulkToolbar</c>): orada öznitelik
/// bilinçli olarak butondadır, çünkü aynı formda birden çok aksiyon butonu var ve her biri
/// farklı bir onay metni taşıyor. Onun <b>ayrı</b> bir dinleyicisi vardır (<c>click</c>
/// olayında, <c>[data-bulk-scope]</c> içinde) — yani istisna gerçek ve kanıtlı.
/// </remarks>
public class PanelConfirmDialogTests
{
    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }

    /// <summary>
    /// Öznitelik <b>gönderen butonda</b> olabilen görünümler — ikisi de kanıtlı istisna.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>_BulkToolbar</c> (11.18): aynı formda birden çok toplu aksiyon butonu var ve her
    /// biri farklı onay metni taşıyor.
    /// </para>
    /// <para>
    /// <c>_ModerationStatusField</c> (12.10): blok Düzenle formunun <b>içinde</b> duruyor ve
    /// HTML'de form iç içe olamıyor, bu yüzden Reddet/Arşivle butonları hedefi
    /// <c>formaction</c> ile değiştiriyor — yine tek formda üç aksiyon.
    /// </para>
    /// <para>
    /// 🔑 İstisna yalnız <i>kanıtlandığı</i> için güvenli: bu listeye ad yazmak yetmez,
    /// <see cref="PanelScript_StillListensForTheConfirmAttribute_AndIsLoadedByTheLayout"/>
    /// dinleyicinin butonu (<c>e.submitter</c>) gerçekten okuduğunu ayrıca denetliyor.
    /// Aksi hâlde liste bir muafiyet çöplüğüne dönerdi (11.15b'nin dersi).
    /// </para>
    /// </remarks>
    private static readonly IReadOnlySet<string> ButtonScopedByDesign =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "_BulkToolbar.cshtml",
            "_ModerationStatusField.cshtml"
        };

    [Fact]
    public void EveryConfirmAttribute_SitsOnAFormElement()
    {
        var viewsRoot = Path.Combine(RepositoryRoot(), "KadirliApp.Web", "Views");
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(viewsRoot, "*.cshtml", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name == "_Layout.cshtml" || ButtonScopedByDesign.Contains(name)) continue;

            var text = File.ReadAllText(file);

            // Özniteliği taşıyan etiketi bul: son açılan '<' ile öznitelik arasındaki ad.
            foreach (Match m in Regex.Matches(text, @"<(?<tag>[a-zA-Z][\w-]*)(?<attrs>(?:[^<>]|\n)*?)>", RegexOptions.Singleline))
            {
                if (!m.Groups["attrs"].Value.Contains("data-confirm", StringComparison.Ordinal)) continue;

                var tag = m.Groups["tag"].Value;
                if (!tag.Equals("form", StringComparison.OrdinalIgnoreCase))
                    offenders.Add($"{Path.GetRelativePath(viewsRoot, file)} → <{tag}>");
            }
        }

        offenders.Should().BeEmpty(
            "`data-confirm` yalnız <form> üzerinde çalışır (panel.js'teki dinleyici submit olayında " +
            "formun özniteliğine bakıyor). Başka bir etikete yazılırsa onay penceresi SESSİZCE hiç " +
            "açılmaz ve geri alınamaz aksiyon onaysız koşar. İhlaller: {0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Ters yön: dinleyicinin kendisi kaybolursa <b>bütün</b> onaylar sessizce ölür ve
    /// yukarıdaki test yine yeşil kalırdı (öznitelikler yerli yerinde çünkü).
    /// </summary>
    /// <remarks>
    /// 📌 <b>Faz 12.9'da dinleyicinin YERİ değişti</b>, sözleşme değişmedi: kod artık
    /// <c>_Layout</c>'un satır içi bloğunda değil <c>wwwroot/js/panel.js</c>'te.
    /// (12.9 satır içi 47 <c>on*=</c> işleyicisini taşırken ortak davranışı da harici
    /// dosyaya aldı.) 🐛 Bu testin <b>o taşımada kırmızıya dönmesi doğrudur ve testin
    /// kendisinin çalıştığının kanıtıdır</b> — yapılan şey testi gevşetmek değil,
    /// beklentiyi yeni tek sahibe taşımak oldu.
    ///
    /// 🔑 İddia iki parçalı ve ikisi de şart: dinleyici <b>panel.js'te var</b> <i>ve</i>
    /// panel.js <b>_Layout tarafından yükleniyor</b>. Yalnız birincisine bakan bir test,
    /// dosya var ama sayfaya hiç dâhil edilmiyorken de yeşil kalırdı — yani onaylar
    /// yine sessizce ölürdü.
    /// </remarks>
    [Fact]
    public void PanelScript_StillListensForTheConfirmAttribute_AndIsLoadedByTheLayout()
    {
        var script = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "KadirliApp.Web", "wwwroot", "js", "panel.js"));

        script.Should().Contain("getAttribute('data-confirm')",
            "onay dinleyicisi panel.js'te tek yerde; kaldırılırsa panelin TÜM onayları sessizce kaybolur");
        script.Should().Contain("window.confirm(message)");

        // Faz 12.10: buton üzerindeki onayın da okunduğunun kanıtı. Bu iddia olmasaydı
        // `e.submitter` dalı silindiğinde moderasyon bloğunun Reddet/Arşivle butonları
        // onaysız koşar ve yukarıdaki tarama (öznitelik yerinde çünkü) yeşil kalırdı.
        //
        // 🐛 İlk yazımda iddia yalnız `"e.submitter"` arıyordu ve BOZMA DENEMESİNDE YEŞİL
        // KALDI: değişkeni bırakıp okumayı silmek yetiyordu (`var submitter = e.submitter;`
        // satırı duruyor). Test artık okumanın KENDİSİNE bakıyor — 12.5/12.6'nın dersi:
        // yeşil kalan bir bozma denemesi "kural sağlam" değil, "test kuralı tutmuyor" demek.
        script.Should().Contain("submitter.getAttribute('data-confirm')",
            "gönderen butonun kendi onay metni GERÇEKTEN okunmalı — tek formda birden çok aksiyon var");

        var layout = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "KadirliApp.Web", "Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("~/js/panel.js",
            "panel.js her sayfaya _Layout üzerinden giriyor; bağlantı düşerse dinleyici " +
            "dosyada durur ama HİÇBİR sayfada çalışmaz");
    }
}
