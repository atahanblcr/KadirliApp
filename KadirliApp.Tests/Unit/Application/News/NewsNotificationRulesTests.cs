using System;
using FluentAssertions;
using KadirliApp.Application.Features.News;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.15 — <b>"gönderilebilir mi, gönderilemiyorsa neden?"</b>
/// </summary>
/// <remarks>
/// 🔑 Cevabın tek sahibi olmak zorunda: aynı soruyu panelin önizlemesi (butonu çizen) ve
/// komutun kendisi (gönderimi reddeden) soruyor. Ayrı yazılsalardı 12.2b'nin dersi
/// tekrarlanırdı — <i>görünüm kendi koşulunu yazarsa komutun reddedeceği bir buton çizilir.</i>
/// </remarks>
public class NewsNotificationRulesTests
{
    [Fact]
    public void AVisiblePublishedArticle_IsSendable()
        => NewsNotificationRules.Evaluate(Article(), isVisibleToCitizens: true)
            .Should().Be(NewsNotifyEligibility.Sendable);

    [Fact]
    public void AnAlreadyNotifiedArticle_CannotBeSentAgain()
    {
        var article = Article();
        article.MarkNotificationSent(Guid.NewGuid(), 10, null, DateTime.UtcNow);

        NewsNotificationRules.Evaluate(article, isVisibleToCitizens: true)
            .Should().Be(NewsNotifyEligibility.AlreadySent);
    }

    [Fact]
    public void AnArchivedArticle_IsNotSendable()
    {
        var article = Article();
        article.Archive("yanlış bilgi", null, DateTime.UtcNow);

        NewsNotificationRules.Evaluate(article, isVisibleToCitizens: false)
            .Should().Be(NewsNotifyEligibility.Archived);
    }

    [Fact]
    public void AGoneArticle_IsNotSendable()
    {
        var article = Article();
        article.MarkSourceGone(DateTime.UtcNow);

        NewsNotificationRules.Evaluate(article, isVisibleToCitizens: false)
            .Should().Be(NewsNotifyEligibility.SourceGone);
    }

    /// <summary>
    /// 🔴 <b>Planın listesinde OLMAYAN dördüncü koşul.</b>
    /// </summary>
    /// <remarks>
    /// 12.15 planı butonun koşulunu <i>"arşivlenmemiş + kaynağı yayında"</i> diye yazıyordu;
    /// oysa haberin görünmezliğinin <b>üç</b> ekseni var (§7 madde 58/59). Dışlanmış
    /// kategorideki bir haber panelde "Yayında" görünür ama uygulamada <b>yoktur</b> —
    /// bildirimi gönderilseydi vatandaş bildirimi alır, dokunur ve <b>boş sayfaya</b> düşerdi
    /// (11.15c'de duyurularda birebir yaşandı, §7 madde 24).
    /// </remarks>
    [Fact]
    public void AnArticleHiddenOnlyByCategoryExclusion_IsNotSendable()
    {
        // Kayıt yayında, kaynağı yerinde — tek sorun görünürlük sorgusunun onu elemesi.
        var article = Article();

        NewsNotificationRules.Evaluate(article, isVisibleToCitizens: false)
            .Should().Be(NewsNotifyEligibility.CategoryExcluded);
    }

    /// <summary>
    /// "Zaten gönderildi" <b>diğer her sebepten önce</b> söylenir.
    /// </summary>
    /// <remarks>
    /// Sıra ters olsaydı arşivlenmiş ama bildirimi çoktan gitmiş bir haberde panel
    /// <i>"önce yayına alın"</i> derdi; yönetici yayına alır, tekrar dener ve <b>o zaman</b>
    /// reddedilirdi. İki adımlık bir yanlış yönlendirme — <c>NewsStates.Of</c>'un
    /// "ortadan kalkması daha zor olan sebebi göster" kuralının aynısı.
    /// </remarks>
    [Fact]
    public void AlreadySent_WinsOverEveryOtherReason()
    {
        var article = Article();
        article.MarkNotificationSent(Guid.NewGuid(), 5, null, DateTime.UtcNow);
        article.Archive("gerekçe", null, DateTime.UtcNow);
        article.MarkSourceGone(DateTime.UtcNow);

        NewsNotificationRules.Evaluate(article, isVisibleToCitizens: false)
            .Should().Be(NewsNotifyEligibility.AlreadySent);
    }

    [Fact]
    public void EveryBlockingReason_HasATurkishExplanation()
    {
        foreach (var value in Enum.GetValues<NewsNotifyEligibility>())
        {
            var reason = NewsNotificationRules.Reason(value);

            if (value == NewsNotifyEligibility.Sendable)
            {
                reason.Should().BeNull("gönderilebilir bir kayıtta ekranda sebep yazmaz");
                continue;
            }

            // ⚠️ Yalnız "dolu mu" değil, "ne yapılacağını söylüyor mu": sebebi olmayan bir
            // kapalı buton, yöneticiyi tahmine bırakır — bu bloğun savaştığı hasar sınıfı.
            reason.Should().NotBeNullOrWhiteSpace();
            reason!.Length.Should().BeGreaterThan(20);
        }
    }

    // ── Yardımcı ────────────────────────────────────────────────────────────

    private static NewsArticle Article()
    {
        var article = new NewsArticle { WpId = 1 };
        article.ApplySourceSnapshot(new NewsArticleSnapshot(
            Title: "Başlık",
            Excerpt: "Özet.",
            ContentHtml: "<p>gövde</p>",
            PlainText: "gövde",
            Url: "https://example.test/1",
            PublishedAtUtc: DateTime.UtcNow,
            ModifiedAtUtc: DateTime.UtcNow,
            Checksum: "c",
            ImageUrl: null,
            ImageFileId: null,
            ImageWidth: null,
            ImageHeight: null,
            ReadingMinutes: 1), DateTime.UtcNow);
        return article;
    }
}
