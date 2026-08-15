using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Legal;

/// <summary>
/// Faz 12.17 (plan dışı ek) — <c>GET /v1/legal/versions/{id}</c>:
/// <b>"ben neyi onaylamıştım?"</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Ucun varlık sebebi bir boşluktu:</b> 12.16 rızayı sürüme bağladı ve
/// <c>GET /v1/users/me/consents</c> onaylanan sürümün kimliğini <i>söylüyordu</i> — ama o
/// kimlikten <b>metne</b> giden bir yol yoktu. Yönetici yeni sürüm yayınladığı an vatandaş,
/// kabul ettiği metni bir daha <b>hiç göremiyordu</b>: kanıt bizdeydi, <b>sahibinde</b>
/// değildi.
/// </para>
/// <para>
/// ⚠️ Kilit <b>iki yönlü</b> olmak zorunda (§7 madde 68'in dersi): yalnız
/// <i>"taslak 404 döner"</i> iddiası, <b>hiçbir sürümü döndürmeyen</b> bir gerçeklemede de
/// yeşil kalırdı. Bu yüzden aynı satırın <b>yayınlandığı anda döndüğü</b> de ölçülüyor.
/// </para>
/// </remarks>
public class LegalVersionEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private const string Marker = "CLAUDE-LEGALVER";

    private Guid _liveVersionId;
    private Guid _supersededVersionId;
    private Guid _draftVersionId;

    public LegalVersionEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task InitializeAsync()
    {
        await CleanAsync();

        await WithScopeAsync(async db =>
        {
            var superseded = NewVersion(1, "Eski metin");
            superseded.Publish(Guid.NewGuid(), DateTime.UtcNow.AddDays(-30));
            superseded.Supersede(DateTime.UtcNow.AddDays(-1));

            var live = NewVersion(2, "Yürürlükteki metin");
            live.Publish(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));

            // Taslak: hiç yayınlanmadı → uçtan **görünmemeli**.
            var draft = NewVersion(3, "Henüz yayınlanmamış taslak");

            var document = new LegalDocument
            {
                Type = $"{Marker}-belge",
                Title = "Sürüm Testi Belgesi",
                IsMandatory = false,
                ShowAtRegistration = false,
                IsActive = true
            };
            document.Versions.Add(superseded);
            document.Versions.Add(live);
            document.Versions.Add(draft);

            db.Set<LegalDocument>().Add(document);
            await db.SaveChangesAsync();

            _supersededVersionId = superseded.Id;
            _liveVersionId = live.Id;
            _draftVersionId = draft.Id;
        });
    }

    public Task DisposeAsync() => CleanAsync();

    private static LegalDocumentVersion NewVersion(int number, string body) => new()
    {
        VersionNumber = number,
        Body = $"<p>{body}</p>",
        Summary = $"{body} özeti",
        EffectiveFrom = DateTime.UtcNow.AddDays(-40)
    };

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

        await db.Set<UserConsent>().Where(c => versionIds.Contains(c.DocumentVersionId)).ExecuteDeleteAsync();
        await db.Set<LegalDocumentVersion>().Where(v => documentIds.Contains(v.DocumentId)).ExecuteDeleteAsync();
        await db.Set<LegalDocument>().Where(d => documentIds.Contains(d.Id)).ExecuteDeleteAsync();
    });

    private async Task<JsonElement> GetDataAsync(Guid versionId)
    {
        var response = await _client.GetAsync($"/v1/legal/versions/{versionId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    // ─────────────────────────── testler ───────────────────────────

    [Fact]
    public async Task ASupersededVersion_IsStillReadable_BecauseThatIsTheWholePointOfTheEndpoint()
    {
        // 🔑 Kullanıcının onayladığı metin, yerini yeni bir sürüme bırakmış olsa bile
        // okunabilmeli — yoksa rıza kaydı "bir metne işaret eden ama o metni
        // gösteremeyen" bir kayda döner (bloğun kapatmak için yazıldığı hasarın ta kendisi).
        var data = await GetDataAsync(_supersededVersionId);

        data.GetProperty("versionNumber").GetInt32().Should().Be(1);
        data.GetProperty("body").GetString().Should().Contain("Eski metin");
        data.GetProperty("isLive").GetBoolean().Should().BeFalse();
        data.GetProperty("supersededAt").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task ALiveVersion_SaysItIsLive_SoTheScreenDoesNotWarnWithoutReason()
    {
        // ⚠️ İkinci yön: `isLive` her zaman `false` dönen bir gerçekleme,
        // yukarıdaki testi de geçerdi ve ekran **yürürlükteki** metnin üstüne de
        // "artık geçerli değil" uyarısı basardı.
        var data = await GetDataAsync(_liveVersionId);

        data.GetProperty("versionNumber").GetInt32().Should().Be(2);
        data.GetProperty("isLive").GetBoolean().Should().BeTrue();
        data.GetProperty("supersededAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ADraftVersion_IsNotFound_SoUnpublishedLegalTextNeverLeaks()
    {
        // 🔴 Taslak "bulunamadı"dır: var olduğunu bile söylemiyoruz. Dönseydi henüz
        // yayınlanmamış bir hukuki metin, kimliğini eline geçiren herkese açılır ve
        // kullanıcı onu **yürürlükteki metin sanabilirdi**.
        var response = await _client.GetAsync($"/v1/legal/versions/{_draftVersionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TheEndpointIsAnonymous_BecauseTheReaderMayNotBeSignedInYet()
    {
        // Kayıt akışındaki kullanıcı henüz kayıtlı değil; kardeş uçlarla aynı gerekçe.
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/v1/legal/versions/{_liveVersionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnUnknownVersionId_IsNotFound_InsteadOfFallingBackToTheLiveOne()
    {
        // ⚠️ 12.16'nın `{type}` kararının aynısı: yanlış hukuki metni göstermek,
        // kullanıcıya okumadığı bir belgeyi onaylatmanın en sessiz yoludur.
        var response = await _client.GetAsync($"/v1/legal/versions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ADeactivatedDocumentsVersion_IsStillReadable_BecauseTheProofMustOutliveTheSwitch()
    {
        // ⚠️ Kardeş uçların `Available` süzgecinin **bilinçli tersi**: bu uç
        // "bugün ne soruluyor?"u değil "ben neyi onaylamıştım?"ı cevaplıyor.
        // Yönetici belgeyi pasifleştirdiğinde geçmişte verilmiş rızanın metni
        // okunamaz hâle gelseydi, kanıt tam da onu isteyen kişi için kaybolurdu —
        // üstelik tek bir panel anahtarıyla ve hiçbir uyarı olmadan.
        await WithScopeAsync(async db =>
        {
            var document = await db.Set<LegalDocument>()
                .FirstAsync(d => d.Type.StartsWith(Marker));
            document.IsActive = false;
            await db.SaveChangesAsync();
        });

        var data = await GetDataAsync(_supersededVersionId);

        data.GetProperty("body").GetString().Should().Contain("Eski metin");
    }
}
