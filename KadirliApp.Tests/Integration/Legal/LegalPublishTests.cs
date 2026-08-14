using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Application.Features.Legal.Commands;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Legal;

/// <summary>
/// Faz 12.16 — <b>"aynı anda en fazla BİR yayında sürüm"</b> kuralının, kuralı taşıyan
/// kısmi unique indeksin <b>kendisini patlatmadan</b> uygulandığının kanıtı.
/// </summary>
/// <remarks>
/// <para>
/// 🐛 <b>Bu dosya bir gerçek hatadan doğdu ve o hata BOZMA TURUNDA bulundu.</b> İlk yazımda
/// yürürlükten kaldırma ile yayınlama <b>tek</b> <c>SaveChanges</c>'teydi; testler üst üste
/// <b>üç kez yeşil</b> koştu. Ölçüldüğünde gerçek şu çıktı: aynı senaryonun <b>8 koşusundan
/// 5'i</b> <c>23505</c> ile düşüyordu.
/// </para>
/// <para>
/// 🔑 Sebep: kısmi unique indeks Postgres'te <b>deyim başına</b> denetlenir; EF ise aynı
/// tablonun UPDATE'lerini <b>birincil anahtar sırasına</b> göre gönderir ve anahtarlar
/// <c>gen_random_uuid()</c> olduğu için sıra <b>rastgeledir</b>. Yani hata, yayınlanan
/// sürümün GUID'i eskisinden küçük geldiğinde çıkıyordu — <i>"bende çalışıyor"</i> diyen
/// geliştirici tamamen haklı olabilirdi.
/// </para>
/// <para>
/// ⚠️ <b>Bu yüzden test TEKRARLI.</b> Tek geçişe bakan bir test bu hatayı <b>%37 olasılıkla
/// kaçırırdı</b> — ve tam olarak öyle de oldu. Tekrar sayısı, yanlış yeşilin olasılığını
/// ihmal edilebilir hâle getiriyor (0.375^10 ≈ %0.005). 📌 Ders: <i>rastgeleliğe bağlı bir
/// hata, tek koşuluk bir testle kilitlenemez.</i>
/// </para>
/// </remarks>
public class LegalPublishTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Marker = "CLAUDE-PUBLISH";
    private const int Rounds = 10;

    private readonly CustomWebApplicationFactory _factory;

    public LegalPublishTests(CustomWebApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => CleanAsync();
    public Task DisposeAsync() => CleanAsync();

    [Fact]
    public async Task PublishingOverALiveVersion_AlwaysSucceeds_AndLeavesExactlyOneLiveVersion()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var (documentId, draftId, liveId) = await SeedLiveAndDraftAsync(round);

            using (var scope = _factory.Services.CreateScope())
            {
                var response = await scope.ServiceProvider.GetRequiredService<ISender>()
                    .Send(new PublishLegalVersionCommand { Id = draftId, AdminId = Guid.NewGuid() });

                response.Success.Should().BeTrue(
                    "yayınlama, kısmi unique indeksi ihlal etmeden koşmalı (tur {0}) — " +
                    "burada düşüyorsa yürürlükten kaldırma ile yayınlama yine TEK SaveChanges'e " +
                    "toplanmış olabilir: EF'in UPDATE sırası rastgeledir", round);
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var versions = await db.Set<LegalDocumentVersion>()
                    .Where(v => v.DocumentId == documentId).ToListAsync();

                versions.Count(v => v.IsLive).Should().Be(1,
                    "kural 'aynı anda en fazla BİR yayında sürüm' (tur {0})", round);

                versions.Single(v => v.Id == draftId).IsLive.Should().BeTrue();
                versions.Single(v => v.Id == liveId).SupersededAt.Should().NotBeNull(
                    "eski sürüm yürürlükten kalkmalı — ama SİLİNMEMELİ: ona verilmiş rızalar onu işaret ediyor");
            }
        }
    }

    /// <summary>
    /// Ters yön: yayınlanmış bir sürümü <b>ikinci kez</b> yayınlamak sessizce başarılı
    /// olmamalı — yoksa "yayınla" butonu her basışta eskiyi yeniden yürürlüğe sokardı.
    /// </summary>
    [Fact]
    public async Task PublishingAnAlreadyPublishedVersion_IsRejected()
    {
        var (_, draftId, liveId) = await SeedLiveAndDraftAsync(round: 99);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var response = await sender.Send(new PublishLegalVersionCommand { Id = liveId });

        response.Success.Should().BeFalse();
        response.Error!.Code.Should().Be("CONFLICT");

        // Ve taslak hâlâ taslak — reddedilen istek başka bir şeyi yayınlamış olmamalı.
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Set<LegalDocumentVersion>().SingleAsync(v => v.Id == draftId))
            .PublishedAt.Should().BeNull();
    }

    // ─────────────────────────── yardımcılar ───────────────────────────

    private async Task<(Guid DocumentId, Guid DraftId, Guid LiveId)> SeedLiveAndDraftAsync(int round)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var live = new LegalDocumentVersion
        {
            VersionNumber = 1, Body = "<p>yayındaki</p>", EffectiveFrom = DateTime.UtcNow
        };
        live.Publish(Guid.NewGuid(), DateTime.UtcNow);

        var draft = new LegalDocumentVersion
        {
            VersionNumber = 2, Body = "<p>taslak</p>", EffectiveFrom = DateTime.UtcNow
        };

        var document = new LegalDocument
        {
            Type = $"{Marker}-{round}", Title = $"{Marker} {round}", IsActive = true
        };
        document.Versions.Add(live);
        document.Versions.Add(draft);

        db.Set<LegalDocument>().Add(document);
        await db.SaveChangesAsync();

        return (document.Id, draft.Id, live.Id);
    }

    private async Task CleanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var documentIds = await db.Set<LegalDocument>()
            .Where(d => d.Type.StartsWith(Marker)).Select(d => d.Id).ToListAsync();

        if (documentIds.Count == 0) return;

        var versionIds = await db.Set<LegalDocumentVersion>()
            .Where(v => documentIds.Contains(v.DocumentId)).Select(v => v.Id).ToListAsync();

        // ⚠️ Sıra FK'lardan geliyor: ikisi de `Restrict`.
        await db.Set<UserConsent>().Where(c => versionIds.Contains(c.DocumentVersionId)).ExecuteDeleteAsync();
        await db.Set<LegalDocumentVersion>().Where(v => documentIds.Contains(v.DocumentId)).ExecuteDeleteAsync();
        await db.Set<LegalDocument>().Where(d => documentIds.Contains(d.Id)).ExecuteDeleteAsync();
    }
}
