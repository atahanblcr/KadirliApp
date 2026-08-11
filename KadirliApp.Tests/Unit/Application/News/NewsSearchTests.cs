using FluentAssertions;
using KadirliApp.Application.Features.News;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.13 — haber aramasının deseni (12.12 sonrası denetim, bulgu 4).
/// </summary>
/// <remarks>
/// 🔑 İki ayrı iddia var ve ikisi de <b>sessiz</b> hasar sınıfına ait:
/// <list type="number">
///   <item><b>Joker kaçışı.</b> Kaçırılmayan bir <c>%</c> aramayı bozmaz, <b>yanlış</b>
///         çalıştırır: vatandaş "%" yazar, bütün arşiv döner ve kimse hata almaz.</item>
///   <item><b>Deseni <c>LIKE</c> için kurmak.</b> <c>Contains</c> sağlayıcıda <c>strpos</c>'a
///         çevriliyor ve <b>hiçbir indeks</b> onu karşılayamıyor — 27k kayıtta her tuş
///         vuruşunda tam tarama. Bu testin kilitlediği şey desenin şekli; indeksin
///         gerçekten kullanıldığı ayrıca <c>PanelNewsTests</c>'te sorgu planıyla ölçülüyor.</item>
/// </list>
/// </remarks>
public class NewsSearchTests
{
    [Fact]
    public void Pattern_WrapsTheTermWithWildcards()
    {
        NewsSearch.Pattern("kadirli").Should().Be("%kadirli%");
    }

    [Fact]
    public void Pattern_LowercasesTheTerm_BecauseTheIndexIsOnLowerCase()
    {
        // ⚠️ İfade indeksi `lower(kolon)` üzerinde; desen küçültülmezse sorgu derlenir,
        // çalışır ve indeksi SESSİZCE kullanmaz.
        NewsSearch.Pattern("KADİRLİ").Should().Be(NewsSearch.Pattern("KADİRLİ")!.ToLowerInvariant());
    }

    [Theory]
    [InlineData("%%", @"%\%\%%")]
    [InlineData("100_", @"%100\_%")]
    [InlineData(@"a\b", @"%a\\b%")]
    public void Pattern_EscapesWildcards_SoASearchTermCannotMatchEverything(string term, string expected)
    {
        // 🔴 Kaçış olmadan "%" araması BÜTÜN arşivi döndürürdü — hata vermeyen yanlış sonuç.
        NewsSearch.Pattern(term).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    public void Pattern_ReturnsNullForEmptyOrTooShortTerms(string? term)
    {
        // §5: süzülmeyen bir değer listeyi BOŞALTMAZ, süzgeci hiç uygulamaz.
        NewsSearch.Pattern(term).Should().BeNull();
    }

    [Fact]
    public void Pattern_TrimsSurroundingWhitespace()
    {
        NewsSearch.Pattern("  kadirli  ").Should().Be("%kadirli%");
    }
}
