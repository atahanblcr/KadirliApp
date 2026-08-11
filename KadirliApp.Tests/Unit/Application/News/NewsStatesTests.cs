using System;
using FluentAssertions;
using KadirliApp.Application.Features.News;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.13 — panelin haber durumu ve override bayatlaması (saf kurallar).
/// </summary>
public class NewsStatesTests
{
    [Fact]
    public void GoneWinsOverArchived_BecauseTheAdminMustSeeTheReasonThatOutlivesTheirAction()
    {
        // 🔴 Hem arşivlenmiş hem kaynaktan kalkmış bir kayıtta "Yayından kaldırıldı" yazmak
        // yöneticiyi YANLIŞ İŞE yönlendirir: "Geri al"a basar, kayıt yine görünmez ve sebebi
        // ekranda hiçbir yerde yazmaz (UnarchiveNewsArticleCommand bunu zaten söylüyor).
        NewsStates.Of(isArchived: true, sourceState: NewsSourceStates.Gone)
            .Should().Be(NewsStates.Gone);
    }

    [Fact]
    public void ArchivedWinsOverPublished()
    {
        NewsStates.Of(isArchived: true, sourceState: NewsSourceStates.Published)
            .Should().Be(NewsStates.Archived);
    }

    [Fact]
    public void PublishedIsTheDefault()
    {
        NewsStates.Of(isArchived: false, sourceState: NewsSourceStates.Published)
            .Should().Be(NewsStates.Published);
    }

    [Fact]
    public void GoneAloneIsGone_EvenWithoutArchiving()
    {
        NewsStates.Of(isArchived: false, sourceState: NewsSourceStates.Gone)
            .Should().Be(NewsStates.Gone);
    }

    // ── Override bayatlaması ────────────────────────────────────────────────────

    [Fact]
    public void OverrideIsStale_WhenTheSourceChangedAfterTheEdit()
    {
        var edited = new DateTime(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc);

        NewsAdminProjection.IsStale(edited, edited.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void OverrideIsNotStale_WhenTheEditIsNewerThanTheSource()
    {
        var modified = new DateTime(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc);

        NewsAdminProjection.IsStale(modified.AddMinutes(1), modified).Should().BeFalse();
    }

    [Fact]
    public void OverrideIsNotStale_WhenThereIsNoOverride()
    {
        // ⚠️ Override'ı olmayan bir kayıt "bayat" olamaz — sayılsaydı panelin
        // "kaynağı güncellenmiş" sayacı, düzenlenmemiş 27k kaydı da sayardı ve
        // yöneticinin bakması gereken iş listesi ANLAMSIZLAŞIRDI.
        NewsAdminProjection.IsStale(null, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Finish_DerivesStateAndExpiredFeature()
    {
        var now = new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

        var dto = NewsAdminProjection.Finish(new NewsAdminDto
        {
            IsArchived = false,
            SourceState = NewsSourceStates.Published,
            IsFeatured = true,
            FeaturedUntil = now.AddMinutes(-1),
            ModifiedAt = now,
            OverrideUpdatedAt = now.AddHours(-1)
        }, now);

        dto.State.Should().Be(NewsStates.Published);
        // Süresi dolmuş manşet "öne çıkan" DEĞİLDİR; panel bunu ayrı bir rozetle söyler.
        dto.FeaturedExpired.Should().BeTrue();
        dto.OverrideIsStale.Should().BeTrue();
    }
}
