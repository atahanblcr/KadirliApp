using FluentAssertions;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.12 — varlığın <b>geçişleri</b>: kim neye dokunabilir?
///
/// Yansıma testi (<c>NewsSourceOwnershipTests</c>) alanların <c>init</c> olduğunu kilitliyor;
/// burada <b>davranış</b> kilitleniyor: bir metot kendi kümesinin dışına taşarsa derleyici
/// bunu göremez (aynı sınıfın içinden her alan yazılabilir).
/// </summary>
public class NewsArticleTransitionTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    private static NewsArticleSnapshot Snapshot(string title = "Kaynak başlığı") => new(
        Title: title,
        Excerpt: "kaynak özeti",
        ContentHtml: "<p>gövde</p>",
        PlainText: "gövde",
        Url: "https://ornek/1",
        PublishedAtUtc: Now.AddDays(-1),
        ModifiedAtUtc: Now,
        Checksum: "abc",
        ImageUrl: null,
        ImageFileId: null,
        ImageWidth: null,
        ImageHeight: null,
        ReadingMinutes: 1);

    private static NewsArticle Article()
    {
        var article = new NewsArticle { WpId = 1 };
        article.ApplySourceSnapshot(Snapshot(), Now);
        return article;
    }

    /// <summary>🔴 Bu bloğun 2 numaralı hasar sınıfının davranış kilidi.</summary>
    [Fact]
    public void ApplySourceSnapshot_NeverTouchesTheOverrides()
    {
        var article = Article();
        var admin = Guid.NewGuid();
        article.SetOverrides("Yönetici başlığı", "Yönetici özeti", null, admin, Now);

        article.ApplySourceSnapshot(Snapshot("Kaynak değişti"), Now.AddHours(1));

        article.SourceTitle.Should().Be("Kaynak değişti");
        article.TitleOverride.Should().Be("Yönetici başlığı");
        article.ExcerptOverride.Should().Be("Yönetici özeti");
        article.OverrideUpdatedBy.Should().Be(admin);
    }

    /// <summary>Simetrik yön: yöneticinin yazması kaynağın alanlarını bozmamalı.</summary>
    [Fact]
    public void SetOverrides_NeverTouchesTheSourceFields()
    {
        var article = Article();

        article.SetOverrides("Yönetici", null, null, Guid.NewGuid(), Now);

        article.SourceTitle.Should().Be("Kaynak başlığı");
        article.SourceChecksum.Should().Be("abc", "sağlama kaynağın malı — override onu değiştiremez");
    }

    /// <summary>Arşivleme kaynağa dokunmaz: kaydın kaynakta durduğu bilgisi kaybolmamalı.</summary>
    [Fact]
    public void Archive_DoesNotChangeTheSourceState()
    {
        var article = Article();

        article.Archive("Yerel değil", Guid.NewGuid(), Now);

        article.IsArchived.Should().BeTrue();
        article.SourceState.Should().Be(NewsSourceStates.Published);
    }

    /// <summary>Bayat gerekçe temizliği (onay/red izi simetrisinin haber karşılığı).</summary>
    [Fact]
    public void Unarchive_ClearsTheStaleReason()
    {
        var article = Article();
        article.Archive("Yanlış haber", Guid.NewGuid(), Now);

        article.Unarchive();

        article.IsArchived.Should().BeFalse();
        article.ArchivedReason.Should().BeNull();
        article.ArchivedBy.Should().BeNull();
        article.ArchivedAt.Should().BeNull();
    }

    /// <summary>Boş/whitespace override <b>yok</b> demektir — "boş başlık" diye bir şey olamaz.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BlankOverride_MeansNoOverride(string? value)
    {
        var article = Article();

        article.SetOverrides(value, value, null, Guid.NewGuid(), Now);

        article.TitleOverride.Should().BeNull();
        article.ExcerptOverride.Should().BeNull();
        article.OverrideUpdatedAt.Should().BeNull("override kalmadıysa damga da kalmamalı");
    }

    /// <summary>
    /// 🔑 Kaynak akışında yeniden görünen haber <c>published</c>'a döner — mutabakatın ters
    /// yönü bu sayede <b>bedava</b> ve idempotent. Damga da tazelenir: "ne zaman geri geldi?"
    /// </summary>
    [Fact]
    public void ApplySourceSnapshot_RestoresAGoneArticle()
    {
        var article = Article();
        article.MarkSourceGone(Now);

        article.ApplySourceSnapshot(Snapshot(), Now.AddHours(2));

        article.SourceState.Should().Be(NewsSourceStates.Published);
        article.SourceStateChangedAt.Should().Be(Now.AddHours(2));
    }

    /// <summary>İdempotentlik: aynı durumu ikinci kez yazmak damgayı <b>kaydırmamalı</b>.</summary>
    [Fact]
    public void MarkSourceGone_IsIdempotent()
    {
        var article = Article();
        article.MarkSourceGone(Now);

        article.MarkSourceGone(Now.AddDays(1));

        article.SourceStateChangedAt.Should().Be(Now, "'ne zaman gitti' bilgisi ilk andır");
    }

    /// <summary>Öne çıkarma kapatılınca süre de düşer — yoksa yeniden açan bayat bir süre bulur.</summary>
    [Fact]
    public void SetFeatured_ClearsTheDeadline_WhenTurnedOff()
    {
        var article = Article();
        article.SetFeatured(true, Now.AddDays(3));

        article.SetFeatured(false, null);

        article.IsFeatured.Should().BeFalse();
        article.FeaturedUntil.Should().BeNull();
    }

    /// <summary>Kategori bağları kaynağın malı: mükerrer bağ yazılmamalı.</summary>
    [Fact]
    public void ReplaceCategories_IsDeduplicated()
    {
        var article = Article();
        var category = new NewsCategory { WpId = 1, Name = "Gündem", Slug = "gundem" };

        article.ReplaceCategories(new[] { category, category });

        article.Categories.Should().HaveCount(1);
    }
}
