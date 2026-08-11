using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.12 — kaynakla ilgili <b>saf</b> kurallar: görsel zinciri, sağlama, okuma süresi,
/// bayatlık eşiği. Hepsi ağa çıkmadan koşar.
/// </summary>
public class NewsSourceRulesTests
{
    private static NewsSourceImage Image(string name) => new($"https://ornek/{name}.webp", 100, 60);

    // ───────────────────────── Görsel zinciri (ölçülmüş gerçeklere dayanıyor) ─────────

    [Fact]
    public void PickCover_PrefersFull()
    {
        var sizes = new Dictionary<string, NewsSourceImage>
        {
            ["thumbnail"] = Image("thumb"),
            ["medium"] = Image("medium"),
            ["full"] = Image("full")
        };

        NewsImagePicker.PickCover(sizes)!.Url.Should().Contain("full");
    }

    /// <summary>
    /// 📊 40 haberin <b>1</b>'inde var: zincire konsaydı 39 haberde sessizce bir adım kayardı
    /// ve zinciri okuyan bir sonraki kişi kaynağın büyük görsel verdiğini <b>sanardı</b>.
    /// </summary>
    [Fact]
    public void Chains_NeverContainTheUnreliableSizes()
    {
        NewsImagePicker.CoverChain.Should().NotContain(NewsImagePicker.UnreliableSizes);
        NewsImagePicker.ThumbnailChain.Should().NotContain(NewsImagePicker.UnreliableSizes);
    }

    /// <summary>
    /// Zincirin tamamı yoksa bile <b>bir şey göster</b> — ama güvenilmez boyutu değil.
    /// "Şüphede kalınca göster" (§7 madde 49) burada da geçerli.
    /// </summary>
    [Fact]
    public void PickCover_FallsBackToAnyUsableSize_ButNotToTheUnreliableOnes()
    {
        var onlyUnreliable = new Dictionary<string, NewsSourceImage> { ["large"] = Image("large") };
        NewsImagePicker.PickCover(onlyUnreliable).Should().BeNull();

        var unknownName = new Dictionary<string, NewsSourceImage> { ["jannah-image-small"] = Image("small") };
        NewsImagePicker.PickCover(unknownName)!.Url.Should().Contain("small");
    }

    [Fact]
    public void PickCover_ReturnsNull_WhenThereIsNoImageAtAll()
    {
        NewsImagePicker.PickCover(null).Should().BeNull();
        NewsImagePicker.PickCover(new Dictionary<string, NewsSourceImage>()).Should().BeNull();
    }

    /// <summary>
    /// ⚠️ <c>jannah-*</c> WP <b>temasından</b> geliyor: tema değişirse sessizce kaybolur.
    /// Yedek zincirde olabilir, <b>ilk sırada</b> olamaz.
    /// </summary>
    [Fact]
    public void ThemeSpecificSize_IsNeverTheFirstChoice()
    {
        NewsImagePicker.CoverChain[0].Should().Be("full");
        NewsImagePicker.ThumbnailChain[0].Should().Be("medium");
    }

    // ───────────────────────── Sağlama ────────────────────────────────────────────────

    [Fact]
    public void Checksum_IsStable_WhenNothingChanged()
    {
        var a = NewsChecksum.Compute("Başlık", "özet", "<p>gövde</p>", "https://g/1.webp", new[] { 3, 1 });
        var b = NewsChecksum.Compute("Başlık", "özet", "<p>gövde</p>", "https://g/1.webp", new[] { 3, 1 });

        a.Should().Be(b);
    }

    /// <summary>
    /// Kaynak kategori sırasını değiştirebiliyor. Sıralanmadan hesaplansaydı <b>aynı içerik</b>
    /// farklı sağlama üretir ve "değişmedi → yazma" kısayolu hiç çalışmazdı: her koşuda
    /// bütün haberler yeniden yazılırdı ve kimse fark etmezdi.
    /// </summary>
    [Fact]
    public void Checksum_IgnoresCategoryOrderAndDuplicates()
    {
        NewsChecksum.Compute("B", null, "<p>x</p>", null, new[] { 49, 51, 52 })
            .Should().Be(NewsChecksum.Compute("B", null, "<p>x</p>", null, new[] { 52, 49, 51, 52 }));
    }

    [Theory]
    [InlineData("Başka başlık", "özet", "<p>gövde</p>", "https://g/1.webp")]
    [InlineData("Başlık", "başka özet", "<p>gövde</p>", "https://g/1.webp")]
    [InlineData("Başlık", "özet", "<p>başka gövde</p>", "https://g/1.webp")]
    [InlineData("Başlık", "özet", "<p>gövde</p>", "https://g/2.webp")]
    public void Checksum_ChangesWithEveryMeaningfulField(string title, string? excerpt, string body, string image)
    {
        var baseline = NewsChecksum.Compute("Başlık", "özet", "<p>gövde</p>", "https://g/1.webp", new[] { 1 });

        NewsChecksum.Compute(title, excerpt, body, image, new[] { 1 }).Should().NotBe(baseline);
    }

    // ───────────────────────── Okuma süresi ───────────────────────────────────────────

    [Fact]
    public void ReadingTime_IsAtLeastOneMinute()
    {
        NewsReadingTime.Minutes(null).Should().Be(1);
        NewsReadingTime.Minutes("   ").Should().Be(1);
        NewsReadingTime.Minutes("tek kelime").Should().Be(1, "'0 dk okuma' bir bilgi değil, hata gibi görünür");
    }

    [Fact]
    public void ReadingTime_RoundsUp()
    {
        var text = string.Join(' ', Enumerable.Repeat("kelime", NewsReadingTime.WordsPerMinute + 1));

        NewsReadingTime.Minutes(text).Should().Be(2);
    }

    // ───────────────────────── Bayatlık ───────────────────────────────────────────────

    /// <summary>
    /// 🔴 Bu bloğun 1 numaralı hasar sınıfı: senkron durursa uygulama <b>eski haberi
    /// göstermeye devam eder</b> ve uçlar 200 döner. Eşikler tek yerde olmak zorunda —
    /// ikiye ayrılırsa pano "taze" derken uyarı "durdu" der (§7 madde 35'in sınıfı).
    /// </summary>
    [Fact]
    public void Freshness_HasThreeDistinctStates()
    {
        var now = new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

        NewsSyncHealth.Evaluate(null, now).Should().Be(NewsSyncFreshness.NeverRan);
        NewsSyncHealth.Evaluate(now.AddMinutes(-10), now).Should().Be(NewsSyncFreshness.Fresh);
        NewsSyncHealth.Evaluate(now - NewsSyncHealth.StaleAfter, now).Should().Be(NewsSyncFreshness.Stale);
        NewsSyncHealth.Evaluate(now - NewsSyncHealth.StalledAfter, now).Should().Be(NewsSyncFreshness.Stalled);
    }

    [Fact]
    public void Freshness_ThresholdsAreOrdered()
    {
        NewsSyncHealth.StaleAfter.Should().BeLessThan(NewsSyncHealth.StalledAfter,
            "'gecikmiş' eşiği 'durmuş' eşiğinden büyük olsaydı ara durum hiç görünmezdi");
    }
}
