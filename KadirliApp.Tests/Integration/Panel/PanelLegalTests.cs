extern alias WebPanel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;
using PanelPermissionFilter = WebPanel::KadirliApp.Web.Authorization.PanelPermissionFilter;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.16 — <b>hukuki metin paneli.</b>
/// </summary>
/// <remarks>
/// Bu testlerin iddiası "ekran açılıyor" değil; 12.16'nın kapattığı sessiz hasar sınıfları:
/// <list type="number">
///   <item><b>Yayınlamak düzenlemekten AYRI bir güvendir</b> — <c>Publish</c> öneki elle
///         eklendi ve <c>approve</c>'a düşüyor (§7 madde 19'un <b>yedinci</b> tekrarı).
///         Eklenmeseydi yalnız başlık düzeltme yetkisi olan bir moderatör, şehrin tamamının
///         onayladığı hukuki metni değiştirebilirdi.</item>
///   <item><b>Yayınlanmış metin panelden de değiştirilemez</b> (§7 madde 72) — komut
///         reddediyor <b>ve sebebini söylüyor</b>.</item>
///   <item><b>Rıza defteri matrisin DIŞINDA</b> — IP ve tarayıcı taşıyor (§3,
///         <c>LoginAttemptsAdmin</c> ile birebir aynı gerekçe).</item>
/// </list>
/// </remarks>
[Collection(PanelCollection.Name)]
public class PanelLegalTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    private const string Marker = "CLAUDE-PANEL-LEGAL";
    private const string ModeratorUsername = "legal-moderator";
    private const string ModeratorPassword = "Moderator123!";

    private Guid _moderatorId;
    private Guid _documentId;
    private Guid _liveVersionId;
    private Guid _draftVersionId;

    public PanelLegalTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        var moderator = await _factory.EnsureModeratorAsync(ModeratorUsername, ModeratorPassword);
        _moderatorId = moderator.Id;
        await _factory.ClearMustChangePasswordAsync(ModeratorUsername);

        await CleanAsync();

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var live = new LegalDocumentVersion
            {
                VersionNumber = 1, Body = "<p>yayındaki metin</p>", EffectiveFrom = DateTime.UtcNow
            };
            live.Publish(Guid.NewGuid(), DateTime.UtcNow);

            var draft = new LegalDocumentVersion
            {
                VersionNumber = 2, Body = "<p>taslak metin</p>", EffectiveFrom = DateTime.UtcNow
            };

            var document = new LegalDocument
            {
                Type = $"{Marker}-tur", Title = $"{Marker} Belgesi",
                IsMandatory = true, ShowAtRegistration = true, IsActive = true
            };
            document.Versions.Add(live);
            document.Versions.Add(draft);

            db.Set<LegalDocument>().Add(document);
            await db.SaveChangesAsync();

            _documentId = document.Id;
            _liveVersionId = live.Id;
            _draftVersionId = draft.Id;
        });
    }

    public Task DisposeAsync() => CleanAsync();

    // ────────────────────────────────────────────────────────────────────────
    // 1) İzin deseni
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 <c>Publish</c> <b>elle eklenmek zorundaydı</b>: hiçbir önekle eşleşmiyor ve POST
    /// olduğu için sessizce <c>update</c>'e düşerdi. Bu, §7 madde 19'un <b>yedinci</b>
    /// tekrarı (BulkApprove 11.18 · Archive 12.10 · Unarchive 12.13 · SendNotification 12.15 ·
    /// ResetOverrides + Feature Faz 0 · <b>Publish 12.16</b>).
    /// </summary>
    [Theory]
    [InlineData("Publish", "approve")]
    [InlineData("Edit", "update")]
    [InlineData("CreateVersion", "create")]
    [InlineData("UpdateVersion", "update")]
    public void LegalActions_MapToTheExpectedPermission(string actionName, string expected)
        => PanelPermissionFilter.ActionFor(actionName, "POST").Should().Be(expected);

    /// <summary>
    /// ⚠️ İddia bilerek <b>duruma değil kaydın kendisine</b> bakıyor: bir yönlendirme (302)
    /// hem "reddedildi" hem "başarıyla yapıldı" anlamına gelebilir, yani durum kodu tek
    /// başına bu kuralı kilitlemez (12.13'ün <c>Feature</c> dersi). Ölçüt, taslağın
    /// <b>yayınlanmamış olması</b>.
    /// </summary>
    [Fact]
    public async Task Publish_IsRejected_ForAModeratorWithOnlyUpdatePermission()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "legal",
            CanRead = true, CanUpdate = true, CanApprove = false
        });

        var client = await ModeratorClientAsync();

        await client.PostFormAsync("/LegalAdmin/Publish",
            new Dictionary<string, string>
            {
                ["id"] = _draftVersionId.ToString(),
                ["documentId"] = _documentId.ToString()
            }, "/LegalAdmin");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var draft = await db.Set<LegalDocumentVersion>().SingleAsync(v => v.Id == _draftVersionId);

            draft.PublishedAt.Should().BeNull(
                "yalnız DÜZENLEME yetkisi olan moderatör, şehrin tamamının onaylayacağı " +
                "hukuki metni yayına alamamalı");
        });
    }

    [Fact]
    public async Task Publish_Succeeds_ForAModeratorWithApprovePermission()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "legal",
            CanRead = true, CanUpdate = true, CanApprove = true
        });

        var client = await ModeratorClientAsync();

        await client.PostFormAsync("/LegalAdmin/Publish",
            new Dictionary<string, string>
            {
                ["id"] = _draftVersionId.ToString(),
                ["documentId"] = _documentId.ToString()
            }, "/LegalAdmin");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            // ⚠️ Ters yön ŞART: yalnız "reddedildi" iddiası, HİÇBİR ŞEYİ yayınlamayan bir
            // gerçeklemede de yeşil kalırdı (§7 madde 68'in iki yönlü kilit dersi).
            var draft = await db.Set<LegalDocumentVersion>().SingleAsync(v => v.Id == _draftVersionId);
            draft.PublishedAt.Should().NotBeNull();
            draft.IsLive.Should().BeTrue();

            // Ve eskisi AYNI işlemde yürürlükten kalkmalı — yoksa iki "yayında" sürüm olurdu
            // ve kısmi unique indeks yazmayı reddederdi.
            var previous = await db.Set<LegalDocumentVersion>().SingleAsync(v => v.Id == _liveVersionId);
            previous.SupersededAt.Should().NotBeNull();
            previous.IsLive.Should().BeFalse();
        });
    }

    [Fact]
    public void TheConsentLedger_IsOutsideThePermissionMatrix()
    {
        // §7 madde 20 + 12.2'nin yapısal kuralı: AdminOnlyControllers'taki her controller'ın
        // menü satırında Module NULL olmak zorunda — yoksa izin matrisinde KARŞILIĞI OLMAYAN
        // bir yetki belirir (11.15b'nin en büyük bulgusu).
        PanelMenu.AdminOnlyControllers.Should().Contain("ConsentLedgerAdmin");
        PanelMenu.Items.Single(i => i.Controller == "ConsentLedgerAdmin").Module.Should().BeNull();

        // Metin ekranı ise matriste: metni yazmak/düzeltmek bir içerik işidir.
        PanelMenu.Items.Single(i => i.Controller == "LegalAdmin").Module.Should().Be("legal");
    }

    [Fact]
    public async Task Moderator_WithLegalPermission_SeesTheDocuments_ButNotTheLedger()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "legal", CanRead = true
        });

        var client = await ModeratorClientAsync();

        (await client.GetAsync("/LegalAdmin")).StatusCode.Should().Be(HttpStatusCode.OK);

        // 🔑 Defter yalnız-admin: IP ve tarayıcı imzası taşıyor.
        var ledger = await client.GetAsync("/ConsentLedgerAdmin");
        ledger.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2) Yayınlanmış metin panelden de değiştirilemez (§7 madde 72)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Bu, bloğun var olma sebebi. Değiştirilebilseydi bütün geçmiş rıza kayıtları
    /// <b>retroaktif olarak</b> başka bir metni işaret ederdi — tablo dolu, kanıt yok,
    /// hata yok.
    /// </summary>
    [Fact]
    public async Task APublishedVersion_CannotBeEditedFromThePanel_AndTheTextIsUntouched()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "legal",
            CanRead = true, CanUpdate = true, CanApprove = true
        });

        var client = await ModeratorClientAsync();

        await client.PostFormAsync("/LegalAdmin/UpdateVersion",
            new Dictionary<string, string>
            {
                ["Id"] = _liveVersionId.ToString(),
                ["documentId"] = _documentId.ToString(),
                ["Body"] = "<p>gizlice değiştirilmiş metin</p>",
                ["RequiresReconsent"] = "false"
            }, "/LegalAdmin");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var live = await db.Set<LegalDocumentVersion>().SingleAsync(v => v.Id == _liveVersionId);

            live.Body.Should().Be("<p>yayındaki metin</p>",
                "yayınlanmış metin panelden de değiştirilemez — kullanıcıların onayı ona verildi");
        });
    }

    /// <summary>Ters yön: taslak <b>düzenlenebilir</b> — yoksa "hiçbir şeyi yazma" da yeşil kalırdı.</summary>
    [Fact]
    public async Task ADraft_CanBeEditedFromThePanel()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "legal",
            CanRead = true, CanUpdate = true, CanApprove = true
        });

        var client = await ModeratorClientAsync();

        await client.PostFormAsync("/LegalAdmin/UpdateVersion",
            new Dictionary<string, string>
            {
                ["Id"] = _draftVersionId.ToString(),
                ["documentId"] = _documentId.ToString(),
                ["Body"] = "<p>düzeltilmiş taslak</p>",
                ["RequiresReconsent"] = "true"
            }, "/LegalAdmin");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var draft = await db.Set<LegalDocumentVersion>().SingleAsync(v => v.Id == _draftVersionId);

            draft.Body.Should().Be("<p>düzeltilmiş taslak</p>");
            draft.RequiresReconsent.Should().BeTrue();
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2b) Formdan gelen TARİH — 12.17 canlı doğrulamasının bulduğu gerçek hata
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🐛 <b>12.16'nın en önemli kuralının tek yolu KAPALIYDI ve hiçbir test görmedi.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Panelin <c>&lt;input type="date"&gt;</c> alanı MVC'de <c>Kind=Unspecified</c> bir
    /// <c>DateTime</c> üretiyor; Npgsql ise <c>timestamptz</c> kolonuna yalnız UTC yazıyor.
    /// Sonuç: <b>"Taslak oluştur" 500 veriyordu</b> — yani 12.16'nın *"yayınlanmış metin
    /// değiştirilemez, değiştirmenin tek yolu yeni sürümdür"* kuralının **tek yolu**
    /// çalışmıyordu. Belirti yalnız canlı panelde görülüyordu.
    /// </para>
    /// <para>
    /// 🔑 <b>Testler neden görmedi:</b> 12.16'nın bütün testleri <c>DateTime.UtcNow</c>
    /// veriyordu (<c>Kind=Utc</c>) — kural doğru ölçülüyordu ama <b>panelin gerçekte
    /// ürettiği değerle değil</b>. Bu test tam da o değeri kuruyor: forma bir tarih dizesi
    /// yazıyor ve zincirin ucunda kaydın **gerçekten oluştuğunu** ölçüyor.
    /// 🔑 Ders: <i>bir alanı test ederken, o alana GERÇEKTE ne geldiğini ölç.</i>
    /// </para>
    /// </remarks>
    [Fact]
    public async Task CreateVersion_AcceptsADateComingFromTheForm_NotOnlyAUtcStampFromCode()
    {
        await SetPermissionsAsync(new AdminPermission
        {
            UserId = _moderatorId, Module = "legal",
            CanRead = true, CanCreate = true, CanUpdate = true, CanApprove = true
        });

        var client = await ModeratorClientAsync();

        // Önce açık taslağı kapatmalıyız: komut aynı belgede ikinci taslağa izin vermiyor.
        await client.PostFormAsync("/LegalAdmin/Publish",
            new Dictionary<string, string>
            {
                ["id"] = _draftVersionId.ToString(),
                ["documentId"] = _documentId.ToString()
            }, "/LegalAdmin");

        // ⚠️ Kritik ayrıntı: tarih **form alanı olarak** gidiyor (`yyyy-MM-dd`), yani
        // model bağlayıcı `Kind=Unspecified` üretecek — hatanın doğduğu tam koşul.
        await client.PostFormAsync("/LegalAdmin/CreateVersion",
            new Dictionary<string, string>
            {
                ["DocumentId"] = _documentId.ToString(),
                ["Body"] = "<p>formdan gelen tarihle açılan taslak</p>",
                ["Summary"] = "form tarihi",
                ["EffectiveFrom"] = "2026-09-01",
                ["RequiresReconsent"] = "false"
            }, "/LegalAdmin");

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var created = await db.Set<LegalDocumentVersion>()
                .Where(v => v.DocumentId == _documentId && v.Summary == "form tarihi")
                .SingleOrDefaultAsync();

            created.Should().NotBeNull(
                "panelin tarih alanı `Kind=Unspecified` üretiyor; UTC'ye etiketlenmezse " +
                "Npgsql yazmayı reddediyor ve yeni sürüm açmanın TEK yolu kapanıyor");

            // ⚠️ Gün **kaydırılmamalı**: yönetici "1 Eylül" yazdığında kastettiği o takvim
            // günüdür (§7 madde 6'nın "TR günü, 00:00 UTC" tuzağı).
            created!.EffectiveFrom.Date.Should().Be(new DateTime(2026, 9, 1));
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3) Denetim izi ve Türkçe
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Denetim izi ekranı ham <c>AuditAction</c> değerini basmaz (Değişmez Kural #6).
    /// ⚠️ Üçü <b>ayrı</b> etiket çünkü üçü ayrı ağırlıkta — özellikle <c>publish</c>
    /// geri alınamaz.
    /// </summary>
    [Theory]
    [InlineData("create_legal_version")]
    [InlineData("update_legal_version")]
    [InlineData("publish_legal_version")]
    [InlineData("update_legal_document")]
    public void EveryLegalAuditAction_HasATurkishLabel(string action)
        => PanelDisplay.AuditAction(action).Label.Should().NotContain("Bilinmeyen");

    /// <summary>Rıza kaynağı da ham basılmaz — defterdeki "nasıl alındı" sütunu Türkçedir.</summary>
    [Theory]
    [InlineData("registration")]
    [InlineData("settings")]
    [InlineData("reconsent")]
    public void EveryConsentSource_HasATurkishLabel(string source)
        => PanelDisplay.ConsentSource(source).Label.Should().NotContain("Bilinmeyen");

    // ─────────────────────────── yardımcılar ───────────────────────────

    private async Task<HttpClient> ModeratorClientAsync()
    {
        var client = _factory.CreatePanelClient();
        await client.LoginAsync(ModeratorUsername, ModeratorPassword);
        return client;
    }

    private async Task SetPermissionsAsync(params AdminPermission[] permissions)
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Set<AdminPermission>().Where(p => p.UserId == _moderatorId).ExecuteDeleteAsync();
            if (permissions.Length > 0) db.Set<AdminPermission>().AddRange(permissions);
            await db.SaveChangesAsync();
        });
    }

    private async Task CleanAsync()
    {
        await SetPermissionsAsync();
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            var documentIds = await db.Set<LegalDocument>()
                .Where(d => d.Type.StartsWith(Marker))
                .Select(d => d.Id)
                .ToListAsync();

            if (documentIds.Count == 0) return;

            var versionIds = await db.Set<LegalDocumentVersion>()
                .Where(v => documentIds.Contains(v.DocumentId)).Select(v => v.Id).ToListAsync();

            // ⚠️ Sıra FK'lardan geliyor: ikisi de `Restrict`.
            await db.Set<UserConsent>().Where(c => versionIds.Contains(c.DocumentVersionId)).ExecuteDeleteAsync();
            await db.Set<LegalDocumentVersion>().Where(v => documentIds.Contains(v.DocumentId)).ExecuteDeleteAsync();
            await db.Set<LegalDocument>().Where(d => documentIds.Contains(d.Id)).ExecuteDeleteAsync();
        });
    }
}
