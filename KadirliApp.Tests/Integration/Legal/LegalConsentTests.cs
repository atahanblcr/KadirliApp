using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Legal;

/// <summary>
/// Faz 12.16 — KVKK rıza akışının uçtan uca doğrulaması (gerçek Postgres).
/// </summary>
/// <remarks>
/// Kilitlediği görünmez sözleşmeler: <b>71</b> (rıza kullanıcının GÖRDÜĞÜ sürüme yazılır),
/// <b>73</b> (rıza satırı kullanıcıyla AYNI işlemde yazılır), <b>74</b> (hesap silinince
/// rıza kaydı KALIR).
/// </remarks>
public class LegalConsentTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Testin kendi belgelerini tanıdığı <b>değişmeyen</b> işaretçi (temizlik buna göre).</summary>
    private const string Marker = "CLAUDE-LEGAL";

    private Guid _mandatoryVersionId;
    private Guid _optionalVersionId;
    private Guid _mandatoryDocumentId;

    public LegalConsentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ─────────────────────────── kurulum ───────────────────────────

    public async Task InitializeAsync()
    {
        await CleanAsync();

        await WithScopeAsync(async db =>
        {
            var mandatory = NewDocument($"{Marker}-zorunlu", "Zorunlu Metin", isMandatory: true);
            var optional = NewDocument($"{Marker}-istege-bagli", "İsteğe Bağlı İzin", isMandatory: false);

            db.Set<LegalDocument>().AddRange(mandatory, optional);
            await db.SaveChangesAsync();

            _mandatoryDocumentId = mandatory.Id;
            _mandatoryVersionId = mandatory.Versions.Single().Id;
            _optionalVersionId = optional.Versions.Single().Id;
        });
    }

    public Task DisposeAsync() => CleanAsync();

    private static LegalDocument NewDocument(string type, string title, bool isMandatory)
    {
        var version = new LegalDocumentVersion
        {
            VersionNumber = 1,
            Body = $"<p>{title} metni</p>",
            Summary = $"{title} özeti",
            EffectiveFrom = DateTime.UtcNow
        };
        version.Publish(Guid.NewGuid(), DateTime.UtcNow);

        var document = new LegalDocument
        {
            Type = type,
            Title = title,
            IsMandatory = isMandatory,
            ShowAtRegistration = true,
            IsActive = true,
            SortOrder = isMandatory ? 0 : 1
        };
        document.Versions.Add(version);
        return document;
    }

    private async Task WithScopeAsync(Func<AppDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task CleanAsync() => await WithScopeAsync(async db =>
    {
        var documentIds = await db.Set<LegalDocument>()
            .Where(d => d.Type.StartsWith(Marker))
            .Select(d => d.Id)
            .ToListAsync();

        if (documentIds.Count == 0) return;

        var versionIds = await db.Set<LegalDocumentVersion>()
            .Where(v => documentIds.Contains(v.DocumentId))
            .Select(v => v.Id)
            .ToListAsync();

        // ⚠️ Sıra FK'lardan geliyor: ikisi de `Restrict` (silme sessiz veri kaybı olmasın diye).
        await db.Set<UserConsent>().Where(c => versionIds.Contains(c.DocumentVersionId)).ExecuteDeleteAsync();
        await db.Set<LegalDocumentVersion>().Where(v => documentIds.Contains(v.DocumentId)).ExecuteDeleteAsync();
        await db.Set<LegalDocument>().Where(d => documentIds.Contains(d.Id)).ExecuteDeleteAsync();
    });

    private static string NewPhone() => $"+9055{Random.Shared.Next(10_000_000, 99_999_999)}";
    private static string NewUsername() => $"kvkk{Guid.NewGuid():N}"[..20];

    private async Task<Guid> NeighborhoodIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync();
    }

    private async Task<string> PhoneTempTokenAsync(string phone)
    {
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).EnsureSuccessStatusCode();

        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("tempToken").GetString()!;
    }

    private async Task<HttpResponseMessage> RegisterAsync(
        string phone, string tempToken, IEnumerable<object>? consents)
    {
        return await _client.PostAsJsonAsync("/v1/auth/register", new
        {
            tempToken,
            username = NewUsername(),
            primaryNeighborhoodId = await NeighborhoodIdAsync(),
            age = 30,
            consents
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // 1) Anonim okuma — kullanıcı henüz kayıtlı DEĞİL
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Uç anonim olmak <b>zorunda</b>: rızayı vermeden önce metni okuması gereken kişinin
    /// henüz hesabı yok. Jeton istenseydi kayıt akışı kendi kendini kilitlerdi.
    /// </summary>
    [Fact]
    public async Task LegalDocuments_AreReadableWithoutAToken_AndCarryTheirBody()
    {
        var response = await _client.GetAsync("/v1/legal/documents?registrationOnly=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();

        var mandatory = items.Single(x => x.GetProperty("versionId").GetGuid() == _mandatoryVersionId);
        mandatory.GetProperty("isMandatory").GetBoolean().Should().BeTrue();
        mandatory.GetProperty("body").GetString().Should().Contain("Zorunlu Metin metni",
            "metin ikinci bir istek gerektirmeden gelmeli — kayıt akışında her ağ turu bir kayıp");
    }

    /// <summary>
    /// ⚠️ Tanınmayan tür <b>varsayılana düşmez</b>: yanlış hukuki metni göstermek,
    /// kullanıcıya okumadığı bir belgeyi onaylatmanın en sessiz yoludur.
    /// </summary>
    [Fact]
    public async Task AnUnknownDocumentType_Is404_NotADefaultDocument()
    {
        var response = await _client.GetAsync("/v1/legal/documents/acik-riza-yanlis-yazim");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>Yayında sürümü olmayan belge listede <b>hiç görünmez</b> (§7 madde 71'in ön koşulu).</summary>
    [Fact]
    public async Task ADocumentWhoseVersionIsOnlyADraft_IsNotListed()
    {
        Guid draftOnlyId = Guid.Empty;

        await WithScopeAsync(async db =>
        {
            var version = new LegalDocumentVersion
            {
                VersionNumber = 1, Body = "<p>taslak</p>", EffectiveFrom = DateTime.UtcNow
            };
            var document = new LegalDocument
            {
                Type = $"{Marker}-taslak", Title = "Taslak Belge",
                IsMandatory = false, ShowAtRegistration = true, IsActive = true
            };
            document.Versions.Add(version);
            db.Set<LegalDocument>().Add(document);
            await db.SaveChangesAsync();
            draftOnlyId = document.Id;
        });

        var response = await _client.GetAsync("/v1/legal/documents");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => x.GetProperty("id").GetGuid())
            .Should().NotContain(draftOnlyId);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2) Kayıt — zorunlu rıza olmadan tamamlanmaz VE SEBEBİNİ SÖYLER
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithoutTheMandatoryConsent_IsRejected_AndNamesTheDocument()
    {
        var phone = NewPhone();
        var response = await RegisterAsync(phone, await PhoneTempTokenAsync(phone), consents: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MISSING_CONSENT");
        body.Should().Contain("Zorunlu Metin",
            "reddetmek yetmez: kullanıcının ekranında HANGİ kutunun eksik olduğu yazmalı");
    }

    /// <summary>
    /// ⚠️ <c>granted=false</c> göndermek, <b>hiç göndermemekle aynı</b> sonucu verir
    /// (kayıt reddedilir) — ama sebebi farklıdır ve bu ayrım kaydın kendisinde durur.
    /// </summary>
    [Fact]
    public async Task Register_WithTheMandatoryConsentDenied_IsAlsoRejected()
    {
        var phone = NewPhone();
        var response = await RegisterAsync(phone, await PhoneTempTokenAsync(phone),
            new[] { new { versionId = _mandatoryVersionId, granted = false } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("MISSING_CONSENT");
    }

    /// <summary>
    /// 🔴 <b>GÖRÜNMEZ SÖZLEŞME 73'ün asıl kilidi.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// İddia "400 döndü" <b>değil</b>: reddedilen bir kayıt arkasında <b>hiçbir iz
    /// bırakmamalı</b>. Rıza doğrulaması <c>SaveChanges</c>'ten <b>sonraya</b> alınsaydı
    /// (ya da rıza satırları ayrı bir işlemde yazılsaydı) kullanıcı satırı <b>yazılmış
    /// olurdu</b> ve telefon "zaten kayıtlı" hâline gelirdi — vatandaş bir daha
    /// <b>hiçbir zaman</b> kayıt olamaz, üstelik hesabı da yoktur.
    /// </para>
    /// <para>
    /// 🔑 Ölçüt bilerek <b>ikinci denemenin başarısı</b>: "kullanıcı tablosunda satır var mı"
    /// diye bakmak da bir iddia olurdu ama <b>bu</b> iddia hasarın kendisini ölçüyor.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ARejectedRegistration_LeavesNoUserBehind_SoThePhoneStaysRegisterable()
    {
        var phone = NewPhone();

        var failed = await RegisterAsync(phone, await PhoneTempTokenAsync(phone), consents: null);
        failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Aynı telefonla, bu kez rızayı vererek: başarılı olmalı.
        var succeeded = await RegisterAsync(phone, await PhoneTempTokenAsync(phone),
            new[] { new { versionId = _mandatoryVersionId, granted = true } });

        succeeded.StatusCode.Should().Be(HttpStatusCode.OK,
            "rızasız kayıt kullanıcı satırı bırakmış olsaydı burada 'bu numara zaten kayıtlı' alırdık");
    }

    /// <summary>
    /// Mutlu yol + <b>"sorduk, hayır dedi"</b> kaydının gerçekten yazıldığı.
    /// </summary>
    /// <remarks>
    /// ⚠️ Yalnız <c>true</c> yazılsaydı <i>"sormadık"</i> ile <i>"sorduk, hayır dedi"</i>
    /// farkı <b>hiçbir yerde durmazdı</b> — ikisi de "satır yok" olurdu. KVKK'da bu fark
    /// anlamlıdır.
    /// </remarks>
    [Fact]
    public async Task Register_WritesBothTheGrantedAndTheDeniedConsent_InTheSameTransaction()
    {
        var phone = NewPhone();

        var response = await RegisterAsync(phone, await PhoneTempTokenAsync(phone), new object[]
        {
            new { versionId = _mandatoryVersionId, granted = true },
            new { versionId = _optionalVersionId, granted = false }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await WithScopeAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Phone == phone);

            var consents = await db.Set<UserConsent>()
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            consents.Should().HaveCount(2, "reddedilen izin de KAYDEDİLİR");

            var granted = consents.Single(c => c.DocumentVersionId == _mandatoryVersionId);
            granted.Granted.Should().BeTrue();
            granted.Source.Should().Be(ConsentSources.Registration);
            granted.RevokedAt.Should().BeNull("hiç onaylanmamış bir rıza 'geri alınmış' görünmemeli");

            var denied = consents.Single(c => c.DocumentVersionId == _optionalVersionId);
            denied.Granted.Should().BeFalse();
            denied.RevokedAt.Should().BeNull(
                "'hayır dedi' ile 'verdi, sonra geri aldı' AYRI şeylerdir — defterde karışmamalı");
        });
    }

    /// <summary>
    /// 🔴 <b>GÖRÜNMEZ SÖZLEŞME 71.</b> Rıza, kullanıcının <b>gördüğü</b> sürüme yazılır;
    /// sunucu "o anki yayında sürüm"ü kendi başına seçmez.
    /// </summary>
    /// <remarks>
    /// Burada kullanıcı <b>yürürlükten kalkmış</b> bir sürümün kimliğini gönderiyor
    /// (formu doldururken yönetici yeni sürüm yayınlamış). Sunucu bunu <b>kabul etmez</b>:
    /// etseydi kayıt, kullanıcının <b>okuduğu</b> metni değil kabul, o an ekranda
    /// <b>göremeyeceği</b> bir metni işaret ederdi. Bedeli — kullanıcı ekranı tazeleyip
    /// yeni metni onaylar — bilinçlidir ve nadirdir; alternatifi sessizce yanlış kanıttır.
    /// </remarks>
    [Fact]
    public async Task Register_RejectsAConsentGivenToASupersededVersion()
    {
        var supersededVersionId = _mandatoryVersionId;

        // Yönetici yeni sürüm yayınlıyor (eskisi yürürlükten kalkıyor).
        await WithScopeAsync(async db =>
        {
            var current = await db.Set<LegalDocumentVersion>()
                .SingleAsync(v => v.Id == supersededVersionId);
            current.Supersede(DateTime.UtcNow);

            var next = new LegalDocumentVersion
            {
                DocumentId = _mandatoryDocumentId,
                VersionNumber = 2,
                Body = "<p>Zorunlu Metin v2</p>",
                EffectiveFrom = DateTime.UtcNow
            };
            next.Publish(Guid.NewGuid(), DateTime.UtcNow);
            db.Set<LegalDocumentVersion>().Add(next);

            await db.SaveChangesAsync();
        });

        var phone = NewPhone();
        var response = await RegisterAsync(phone, await PhoneTempTokenAsync(phone),
            new[] { new { versionId = supersededVersionId, granted = true } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("MISSING_CONSENT");

        await WithScopeAsync(async db =>
        {
            (await db.Set<UserConsent>().AnyAsync(c => c.DocumentVersionId == supersededVersionId))
                .Should().BeFalse("yürürlükten kalkmış metne rıza yazılmaz");
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3) Ayarlar akışı ve hesap silme
    // ────────────────────────────────────────────────────────────────────────

    private async Task<(string Access, Guid UserId, string Phone)> RegisteredUserAsync()
    {
        var phone = NewPhone();
        var response = await RegisterAsync(phone, await PhoneTempTokenAsync(phone),
            new[] { new { versionId = _mandatoryVersionId, granted = true } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var access = doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;

        Guid userId = Guid.Empty;
        await WithScopeAsync(async db =>
            userId = (await db.Users.SingleAsync(u => u.Phone == phone)).Id);

        return (access, userId, phone);
    }

    private HttpClient AuthedClient(string access)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return client;
    }

    [Fact]
    public async Task MyConsents_ListsEveryLiveDocument_IncludingTheOnesNeverAnswered()
    {
        var (access, _, _) = await RegisteredUserAsync();

        var response = await AuthedClient(access).GetAsync("/v1/users/me/consents");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();

        var optional = items.Single(x => x.GetProperty("currentVersionId").GetGuid() == _optionalVersionId);
        optional.GetProperty("consentedVersionId").ValueKind.Should().Be(JsonValueKind.Null,
            "hiç sorulmamış izin de listede DURMALI — yoksa kullanıcı onu verecek bir yol bulamaz");
        optional.GetProperty("granted").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AnOptionalConsent_CanBeGrantedAndThenRevoked_FromSettings()
    {
        var (access, userId, _) = await RegisteredUserAsync();
        var client = AuthedClient(access);

        var grant = await client.PostAsJsonAsync("/v1/users/me/consents", new
        {
            consents = new[] { new { versionId = _optionalVersionId, granted = true } }
        });
        grant.StatusCode.Should().Be(HttpStatusCode.OK);

        await WithScopeAsync(async db =>
        {
            var consent = await db.Set<UserConsent>()
                .SingleAsync(c => c.UserId == userId && c.DocumentVersionId == _optionalVersionId);
            consent.Granted.Should().BeTrue();
            consent.Source.Should().Be(ConsentSources.Settings, "kaynak SUNUCUDA sabitlenir");
        });

        var revoke = await client.PostAsJsonAsync("/v1/users/me/consents", new
        {
            consents = new[] { new { versionId = _optionalVersionId, granted = false } }
        });
        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        await WithScopeAsync(async db =>
        {
            var consent = await db.Set<UserConsent>()
                .SingleAsync(c => c.UserId == userId && c.DocumentVersionId == _optionalVersionId);
            consent.Granted.Should().BeFalse();
            consent.RevokedAt.Should().NotBeNull(
                "geri alma 'hiç onaylamadı'dan ayrılmalı — satır silinseydi ikisi aynı görünürdü");
        });
    }

    /// <summary>
    /// 🔴 Zorunlu rıza ayarlardan geri <b>alınamaz</b> — karşılığı var olan
    /// <c>DELETE /v1/users/me</c>'dir (10.8). İkinci bir yol açılsaydı hesabı duran ama
    /// işlemesinin dayanağı olmayan bir kullanıcı doğardı.
    /// </summary>
    [Fact]
    public async Task AMandatoryConsent_CannotBeRevokedFromSettings_AndSaysWhy()
    {
        var (access, userId, _) = await RegisteredUserAsync();

        var response = await AuthedClient(access).PostAsJsonAsync("/v1/users/me/consents", new
        {
            consents = new[] { new { versionId = _mandatoryVersionId, granted = false } }
        });

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("MANDATORY_CONSENT");
        body.Should().Contain("hesabınızı silmeniz", "kullanıcıya YAPABİLECEĞİ şey söylenmeli");

        await WithScopeAsync(async db =>
        {
            var consent = await db.Set<UserConsent>()
                .SingleAsync(c => c.UserId == userId && c.DocumentVersionId == _mandatoryVersionId);
            consent.Granted.Should().BeTrue("reddedilen istek kaydı DEĞİŞTİRMEMELİ");
        });
    }

    /// <summary>
    /// 🔴 <b>GÖRÜNMEZ SÖZLEŞME 74</b> — ve bu, 12.7'nin <c>user_identities</c> kararının
    /// <b>bilinçli tersi</b>.
    /// </summary>
    /// <remarks>
    /// Fark kaydın <i>cinsinde</i>: sosyal kimlik <b>kanıt değeri olmayan kişisel veridir</b>
    /// → silinir; rıza kaydı <b>işlemenin hukuki dayanağının kanıtıdır</b> → silinirse
    /// geçmişte yapılmış işlemenin dayanağı kaybolur. Hesap silme zaten anonimleştirmedir
    /// (10.8), yani satır <b>anonim bir kullanıcıya</b> bağlı kalır.
    /// <para>
    /// ⚠️ İddia iki yönlü ve bu şart: yalnız "rıza duruyor" denseydi, <b>hiçbir şeyi
    /// silmeyen</b> bir hesap silme gerçeklemesi de yeşil kalırdı. Kimliğin gerçekten
    /// silindiği de ölçülüyor.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task DeletingTheAccount_KeepsTheConsentRecord_ButAnonymisesTheUser()
    {
        var (access, userId, phone) = await RegisteredUserAsync();

        var deletion = await AuthedClient(access).DeleteAsync("/v1/users/me");
        deletion.StatusCode.Should().Be(HttpStatusCode.OK);

        await WithScopeAsync(async db =>
        {
            var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == userId);
            user.DeletedAt.Should().NotBeNull();
            user.Phone.Should().NotBe(phone, "hesap silme anonimleştirmedir — telefon yeniden kayda açılır");

            var consent = await db.Set<UserConsent>()
                .SingleOrDefaultAsync(c => c.UserId == userId && c.DocumentVersionId == _mandatoryVersionId);

            consent.Should().NotBeNull(
                "rıza kaydı KALIR (§7 madde 74): silinirse geçmişte yapılmış işlemenin hukuki " +
                "dayanağı kaybolur. Bu, sosyal kimlik kararının bilinçli TERSİDİR.");
            consent!.Granted.Should().BeTrue();
        });
    }
}
