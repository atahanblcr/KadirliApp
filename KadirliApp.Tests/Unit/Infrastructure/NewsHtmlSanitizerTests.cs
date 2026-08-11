using FluentAssertions;
using KadirliApp.Application.Features.News;
using KadirliApp.Infrastructure.News;
using Xunit;

namespace KadirliApp.Tests.Unit.Infrastructure;

/// <summary>
/// Faz 12.12 — <b>alım anındaki temizlik.</b>
///
/// 📊 Korpusta gerçekten bulunanlar (400 haber): <c>object</c> ×14, <c>video</c> ×4,
/// <b><c>form</c> ×2</b>. Sonuncusu bu testlerin var olma sebebi: temizlenmemiş bir gövde,
/// uygulamanın içinde <b>başka bir siteye veri gönderen</b> bir kutu demektir.
///
/// ⚠️ Temizleyici tek kapı değil: panelde <c>@Html.Raw</c> kullanılmaz (§7 madde 33) ve
/// istemci ikinci bir beyaz liste yazmaz.
/// </summary>
public class NewsHtmlSanitizerTests
{
    private readonly NewsHtmlSanitizer _sanitizer = new();

    [Theory]
    [InlineData("<script>alert('x')</script><p>Haber</p>", "alert")]
    [InlineData("<form action=\"https://kotu\"><input name=\"tc\"></form><p>Haber</p>", "<form")]
    [InlineData("<object data=\"x.swf\"></object><p>Haber</p>", "<object")]
    [InlineData("<video src=\"x.mp4\"></video><p>Haber</p>", "<video")]
    [InlineData("<iframe src=\"https://kotu\"></iframe><p>Haber</p>", "<iframe")]
    public void Sanitize_DropsDangerousElements(string html, string forbidden)
    {
        var clean = _sanitizer.Sanitize(html);

        clean.Should().NotContain(forbidden);
        clean.Should().Contain("Haber", "temizlik haberin kendisini götürmemeli");
    }

    /// <summary>
    /// 🔑 <c>script</c>/<c>style</c> <b>içeriğiyle</b> atılır. Yalnız etiketi atmak, sayfaya
    /// JS/CSS metnini <b>düz yazı</b> olarak basardı: kullanıcı "function(){…}" okur,
    /// kimse hata almaz.
    /// </summary>
    [Fact]
    public void Sanitize_DropsScriptContent_NotJustTheTag()
    {
        _sanitizer.Sanitize("<p>A</p><script>var gizli = 42;</script>")
            .Should().NotContain("gizli");

        _sanitizer.Sanitize("<style>.x{color:red}</style><p>A</p>")
            .Should().NotContain("color:red");
    }

    [Fact]
    public void Sanitize_DropsEventHandlersAndInlineStyle()
    {
        var clean = _sanitizer.Sanitize("<p onclick=\"kotu()\" style=\"font-size:80px\">Metin</p>");

        clean.Should().NotContain("onclick").And.NotContain("font-size");
        clean.Should().Contain("Metin");
    }

    [Fact]
    public void Sanitize_DropsJavascriptLinks_ButKeepsHttpOnes()
    {
        _sanitizer.Sanitize("<a href=\"javascript:alert(1)\">bağlantı</a>")
            .Should().NotContain("javascript:");

        _sanitizer.Sanitize("<a href=\"https://ornek.com\">bağlantı</a>")
            .Should().Contain("https://ornek.com");
    }

    /// <summary>Beyaz listedeki etiketler gerçekten kalmalı — aksi hâlde gövde düz metne döner.</summary>
    [Fact]
    public void Sanitize_KeepsTheAllowedStructure()
    {
        var clean = _sanitizer.Sanitize(
            "<p>Paragraf</p><h2>Ara başlık</h2><ul><li>madde</li></ul>" +
            "<figure><img src=\"https://ornek/1.webp\" alt=\"a\"><figcaption>alt yazı</figcaption></figure>" +
            "<blockquote>alıntı</blockquote><strong>kalın</strong>");

        foreach (var tag in new[] { "<p>", "<h2>", "<ul>", "<li>", "<figure>", "<img", "<figcaption>", "<blockquote>", "<strong>" })
            clean.Should().Contain(tag);
    }

    /// <summary>Sözleşme: girdi ne olursa olsun <b>fırlatmaz</b> — bozuk HTML koşuyu düşürmemeli.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<<>><p>yarım")]
    public void Sanitize_NeverThrows(string? html)
    {
        _sanitizer.Invoking(s => s.Sanitize(html)).Should().NotThrow();
        _sanitizer.Invoking(s => s.ToPlainText(html)).Should().NotThrow();
    }

    [Fact]
    public void ToPlainText_StripsTagsAndDecodesEntities()
    {
        var text = _sanitizer.ToPlainText("<p>Osmaniye&#8217;de   kaza</p><p>ikinci</p>");

        text.Should().Be("Osmaniye’de kaza ikinci");
    }

    [Fact]
    public void ToPlainText_DoesNotLeakScriptSource()
    {
        _sanitizer.ToPlainText("<p>Haber</p><script>gizliDeger()</script>")
            .Should().NotContain("gizliDeger");
    }

    /// <summary>
    /// Politika Application'da yaşıyor ve gerçeklemenin ona uyduğu burada kilitli:
    /// kütüphane güncellemesi varsayılan listeyi genişletirse test kırılmalı.
    /// </summary>
    [Fact]
    public void Policy_AndImplementation_AgreeOnTheForbiddenSet()
    {
        foreach (var tag in NewsHtmlPolicy.DroppedWithContent)
            _sanitizer.Sanitize($"<{tag}>içerik</{tag}><p>Haber</p>")
                .Should().NotContain($"<{tag}", "politikada 'içeriğiyle atılacak' yazan etiket kalmamalı");

        foreach (var tag in new[] { "table", "span", "div" })
            _sanitizer.Sanitize($"<{tag}>metin</{tag}>")
                .Should().NotContain($"<{tag}", "beyaz listede olmayan etiket kalmamalı (içeriği kalabilir)");
    }
}
