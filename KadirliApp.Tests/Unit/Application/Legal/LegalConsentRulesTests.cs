using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KadirliApp.Application.Features.Legal;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Legal;

/// <summary>
/// Faz 12.16 — <b>"hangi belge sorulur, hangisi kaydı bloklar?"</b> kuralının saf testi.
/// </summary>
/// <remarks>
/// Kilitlediği şey görünmez sözleşme <b>71</b>'in ön koşulu: <b>yayında sürümü olmayan belge
/// zorunlu olamaz.</b> Sayılmasaydı, <c>IsMandatory</c> işaretli ama hiç sürüm yayınlanmamış
/// bir belge kaydı <b>tamamen kilitlerdi</b> — istemci gösterecek metin bulamaz, sunucu
/// onaysız kaydı reddeder ve uygulama hiç yeni kullanıcı alamaz hâle gelirdi. Belirti
/// "kayıt olmuyor"dur, sebep hiçbir ekranda yazmaz (§7 madde 65'in "kaç eksen?" dersi).
/// </remarks>
public class LegalConsentRulesTests
{
    private static LegalDocument Doc(
        string type, bool mandatory = true, bool active = true, bool atRegistration = true,
        params LegalDocumentVersion[] versions)
    {
        var document = new LegalDocument
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = type,
            IsMandatory = mandatory,
            IsActive = active,
            ShowAtRegistration = atRegistration
        };
        document.Versions.AddRange(versions);
        return document;
    }

    private static LegalDocumentVersion Version(int number, bool published, bool superseded = false)
    {
        var version = new LegalDocumentVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = number,
            Body = $"<p>v{number}</p>",
            EffectiveFrom = DateTime.UtcNow
        };

        if (published) version.Publish(Guid.NewGuid(), DateTime.UtcNow);
        if (superseded) version.Supersede(DateTime.UtcNow);
        return version;
    }

    private static IQueryable<LegalDocument> Query(params LegalDocument[] documents) =>
        documents.AsQueryable();

    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ADocumentWithOnlyADraft_IsNotAvailable()
    {
        var documents = Query(Doc(LegalDocumentTypes.Kvkk, versions: Version(1, published: false)));

        LegalConsentRules.Available(documents).Should().BeEmpty();
    }

    /// <summary>
    /// 🔴 <b>Bu testin asıl iddiası:</b> zorunlu ama metni yayınlanmamış bir belge
    /// <b>kaydı bloklamaz</b>. Bloklasaydı uygulama hiç kayıt alamaz hâle gelirdi.
    /// </summary>
    [Fact]
    public void AMandatoryDocumentWithoutAPublishedVersion_IsNotMandatory()
    {
        var documents = Query(Doc(LegalDocumentTypes.Kvkk, mandatory: true, versions: Version(1, published: false)));

        LegalConsentRules.Mandatory(documents).Should().BeEmpty(
            "metni olmayan bir belge sorulamaz; zorunlu tutulması kaydı tamamen kilitlerdi");
    }

    /// <summary>Ters yön — yoksa "hiçbir şeyi zorunlu tutma" gerçeklemesi de yeşil kalırdı.</summary>
    [Fact]
    public void AMandatoryDocumentWithAPublishedVersion_IsMandatory()
    {
        var documents = Query(Doc(LegalDocumentTypes.Kvkk, mandatory: true, versions: Version(1, published: true)));

        LegalConsentRules.Mandatory(documents).Should().HaveCount(1);
    }

    [Fact]
    public void APassiveDocument_IsNeverAvailable_EvenWithALiveVersion()
    {
        var documents = Query(Doc(LegalDocumentTypes.Kvkk, active: false, versions: Version(1, published: true)));

        LegalConsentRules.Available(documents).Should().BeEmpty();
    }

    [Fact]
    public void ADocumentWhoseOnlyPublishedVersionWasSuperseded_IsNotAvailable()
    {
        var documents = Query(Doc(
            LegalDocumentTypes.Kvkk,
            versions: Version(1, published: true, superseded: true)));

        LegalConsentRules.Available(documents).Should().BeEmpty(
            "yürürlükten kalkmış metin gösterilemez; ona rıza almak da alınmamış sayılır");
    }

    [Fact]
    public void LiveVersionOf_PicksThePublishedAndNotSupersededOne()
    {
        var old = Version(1, published: true, superseded: true);
        var live = Version(2, published: true);
        var draft = Version(3, published: false);

        var document = Doc(LegalDocumentTypes.Kvkk, versions: new[] { old, live, draft });

        LegalConsentRules.LiveVersionOf(document)!.VersionNumber.Should().Be(2);
    }

    /// <summary>
    /// 🔴 <c>AtRegistration</c> ile <c>Mandatory</c> <b>ayrı</b> sorulardır ve
    /// <c>Mandatory</c> bilerek <c>ShowAtRegistration</c>'a bakmaz: zorunlu ama gösterilmeyen
    /// bir belgeyi "gösterilmiyorsa zorunlu değildir" diye yorumlamak, yöneticinin zorunlu
    /// işaretlediği metni <b>hiç kimseye sormadan</b> geçmek olurdu. Panel bu tutarsızlığı
    /// uyarı olarak gösterir; kural onu yutmaz.
    /// </summary>
    [Fact]
    public void AMandatoryDocumentHiddenFromRegistration_IsStillMandatory_ButIsNotShown()
    {
        var documents = Query(Doc(
            LegalDocumentTypes.Kvkk, mandatory: true, atRegistration: false,
            versions: Version(1, published: true)));

        LegalConsentRules.AtRegistration(documents).Should().BeEmpty();
        LegalConsentRules.Mandatory(documents).Should().HaveCount(1);
    }
}
