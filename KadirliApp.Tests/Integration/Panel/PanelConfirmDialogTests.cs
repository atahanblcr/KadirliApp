using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.2b — **onay penceresi gerçekten açılıyor mu?**
/// </summary>
/// <remarks>
/// 🐛 <b>Bu test bir gerçek hatadan doğdu.</b> Panelin geri alınamaz aksiyonları
/// <c>data-confirm</c> özniteliğiyle onay soruyor ve dinleyici <c>_Layout</c>'ta <b>tek
/// yerde</b>: <c>submit</c> olayında <b>form</b>un özniteliğine bakıyor. Öznitelik yanlışlıkla
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

    /// <summary>Toplu işlem çubuğu kendi <c>click</c> dinleyicisine sahip — bilinçli istisna.</summary>
    private static readonly IReadOnlySet<string> ButtonScopedByDesign =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "_BulkToolbar.cshtml" };

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
            "`data-confirm` yalnız <form> üzerinde çalışır (_Layout'taki dinleyici submit olayında " +
            "formun özniteliğine bakıyor). Başka bir etikete yazılırsa onay penceresi SESSİZCE hiç " +
            "açılmaz ve geri alınamaz aksiyon onaysız koşar. İhlaller: {0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Ters yön: dinleyicinin kendisi kaybolursa <b>bütün</b> onaylar sessizce ölür ve
    /// yukarıdaki test yine yeşil kalırdı (öznitelikler yerli yerinde çünkü).
    /// </summary>
    [Fact]
    public void Layout_StillListensForTheConfirmAttribute()
    {
        var layout = File.ReadAllText(
            Path.Combine(RepositoryRoot(), "KadirliApp.Web", "Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("hasAttribute('data-confirm')",
            "onay dinleyicisi _Layout'ta tek yerde; kaldırılırsa panelin TÜM onayları sessizce kaybolur");
        layout.Should().Contain("window.confirm(form.getAttribute('data-confirm'))");
    }
}
