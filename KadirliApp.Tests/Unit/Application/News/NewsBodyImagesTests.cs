using System.Collections.Generic;
using FluentAssertions;
using KadirliApp.Application.Features.News;
using KadirliApp.Infrastructure.News;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.14 — metin arası görsellerin bulunması ve yeniden yazılması (saf, container'sız).
///
/// 🔑 <b>Testlerin bir kısmı gerçek sanitizer'ın çıktısıyla besleniyor</b> ve bu bilinçli:
/// <c>NewsBodyImages</c> regex kullanıyor ve bunun tek gerekçesi girdinin <b>rastgele HTML
/// değil</b>, kendi temizleyicimizin ürettiği dar bir alt küme olması. Bu varsayım
/// <b>görünmez</b> — temizleyiciyi değiştiren biri buranın zeminini de değiştirir. Zinciri
/// uçtan uca deneyen testler o zemini kilitliyor.
/// </summary>
public class NewsBodyImagesTests
{
    private static readonly NewsHtmlSanitizer Sanitizer = new();

    [Fact]
    public void ExternalUrls_FindsEachImageOnce()
    {
        var html =
            "<p>Metin</p><img src=\"https://ornek.com/a.jpg\">" +
            "<figure><img src=\"https://ornek.com/b.jpg\"></figure>" +
            // Aynı görsel iki kez geçebilir: iki kez indirmek istemiyoruz.
            "<img src=\"https://ornek.com/a.jpg\">";

        NewsBodyImages.ExternalUrls(html).Should()
            .Equal("https://ornek.com/a.jpg", "https://ornek.com/b.jpg");
    }

    [Fact]
    public void ExternalUrls_IgnoresAlreadyMirroredImages()
    {
        // 🔴 En önemli iddia: aynalanmış (göreli) adres yeniden indirilmeye çalışılmamalı.
        // Aksi hâlde her koşu KENDİ çıktısını indirir ve `uploads/` mükerrer dosyayla
        // şişer — "sorun yıllar sonra fark edilir" sınıfı.
        var html = "<img src=\"/uploads/abc_a.jpg\"><img src=\"https://ornek.com/b.jpg\">";

        NewsBodyImages.ExternalUrls(html).Should().Equal("https://ornek.com/b.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<p>Görselsiz gövde</p>")]
    public void ExternalUrls_IsEmptyWhenThereIsNothingToMirror(string? html)
    {
        NewsBodyImages.ExternalUrls(html).Should().BeEmpty();
        NewsBodyImages.HasExternalImages(html).Should().BeFalse();
    }

    [Fact]
    public void Rewrite_ReplacesOnlyTheMirroredOnes()
    {
        var html =
            "<p>Metin</p><img src=\"https://ornek.com/a.jpg\" alt=\"a\">" +
            "<img src=\"https://ornek.com/b.jpg\">";

        var result = NewsBodyImages.Rewrite(html, new Dictionary<string, string>
        {
            ["https://ornek.com/a.jpg"] = "/uploads/x_a.jpg"
        });

        result.Should().Contain("src=\"/uploads/x_a.jpg\"");
        result.Should().Contain("alt=\"a\"", "diğer öznitelikler korunmalı");
        // 🔴 İndirilemeyen görsel gövdeden SİLİNMEZ: hotlink hâli 12.14 öncesinin ta
        // kendisi, yani "aynalayamadım" durumunda eski davranışa düşmek doğru yön.
        result.Should().Contain("https://ornek.com/b.jpg");
    }

    [Fact]
    public void Rewrite_IsIdempotent()
    {
        // İkinci koşuda aynı gövde yeniden yazılmaya çalışılırsa hiçbir şey değişmemeli.
        var html = "<img src=\"https://ornek.com/a.jpg\">";
        var map = new Dictionary<string, string> { ["https://ornek.com/a.jpg"] = "/uploads/x_a.jpg" };

        var once = NewsBodyImages.Rewrite(html, map);
        var twice = NewsBodyImages.Rewrite(once, map);

        twice.Should().Be(once);
        NewsBodyImages.HasExternalImages(twice).Should().BeFalse();
    }

    [Fact]
    public void Rewrite_LeavesTheBodyUntouchedWhenNothingWasMirrored()
    {
        var html = "<p>Metin</p><img src=\"https://ornek.com/a.jpg\">";

        NewsBodyImages.Rewrite(html, new Dictionary<string, string>()).Should().Be(html);
    }

    // ─────────────────── Temizleyici ile zincir (görünmez zemin) ────────────────────

    [Fact]
    public void TheSanitizerOutput_IsStillParseableByTheRewriter()
    {
        // 🔑 Zincirin gerçek sırası: kaynak HTML → sanitizer → NewsBodyImages.
        // Sanitizer tırnak biçimini ve öznitelik sırasını normalleştiriyor; regex'in
        // dayandığı varsayım tam olarak bu.
        var raw =
            "<figure class='wp-block-image'><img src='https://ornek.com/a.jpg' " +
            "srcset='https://ornek.com/a-300.jpg 300w' width=650 height=368 alt='Foto'>" +
            "<figcaption>Fotoğraf</figcaption></figure>";

        var clean = Sanitizer.Sanitize(raw);

        NewsBodyImages.ExternalUrls(clean).Should().Equal("https://ornek.com/a.jpg");

        var rewritten = NewsBodyImages.Rewrite(clean, new Dictionary<string, string>
        {
            ["https://ornek.com/a.jpg"] = "/uploads/x_a.jpg"
        });

        rewritten.Should().Contain("/uploads/x_a.jpg");
        NewsBodyImages.HasExternalImages(rewritten).Should().BeFalse(
            "srcset beyaz listede olmadığı için geriye bayat bir dış adres kalmamalı");
    }

    [Fact]
    public void TheSanitizer_DoesNotKeepSrcset()
    {
        // ⚠️ Bu iddia bu fazın bir ön koşulu: `srcset` kalsaydı tarayıcı/istemci onu
        // `src`'ye tercih eder ve aynaladığımız görsel HİÇ kullanılmazdı — aynalama
        // çalışır görünür, hiçbir işe yaramazdı.
        var clean = Sanitizer.Sanitize(
            "<img src='https://ornek.com/a.jpg' srcset='https://ornek.com/a-300.jpg 300w'>");

        clean.Should().NotContain("srcset");
    }
}
