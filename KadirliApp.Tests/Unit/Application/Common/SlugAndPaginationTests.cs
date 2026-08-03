using FluentAssertions;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Common.Utils;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Common;

/// <summary>
/// Faz 11.15b — iki küçük saf fonksiyon, iki büyük sessiz risk.
/// </summary>
public class SlugHelperTests
{
    /// <summary>
    /// 🔑 Slug **kalıcı kimliktir**: mahalle, etkinlik kategorisi, mekan kategorisi,
    /// duyuru türü ve işletme kategorisi hep bunu kullanıyor ve <c>slug</c> kolonu
    /// benzersiz. Üretim kuralı sessizce değişirse:
    /// <list type="bullet">
    ///   <item>yeni kayıtlar eskilerle **çakışır** (409) ya da</item>
    ///   <item>daha kötüsü, eskiden çakışan iki ad artık çakışmaz ve **mükerrer** kayıt oluşur.</item>
    /// </list>
    /// Türkçe karakterler işin özü: "Çukurova" ile "Cukurova" aynı slug'a düşmeli.
    /// </summary>
    [Theory]
    [InlineData("Çukurova Mahallesi", "cukurova-mahallesi")]
    [InlineData("Şehit Öğretmen", "sehit-ogretmen")]
    [InlineData("Gazi Osman Paşa", "gazi-osman-pasa")]
    [InlineData("Yeni Mahalle", "yeni-mahalle")]
    [InlineData("İstasyon", "istasyon")]
    [InlineData("Ilıca", "ilica")]
    [InlineData("ĞÜŞİÖÇ", "gusioc")]
    public void Slugify_TransliteratesTurkishCharacters(string input, string expected)
        => SlugHelper.Slugify(input).Should().Be(expected);

    /// <summary>Aynı adın farklı yazımları aynı slug'a düşmeli — yoksa mükerrer kayıt oluşur.</summary>
    [Fact]
    public void Slugify_IsCaseInsensitive()
        => SlugHelper.Slugify("KADIRLI MERKEZ").Should().Be(SlugHelper.Slugify("Kadirli Merkez"));

    /// <summary>Baştaki/sondaki tire URL'de çirkin görünür ve eşleştirmeyi bozar.</summary>
    [Theory]
    [InlineData("  Merkez  ", "merkez")]
    [InlineData("-Merkez-", "merkez")]
    [InlineData("Merkez!", "merkez")]
    public void Slugify_TrimsSeparators(string input, string expected)
        => SlugHelper.Slugify(input).Should().Be(expected);

    /// <summary>
    /// Noktalama atılır; alt çizgi ve tire ayraca dönüşür. Rakamlar korunur
    /// (ör. "75. Yıl Mahallesi").
    /// </summary>
    [Theory]
    [InlineData("75. Yıl", "75-yil")]
    [InlineData("Bir_İki", "bir-iki")]
    [InlineData("A & B", "a-b")]
    public void Slugify_HandlesPunctuationAndDigits(string input, string expected)
        => SlugHelper.Slugify(input).Should().Be(expected);

    /// <summary>
    /// ⚠️ Yalnız noktalamadan oluşan ad **boş slug** üretir. Çağıranlar bunu kontrol
    /// etmeli (<c>LookupRules.ValidateSluggedNameAsync</c> ediyor) — yoksa birden fazla
    /// kayıt boş slug'a düşer ve benzersizlik kısıtı beklenmedik yerde patlar.
    /// </summary>
    [Fact]
    public void Slugify_ReturnsEmpty_WhenNothingSurvives()
        => SlugHelper.Slugify("!!! ???").Should().BeEmpty();
}

/// <summary>
/// Faz 10.7'de eklenen DoS koruması: <c>?limit=1000000</c> tüm tabloyu çekiyordu.
/// Koruma sessizce kalkarsa yalnız yavaşlık olarak görünür — hata mesajı yoktur.
/// </summary>
public class PaginationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    public void Clamp_ForcesPageToBeAtLeastOne(int input, int expected)
        => Pagination.Clamp(input, 20).Page.Should().Be(expected);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(20, 20)]
    [InlineData(50, 50)]
    [InlineData(1_000_000, Pagination.MaxLimit)]
    public void Clamp_CapsLimitAtThePublicMaximum(int input, int expected)
        => Pagination.Clamp(1, input).Limit.Should().Be(expected);

    /// <summary>Panel listeleri daha yüksek tavana sahip ama yine sınırlı.</summary>
    [Fact]
    public void Clamp_UsesTheAdminCeilingWhenAsked()
    {
        Pagination.Clamp(1, 1_000_000, Pagination.AdminMaxLimit).Limit.Should().Be(Pagination.AdminMaxLimit);
        Pagination.AdminMaxLimit.Should().BeGreaterThan(Pagination.MaxLimit);
        Pagination.AdminMaxLimit.Should().BeLessThan(1000, "panel tavanı da sınırsız olmamalı");
    }
}
