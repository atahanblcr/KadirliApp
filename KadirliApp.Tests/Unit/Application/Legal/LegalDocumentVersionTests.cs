using System;
using FluentAssertions;
using KadirliApp.Application.Features.Legal;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Legal;

/// <summary>
/// Faz 12.16 — <b>görünmez sözleşme 72</b>: yayınlanmış sürüm değiştirilemez.
/// </summary>
/// <remarks>
/// <para>
/// Kuralın <b>birinci</b> ayağı derleyicidedir: içerik alanları <c>init</c>, yani
/// <c>version.Body = "…"</c> yazmak <b><c>CS8852</c></b>'dir ve bir testle ölçülemez —
/// ölçen şey derlemenin kendisidir (12.11'in <c>ModerationSingleOwnerTests</c> ile aynı
/// ayrımı; oradaki yapısal test de kuralı değil <b>derleyici güvencesinin sökülmesini</b>
/// kilitler — bkz. <see cref="LegalImmutabilityStructureTests"/>).
/// </para>
/// <para>
/// Buradaki testler <b>ikinci</b> ayağı tutar: varlığın kendi kapısı
/// (<c>TryRevise</c>/<c>Publish</c>/<c>Supersede</c>) <i>ne zaman</i> yazılabileceğini
/// söylüyor. Derleyici "kim yazabilir"i, bu kapı "ne zaman"ı tutar.
/// </para>
/// </remarks>
public class LegalDocumentVersionTests
{
    private static LegalDocumentVersion Draft(string body = "<p>ilk</p>") => new()
    {
        Id = Guid.NewGuid(),
        VersionNumber = 1,
        Body = body,
        Summary = "ilk özet",
        EffectiveFrom = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void ADraft_CanBeRevised()
    {
        var version = Draft();

        version.TryRevise("<p>yeni</p>", "yeni özet", requiresReconsent: true, effectiveFrom: version.EffectiveFrom)
            .Should().BeTrue();

        version.Body.Should().Be("<p>yeni</p>");
        version.Summary.Should().Be("yeni özet");
        version.RequiresReconsent.Should().BeTrue();
    }

    /// <summary>
    /// 🔴 Bu, bloğun var olma sebebi: değiştirilebilseydi bütün geçmiş rıza kayıtları
    /// <b>retroaktif olarak</b> başka bir metni işaret ederdi — tablo dolu, kanıt yok.
    /// </summary>
    [Fact]
    public void APublishedVersion_CannotBeRevised_AndIsLeftUntouched()
    {
        var version = Draft();
        version.Publish(Guid.NewGuid(), DateTime.UtcNow);

        version.TryRevise("<p>gizlice değiştirilmiş</p>", "başka", true, DateTime.UtcNow)
            .Should().BeFalse();

        // ⚠️ İddia yalnız dönüş değerine bakmıyor: `false` dönüp yine de yazan bir
        // gerçekleme "reddettim" der ve metni değiştirmiş olurdu — en sinsi hâli.
        version.Body.Should().Be("<p>ilk</p>");
        version.Summary.Should().Be("ilk özet");
        version.RequiresReconsent.Should().BeFalse();
    }

    [Fact]
    public void ASupersededVersion_CannotBeRevisedEither()
    {
        var version = Draft();
        version.Publish(Guid.NewGuid(), DateTime.UtcNow);
        version.Supersede(DateTime.UtcNow);

        version.TryRevise("<p>x</p>", null, false, DateTime.UtcNow).Should().BeFalse();
        version.Body.Should().Be("<p>ilk</p>");
    }

    /// <summary>Yayınlama <b>terminaldir</b> — ikinci çağrı kaydı değiştirmez (12.15 deseni).</summary>
    [Fact]
    public void Publish_IsTerminal_TheSecondCallChangesNothing()
    {
        var version = Draft();
        var firstAdmin = Guid.NewGuid();
        var firstMoment = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

        version.Publish(firstAdmin, firstMoment).Should().BeTrue();
        version.Publish(Guid.NewGuid(), firstMoment.AddDays(1)).Should().BeFalse();

        version.PublishedBy.Should().Be(firstAdmin);
        version.PublishedAt.Should().Be(firstMoment);
    }

    [Fact]
    public void ADraft_CannotBeSuperseded()
    {
        var version = Draft();

        version.Supersede(DateTime.UtcNow).Should().BeFalse();
        version.SupersededAt.Should().BeNull();
    }

    [Fact]
    public void IsLive_IsTrueOnlyBetweenPublishAndSupersede()
    {
        var version = Draft();
        version.IsLive.Should().BeFalse();
        version.IsDraft.Should().BeTrue();

        version.Publish(Guid.NewGuid(), DateTime.UtcNow);
        version.IsLive.Should().BeTrue();
        version.IsDraft.Should().BeFalse();

        version.Supersede(DateTime.UtcNow);
        version.IsLive.Should().BeFalse();
        version.IsDraft.Should().BeFalse("yürürlükten kalkmış sürüm taslak DEĞİLDİR — yeniden düzenlenemez");
    }

    // ── Yeniden onay türetmesi ──────────────────────────────────────────────

    private static LegalDocument DocumentWith(params (int Number, bool RequiresReconsent, bool Superseded)[] versions)
    {
        var document = new LegalDocument
        {
            Id = Guid.NewGuid(),
            Type = LegalDocumentTypes.Kvkk,
            Title = "KVKK",
            IsMandatory = true,
            IsActive = true
        };

        foreach (var (number, reconsent, superseded) in versions)
        {
            var version = new LegalDocumentVersion
            {
                Id = Guid.NewGuid(),
                VersionNumber = number,
                Body = $"<p>v{number}</p>",
                RequiresReconsent = reconsent,
                EffectiveFrom = DateTime.UtcNow
            };
            version.Publish(Guid.NewGuid(), DateTime.UtcNow);
            if (superseded) version.Supersede(DateTime.UtcNow);
            document.Versions.Add(version);
        }

        return document;
    }

    [Fact]
    public void NeedsReconsent_IsFalse_WhenTheUserAlreadyConsentedToTheLiveVersion()
    {
        var document = DocumentWith((1, false, true), (2, true, false));

        LegalProjection.NeedsReconsent(document, consentedVersionNumber: 2, granted: true).Should().BeFalse();
    }

    [Fact]
    public void NeedsReconsent_IsFalse_WhenTheNewVersionIsOnlyATypoFix()
    {
        var document = DocumentWith((1, false, true), (2, false, false));

        LegalProjection.NeedsReconsent(document, consentedVersionNumber: 1, granted: true).Should().BeFalse(
            "yazım hatası düzeltmesi bütün şehri yeniden onay ekranına düşürmemeli");
    }

    /// <summary>
    /// 🔴 <b>Asıl sınav:</b> ölçüt "sürüm numarası değişti mi" <b>değil</b>, "aradaki
    /// sürümlerden <b>herhangi biri</b> esaslı mıydı". Yalnız <b>sonuncuya</b> bakan bir
    /// gerçekleme burada yeşil kalırdı ve esaslı bir kapsam değişikliği
    /// <b>hiç kimseye ulaşmazdı</b>.
    /// </summary>
    [Fact]
    public void NeedsReconsent_IsTrue_WhenAnIntermediateVersionRequiredIt()
    {
        // v1 (onaylandı) → v2 ESASLI → v3 yazım düzeltmesi (yayında)
        var document = DocumentWith((1, false, true), (2, true, true), (3, false, false));

        LegalProjection.NeedsReconsent(document, consentedVersionNumber: 1, granted: true).Should().BeTrue(
            "aradaki esaslı değişiklik atlanırsa kapsam genişlemesi hiç kimseye ulaşmaz");
    }

    [Fact]
    public void NeedsReconsent_IsTrue_ForAMandatoryDocumentTheUserNeverAnswered()
    {
        var document = DocumentWith((1, false, false));

        LegalProjection.NeedsReconsent(document, consentedVersionNumber: null, granted: false).Should().BeTrue();
    }

    [Fact]
    public void NeedsReconsent_IsFalse_ForAnOptionalDocumentTheUserNeverAnswered()
    {
        var document = DocumentWith((1, false, false));
        document.IsMandatory = false;

        LegalProjection.NeedsReconsent(document, consentedVersionNumber: null, granted: false).Should().BeFalse(
            "isteğe bağlı bir izni hiç sormadıysak kullanıcıyı rahatsız etmiyoruz");
    }
}
