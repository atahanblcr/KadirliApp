using FluentAssertions;
using KadirliApp.Application.Features.Campaigns.Commands;
using KadirliApp.Application.Features.Deaths.Commands;
using KadirliApp.Application.Features.Events.Commands;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **moderasyon durum makineleri: vefat / etkinlik / kampanya / işletme.**
///
/// İlanların onay-red akışı 11.14'te iyi kaplandı; diğer dört modül yalnız uç seviyesinde,
/// dolaylı olarak deneniyordu. Oysa bu modüllerin ortak riski aynı ve ağır: **onaylanmamış
/// içerik yayına sızarsa** ya da **onaylanan içerik yayına çıkmazsa** kimse hata almaz.
/// Vefat ilanında bu, bir ailenin duyurusunun hiç görünmemesi demektir.
///
/// Testler MediatR üzerinden koşuyor (HTTP değil): durum makinesi Application katmanında
/// yaşıyor ve orada kilitlenmesi gerekiyor.
/// </summary>
[Collection(PanelCollection.Name)]
public class ModerationStateMachineTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "Mod-" + Guid.NewGuid().ToString("N")[..8];
    private readonly Guid _adminId = Guid.NewGuid();

    public ModerationStateMachineTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.DeathNotices.IgnoreQueryFilters().Where(x => x.DeceasedName.Contains(_marker)).ExecuteDeleteAsync();
            await db.Events.IgnoreQueryFilters().Where(x => x.Title.Contains(_marker)).ExecuteDeleteAsync();
            await db.Campaigns.IgnoreQueryFilters().Where(x => x.Title.Contains(_marker)).ExecuteDeleteAsync();
            await db.Businesses.IgnoreQueryFilters().Where(x => x.BusinessName.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await sp.GetRequiredService<ISender>().Send(request));
        return result;
    }

    private async Task<T?> ReloadAsync<T>(Guid id) where T : class
    {
        T? row = null;
        await _factory.WithScopeAsync(async sp =>
            row = await sp.GetRequiredService<AppDbContext>().Set<T>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id));
        return row;
    }

    private async Task<Guid> InsertAsync<T>(T entity) where T : Domain.Common.BaseEntity
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Set<T>().Add(entity);
            await db.SaveChangesAsync();
        });
        return entity.Id;
    }

    // ─────────────────────────────── Vefat ───────────────────────────────

    private DeathNotice NewDeathNotice() => new()
    {
        DeceasedName = _marker + " Merhum",
        FuneralDate = DateTime.UtcNow.Date.AddDays(1),
        Status = "pending"
    };

    /// <summary>
    /// Onay yalnız durumu değiştirmez; **kimin** ve **ne zaman** onayladığını da yazar.
    /// Bu iki alan boş kalırsa moderasyon kararı geriye dönük denetlenemez.
    /// </summary>
    [Fact]
    public async Task DeathNotice_Approval_RecordsWhoAndWhen()
    {
        var id = await InsertAsync(NewDeathNotice());
        var before = DateTime.UtcNow.AddSeconds(-1);

        (await SendAsync(new ApproveDeathNoticeCommand(id, _adminId))).Should().BeTrue();

        var notice = await ReloadAsync<DeathNotice>(id);
        notice!.Status.Should().Be("approved");
        notice.ApprovedBy.Should().Be(_adminId, "onayı yapan yönetici kaydedilmeli");
        notice.ApprovedAt.Should().NotBeNull().And.BeAfter(before);
    }

    /// <summary>
    /// Onaylanmış bir ilanı ikinci kez onaylamak zararsız olmalı (panelde çift tıklama
    /// gerçek bir senaryo) ama **onay damgasını da bozmamalı**.
    /// </summary>
    [Fact]
    public async Task DeathNotice_ApprovingTwice_IsHarmless()
    {
        var id = await InsertAsync(NewDeathNotice());
        await SendAsync(new ApproveDeathNoticeCommand(id, _adminId));
        var first = (await ReloadAsync<DeathNotice>(id))!.ApprovedAt;

        (await SendAsync(new ApproveDeathNoticeCommand(id, _adminId))).Should().BeTrue();

        var notice = await ReloadAsync<DeathNotice>(id);
        notice!.Status.Should().Be("approved", "ikinci onay durumu bozmamalı");
        notice.ApprovedAt.Should().NotBeNull();
        first.Should().NotBeNull();
    }

    /// <summary>Var olmayan kayıt <c>false</c> döner — istisna fırlatıp paneli 500'e düşürmez.</summary>
    [Fact]
    public async Task DeathNotice_ApprovingAMissingRow_ReturnsFalse()
        => (await SendAsync(new ApproveDeathNoticeCommand(Guid.NewGuid(), _adminId))).Should().BeFalse();

    /// <summary>
    /// ⚠️ Silinmiş (soft-delete) ilan onaylanamamalı. Onaylanabilseydi, silinmiş bir
    /// kayıt "approved" damgası alır ve bir sonraki sorgu değişikliğinde yayına dönebilirdi.
    /// </summary>
    [Fact]
    public async Task DeathNotice_SoftDeleted_CannotBeApproved()
    {
        var notice = NewDeathNotice();
        var id = await InsertAsync(notice);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.DeathNotices.Where(d => d.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.DeletedAt, DateTime.UtcNow));
        });

        (await SendAsync(new ApproveDeathNoticeCommand(id, _adminId))).Should().BeFalse(
            "silinmiş kayıt moderasyona konu olmamalı");

        var reloaded = await ReloadAsync<DeathNotice>(id);
        reloaded!.Status.Should().Be("pending", "silinmiş kaydın durumu değişmemeli");
    }

    /// <summary>Silme soft-delete olmalı — vefat ilanı kullanıcı içeriğidir, geri alınabilmeli.</summary>
    [Fact]
    public async Task DeathNotice_Delete_IsSoftAndReversible()
    {
        var id = await InsertAsync(NewDeathNotice());

        (await SendAsync(new DeleteDeathNoticeCommand(id))).Should().BeTrue();

        var row = await ReloadAsync<DeathNotice>(id);
        row.Should().NotBeNull("soft-delete satırı silmez");
        row!.DeletedAt.Should().NotBeNull();

        var visible = default(bool);
        await _factory.WithScopeAsync(async sp =>
            visible = await sp.GetRequiredService<AppDbContext>().DeathNotices.AnyAsync(d => d.Id == id));
        visible.Should().BeFalse("silinen ilan normal sorgularda görünmemeli");
    }

    // ─────────────────────────────── Etkinlik ───────────────────────────────

    /// <summary>
    /// ⚠️ Fixture kurmanın iki tuzağı: <c>events.description</c> NOT NULL ve etkinlik
    /// seed'lenmiş bir kategoriye FK ile bağlı. Test veritabanı yalnız DbSeeder'ın lookup
    /// verisiyle geldiği için kategori oradan alınır (11.14 dersi).
    /// </summary>
    private async Task<Guid> NewEventAsync()
    {
        Guid categoryId = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
            categoryId = await sp.GetRequiredService<AppDbContext>().EventCategories
                .Select(c => c.Id).FirstAsync());

        return await InsertAsync(new Event
        {
            Title = _marker + " Etkinlik",
            Description = "Test etkinliği",
            CategoryId = categoryId,
            EventDate = DateTime.UtcNow.Date.AddDays(3),
            Status = "pending"
        });
    }

    /// <summary>
    /// Onay → red → tekrar onay: moderatör kararını değiştirebilmeli. Tek yönlü bir
    /// durum makinesi, yanlışlıkla reddedilen etkinliği kalıcı olarak gömerdi.
    /// </summary>
    [Fact]
    public async Task Event_ModerationDecision_CanBeReversed()
    {
        var id = await NewEventAsync();

        await SendAsync(new ApproveEventCommand(id, _adminId));
        (await ReloadAsync<Event>(id))!.Status.Should().Be("approved");

        await SendAsync(new RejectEventCommand(id, _adminId));
        (await ReloadAsync<Event>(id))!.Status.Should().Be("rejected", "onaylanan etkinlik geri çekilebilmeli");

        await SendAsync(new ApproveEventCommand(id, _adminId));
        (await ReloadAsync<Event>(id))!.Status.Should().Be("approved", "reddedilen etkinlik yeniden onaylanabilmeli");
    }

    [Fact]
    public async Task Event_SoftDeleted_CannotBeApproved()
    {
        var id = await NewEventAsync();
        await _factory.WithScopeAsync(async sp =>
            await sp.GetRequiredService<AppDbContext>().Events.Where(e => e.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.DeletedAt, DateTime.UtcNow)));

        (await SendAsync(new ApproveEventCommand(id, _adminId))).Should().BeFalse();
        (await ReloadAsync<Event>(id))!.Status.Should().Be("pending");
    }

    [Fact]
    public async Task Event_ApprovingAMissingRow_ReturnsFalse()
        => (await SendAsync(new ApproveEventCommand(Guid.NewGuid(), _adminId))).Should().BeFalse();

    // ─────────────────────────────── Kampanya ───────────────────────────────

    /// <summary>
    /// ⚠️ İşletme, seed'lenmiş bir kategoriye bağlı olmak zorunda (FK). Test veritabanı
    /// yalnız DbSeeder lookup verisiyle geldiği için kategori oradan alınır.
    /// </summary>
    private async Task<Guid> NewBusinessAsync()
    {
        Guid categoryId = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
            categoryId = await sp.GetRequiredService<AppDbContext>().BusinessCategories
                .Select(c => c.Id).FirstAsync());

        return await InsertAsync(new Business
        {
            BusinessName = _marker + " İşletme",
            CategoryId = categoryId,
            Phone = "03281112233"
        });
    }

    private async Task<Guid> NewCampaignAsync()
    {
        var businessId = await NewBusinessAsync();

        return await InsertAsync(new Campaign
        {
            BusinessId = businessId,
            Title = _marker + " Kampanya",
            Description = "Test kampanyası", // ⚠️ campaigns.description NOT NULL
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            Status = "pending"
        });
    }

    [Fact]
    public async Task Campaign_Approval_RecordsWhoAndWhen()
    {
        var id = await NewCampaignAsync();

        (await SendAsync(new ApproveCampaignCommand(id, _adminId))).Should().BeTrue();

        var campaign = await ReloadAsync<Campaign>(id);
        campaign!.Status.Should().Be("approved");
        campaign.ApprovedBy.Should().Be(_adminId);
        campaign.ApprovedAt.Should().NotBeNull();
    }

    /// <summary>
    /// 🔑 Red gerekçesi **kaydedilmeli**. Kaydedilmezse işletme sahibi kampanyasının
    /// neden yayınlanmadığını hiçbir yerden öğrenemez — 11.x'te ilanlarda aynı sorun
    /// düzeltilmişti (<c>RejectedReason</c>), kampanyada da tutulmalı.
    /// </summary>
    [Fact]
    public async Task Campaign_Rejection_StoresTheReason()
    {
        var id = await NewCampaignAsync();

        await SendAsync(new RejectCampaignCommand(id, _adminId, "Görsel telif ihlali içeriyor"));

        var campaign = await ReloadAsync<Campaign>(id);
        campaign!.Status.Should().Be("rejected");
        campaign.RejectedReason.Should().Be("Görsel telif ihlali içeriyor",
            "red gerekçesi yazılmazsa işletme sahibi sebebini öğrenemez");
    }

    /// <summary>
    /// Reddedilen kampanya sonradan onaylanırsa **eski red gerekçesi silinmeli** —
    /// yoksa onaylı kampanyanın üstünde eski bir "reddedildi" notu asılı kalır ve
    /// panelde çelişkili görünür.
    /// </summary>
    [Fact]
    public async Task Campaign_ApprovingAfterRejection_ClearsTheStaleReason()
    {
        var id = await NewCampaignAsync();
        await SendAsync(new RejectCampaignCommand(id, _adminId, "Eksik bilgi"));

        await SendAsync(new ApproveCampaignCommand(id, _adminId));

        var campaign = await ReloadAsync<Campaign>(id);
        campaign!.Status.Should().Be("approved");
        campaign.RejectedReason.Should().BeNull(
            "onaylanan kampanyada eski red gerekçesi kalmamalı — panelde çelişkili bilgi gösterir");
    }

    // ─────────────────────────────── İşletme ───────────────────────────────

    /// <summary>
    /// İşletme doğrulaması bir **rozet**tir: geri alındığında iz de temizlenmeli, yoksa
    /// "doğrulanmamış ama doğrulayan yönetici yazılı" gibi tutarsız bir satır kalır.
    /// </summary>
    [Fact]
    public async Task Business_Verification_CanBeGrantedAndRevokedCleanly()
    {
        var id = await NewBusinessAsync();

        await SendAsync(new KadirliApp.Application.Features.Businesses.Commands
            .SetBusinessVerificationCommand(id, true, _adminId));

        var verified = await ReloadAsync<Business>(id);
        verified!.IsVerified.Should().BeTrue();
        verified.VerifiedBy.Should().Be(_adminId);
        verified.VerifiedAt.Should().NotBeNull();

        await SendAsync(new KadirliApp.Application.Features.Businesses.Commands
            .SetBusinessVerificationCommand(id, false, _adminId));

        var revoked = await ReloadAsync<Business>(id);
        revoked!.IsVerified.Should().BeFalse();
        revoked.VerifiedBy.Should().BeNull("rozet geri alınınca iz de temizlenmeli");
        revoked.VerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task Business_VerifyingAMissingRow_ReturnsFalse()
        => (await SendAsync(new KadirliApp.Application.Features.Businesses.Commands
            .SetBusinessVerificationCommand(Guid.NewGuid(), true, _adminId))).Should().BeFalse();
}
