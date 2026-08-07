using FluentAssertions;
using KadirliApp.Application.Common.Utils;
using KadirliApp.Application.Features.PowerOutages;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.PowerOutages;

/// <summary>
/// Faz 12.3 — geri doldurmanın <b>saf</b> kalbi: serbest metin mahalle → sözlük satırı.
///
/// 🔑 Bu testler container gerektirmiyor ve gerektirmemeli: eşleştirme bir veri sorusu değil
/// bir <b>metin normalleştirme</b> sorusu. Yanlış cevabı hata vermez — kesinti <i>başka bir
/// mahallenin</i> sakinlerine bildirim yollar ya da hiçbirine yollamaz.
/// </summary>
public class PowerOutageNeighborhoodMatcherTests
{
    private static readonly NeighborhoodRef[] Dictionary =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Cengiz Topel", SlugHelper.Slugify("Cengiz Topel")),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "İstasyon", SlugHelper.Slugify("İstasyon")),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Yenimahalle", SlugHelper.Slugify("Yenimahalle")),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Şehit Kansu", SlugHelper.Slugify("Şehit Kansu"))
    ];

    // ─────────────────── normalleştirme ───────────────────

    [Theory]
    [InlineData("Cengiz Topel Mahallesi", "cengiz-topel")]
    [InlineData("Cengiz Topel Mahalle", "cengiz-topel")]
    [InlineData("Cengiz Topel Mah.", "cengiz-topel")]
    [InlineData("Cengiz Topel MH", "cengiz-topel")]
    [InlineData("  cengiz   topel  ", "cengiz-topel")]
    public void Normalize_StripsNeighbourhoodSuffixes(string raw, string expected)
        => PowerOutageNeighborhoodMatcher.Normalize(raw).Should().Be(expected);

    /// <summary>
    /// 🔴 Görünmez sözleşme #21'in bu fazdaki karşılığı. Türkçe <c>'İ'</c> (U+0130)
    /// <c>ToLowerInvariant()</c> ile <b>küçülmez</b>; ikinci bir normalleştirme yazılsaydı
    /// "İstasyon" ile "istasyon" farklı anahtar üretir ve kesinti sessizce eşleşmezdi.
    /// Kadirli'de İ ile başlayan mahalle adı yaygın — bu yola er ya da geç girilecekti.
    /// </summary>
    [Theory]
    [InlineData("İstasyon")]
    [InlineData("istasyon")]
    [InlineData("İSTASYON Mahallesi")]
    [InlineData("İstasyon Mah.")]
    public void Match_HandlesTurkishDottedCapitalI(string raw)
        => PowerOutageNeighborhoodMatcher.Match(raw, Dictionary)!.Value.Name.Should().Be("İstasyon");

    /// <summary>
    /// "Yenimahalle" içinde "mahalle" geçiyor ama <b>ek değil</b> — kırpılırsa geriye "yeni"
    /// kalır ve mahalle hiçbir zaman eşleşmez. Ek yalnız ayraçtan sonra gelirse ektir.
    /// </summary>
    [Fact]
    public void Normalize_DoesNotStripSuffixThatIsPartOfTheName()
    {
        PowerOutageNeighborhoodMatcher.Normalize("Yenimahalle").Should().Be("yenimahalle");
        PowerOutageNeighborhoodMatcher.Match("Yenimahalle", Dictionary)!.Value.Name.Should().Be("Yenimahalle");
    }

    /// <summary>Yalnız "Mahalle" yazan kayıt boş anahtara inmemeli — boş anahtar her şeyle eşleşmeye açık.</summary>
    [Fact]
    public void Normalize_KeepsSlugWhenStrippingWouldEmptyIt()
        => PowerOutageNeighborhoodMatcher.Normalize("Mahalle").Should().Be("mahalle");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("...")]
    public void Normalize_ReturnsNullForEmptyInput(string? raw)
        => PowerOutageNeighborhoodMatcher.Normalize(raw).Should().BeNull();

    // ─────────────────── eşleştirme ───────────────────

    [Fact]
    public void Match_IsExact_NotContains()
    {
        // "Cengiz" tek başına dört mahalleden hiçbirine bağlanmamalı. "İçeren" eşleşme
        // denenseydi kesinti YANLIŞ mahalleye bağlanırdı — hiç bağlanmamaktan kötü:
        // eşleşmeyen kayıt panelde uyarıyla görünür, yanlış eşleşen kayıt sessizce
        // başka bir mahallenin sakinlerine bildirim yollar.
        PowerOutageNeighborhoodMatcher.Match("Cengiz", Dictionary).Should().BeNull();
        PowerOutageNeighborhoodMatcher.Match("Cengiz Topel Caddesi", Dictionary).Should().BeNull();
    }

    [Fact]
    public void Match_ReturnsNullWhenDictionaryIsEmpty()
        => PowerOutageNeighborhoodMatcher.Match("Cengiz Topel", []).Should().BeNull();

    [Fact]
    public void Match_UsesTheDictionaryCanonicalName()
    {
        var hit = PowerOutageNeighborhoodMatcher.Match("şehit kansu mahallesi", Dictionary);

        hit.Should().NotBeNull();
        hit!.Value.Name.Should().Be("Şehit Kansu",
            "kayda yazılacak ad SÖZLÜKTEN gelir — mobil eşleşmesi kullanıcının profilindeki adla birebir tutmalı");
    }
}
