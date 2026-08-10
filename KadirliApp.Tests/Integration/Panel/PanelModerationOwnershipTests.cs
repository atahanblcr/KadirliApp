using FluentAssertions;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Ads.Commands.ApproveAd;
using KadirliApp.Application.Features.Ads.Commands.UpdateAd;
using KadirliApp.Application.Features.Deaths.Commands;
using KadirliApp.Application.Features.Deaths.Dtos;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.10 — görünmez sözleşme <b>#52</b>'nin <b>davranış</b> ayağı:
/// moderasyon durumunu yazmanın tek yolu Onayla/Reddet komutlarıdır.
/// </summary>
/// <remarks>
/// <para>
/// 🔬 <b>Kanıt (gerçek Postgres üzerinde koşturulmuştu):</b> süresi 3 gün önce dolmuş bir
/// ilan, panelin Düzenle formunun gönderdiği <c>UpdateAdCommand</c> ile <c>approved</c>
/// yapılabiliyordu. Sonuç: <c>Status=approved</c>, <c>ExpiresAt</c> geçmişte,
/// <c>ApprovedBy=NULL</c> — yani panel "güncellendi" diyor, <b>vatandaş hiçbir şey
/// görmüyor</b> ve <c>ExpireAdsJob</c> bir saat içinde durumu sessizce geri alıyordu.
/// Reddedilmiş bir ilan aynı yoldan onaylanınca panelde "Onaylandı" rozeti ile
/// "Reddedilme sebebi: …" satırı <b>yan yana</b> duruyordu.
/// </para>
/// <para>
/// 🔑 <b>Yapısal testten (<c>ModerationSingleOwnerTests</c>) ayrı olmak zorunda.</b>
/// Yapısal test "kimse <c>.Status =</c> yazmıyor" der; buradaki testler kuralın
/// <i>çalıştığını</i> ve reddetmenin kaydı <b>ezmediğini</b> kanıtlar. Kuralı doğru yazıp
/// handler'a bağlamayı unutmak mümkün — o durumda yalnız biri kırmızıya döner.
/// </para>
/// </remarks>
[Collection(PanelCollection.Name)]
public class PanelModerationOwnershipTests
{
    private readonly WebPanelApplicationFactory _factory;

    public PanelModerationOwnershipTests(WebPanelApplicationFactory factory) => _factory = factory;

    // ── İkinci yol kapandı ─────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Faz 12.10'un doğuş sebebi: Düzenle yolundan onaylanan, süresi dolmuş ilan.
    /// Artık komut <b>reddediyor</b> ve <c>#25</c>'in taze penceresi atlanamıyor.
    /// </summary>
    [Fact]
    public async Task UpdatingAnAd_CannotChangeItsModerationStatus()
    {
        var adId = await SeedAdAsync("12.10 ikinci yol testi", "expired", DateTime.UtcNow.AddDays(-3));

        var command = await BuildUpdateAsync(adId, "Yeni baslik", status: "approved");
        var act = () => SendAsync(command);

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be("VALIDATION_ERROR");

        await DeleteAdAsync(adId);
    }

    /// <summary>
    /// ⚠️ <b>Reddetme kaydı EZMEMELİ</b> (#46'nın kuralı). Guard, handler'ın ilk
    /// yazmasından ÖNCE çağrılmasaydı bu test yeşil kalırdı ama ilanın başlığı
    /// değişirdi: "reddettim" derken veri kaybı.
    /// </summary>
    [Fact]
    public async Task RefusedStatusChange_DoesNotOverwriteTheRecord()
    {
        var adId = await SeedAdAsync("Degismemesi gereken baslik", "pending", DateTime.UtcNow.AddDays(10));

        try
        {
            await SendAsync(await BuildUpdateAsync(adId, "EZILMIS BASLIK", status: "approved"));
        }
        catch (AppException) { /* beklenen */ }

        var ad = await LoadAdAsync(adId);
        ad.Title.Should().Be("Degismemesi gereken baslik",
            "reddedilen bir istek kaydın DİĞER alanlarını da değiştirmemeli");
        ad.Status.Should().Be("pending");

        await DeleteAdAsync(adId);
    }

    /// <summary>
    /// Ters yön: durum <b>aynı</b> gönderildiğinde düzenleme normal biçimde geçmeli.
    /// Bu iddia olmasaydı "her Update'i reddet" gibi bir gerçekleme de yeşil kalırdı.
    /// </summary>
    [Fact]
    public async Task UpdatingAnAd_StillWorksWhenTheStatusIsUnchanged()
    {
        var adId = await SeedAdAsync("Duzenlenecek ilan", "pending", DateTime.UtcNow.AddDays(10));

        await SendAsync(await BuildUpdateAsync(adId, "Duzenlenmis baslik", status: "pending"));

        (await LoadAdAsync(adId)).Title.Should().Be("Duzenlenmis baslik");

        await DeleteAdAsync(adId);
    }

    /// <summary>
    /// 🔑 <b>Panelin bugünkü davranışı:</b> form <c>Status</c> göndermiyor. Alan DTO'da
    /// duruyor (§5 — silmek kırıcı olurdu) ama boş gelmesi bir değişiklik talebi değil.
    /// Bu iddia düşerse <b>hiçbir düzenleme kaydedilemez</b>.
    /// </summary>
    [Fact]
    public async Task UpdatingAnAd_WithoutAStatusField_IsAccepted()
    {
        var adId = await SeedAdAsync("Statussuz gonderim", "approved", DateTime.UtcNow.AddDays(10));

        await SendAsync(await BuildUpdateAsync(adId, "Statussuz duzenleme", status: null));

        var ad = await LoadAdAsync(adId);
        ad.Title.Should().Be("Statussuz duzenleme");
        ad.Status.Should().Be("approved", "gönderilmeyen alan mevcut durumu değiştirmemeli");

        await DeleteAdAsync(adId);
    }

    // ── Kural taşındı, kaybolmadı ──────────────────────────────────────────────

    /// <summary>
    /// #25 hâlâ çalışıyor — kural <c>AdModeration</c>'a <b>taşındı</b>, silinmedi.
    /// (<c>PanelBusinessRuleTests</c> aynı şeyi bağımsız olarak da denetliyor; bu, taşımanın
    /// kuralı düşürmediğinin 12.10 tarafındaki kanıtı.)
    /// </summary>
    [Fact]
    public async Task ApproveCommand_StillGivesAnExpiredAdAFreshWindow()
    {
        var adId = await SeedAdAsync("Onay penceresi korunuyor mu", "expired", DateTime.UtcNow.AddDays(-3));

        await SendAsync(new ApproveAdCommand(adId, await AdminIdAsync()));

        var ad = await LoadAdAsync(adId);
        ad.Status.Should().Be("approved");
        ad.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        ad.ApprovedBy.Should().NotBeNull("Düzenle yolunun atladığı onay izi artık her zaman yazılıyor");

        await DeleteAdAsync(adId);
    }

    /// <summary>
    /// 12.10'un kanıt bölümündeki ikinci çelişki: reddedilmiş ilan Düzenle yolundan
    /// onaylanınca bayat gerekçe kalıyordu. Artık onay tek yoldan geçiyor ve temizliyor.
    /// </summary>
    [Fact]
    public async Task ApproveCommand_ClearsAStaleRejectionReason()
    {
        var adId = await SeedAdAsync("Bayat gerekce testi", "rejected", DateTime.UtcNow.AddDays(10),
            rejectedReason: "Uygunsuz gorsel.");

        await SendAsync(new ApproveAdCommand(adId, await AdminIdAsync()));

        var ad = await LoadAdAsync(adId);
        ad.RejectedReason.Should().BeNull(
            "'Onaylandı' rozetiyle 'Reddedilme sebebi: …' satırı yan yana duramaz");

        await DeleteAdAsync(adId);
    }

    // ── Vefat: 12.10'da AÇILAN iki yol ─────────────────────────────────────────

    /// <summary>
    /// Vefatta reddetmenin tek yolu Düzenle formuydu. Menü kaldırıldı; karşılığı
    /// yazılmasaydı "reddet" panelden <b>tamamen kaybolurdu</b>.
    /// </summary>
    [Fact]
    public async Task DeathNotice_CanBeRejectedWithAReasonThroughItsOwnCommand()
    {
        var noticeId = await SeedDeathNoticeAsync("Reddedilecek kayit", "pending");

        await SendAsync(new RejectDeathNoticeCommand(noticeId, await AdminIdAsync(), "Dogrulanamadi."));

        var notice = await LoadDeathNoticeAsync(noticeId);
        notice.Status.Should().Be("rejected");
        notice.RejectedReason.Should().Be("Dogrulanamadi.");

        await DeleteDeathNoticeAsync(noticeId);
    }

    /// <summary>
    /// Arşivleme de yalnız Düzenle formundan yapılabiliyordu. <c>AutoArchiveAt</c>'e
    /// dokunulmadığı burada da doğrulanıyor: kayıt tekrar onaylanırsa
    /// <c>ArchiveDeathsJob</c> yine doğru tarihte devreye girmeli.
    /// </summary>
    [Fact]
    public async Task DeathNotice_CanBeArchivedThroughItsOwnCommand()
    {
        var noticeId = await SeedDeathNoticeAsync("Arsivlenecek kayit", "approved");
        var before = (await LoadDeathNoticeAsync(noticeId)).AutoArchiveAt;

        await SendAsync(new ArchiveDeathNoticeCommand(noticeId, await AdminIdAsync()));

        var notice = await LoadDeathNoticeAsync(noticeId);
        notice.Status.Should().Be("archived");
        notice.AutoArchiveAt.Should().Be(before);

        await DeleteDeathNoticeAsync(noticeId);
    }

    /// <summary>Vefatın Düzenle yolu da kapalı — dört modülün dördü de aynı kuralda.</summary>
    [Fact]
    public async Task UpdatingADeathNotice_CannotChangeItsModerationStatus()
    {
        var noticeId = await SeedDeathNoticeAsync("Ikinci yol kapali mi", "pending");

        var act = () => SendAsync(new UpdateDeathNoticeCommand(noticeId, new UpdateDeathNoticeDto(
            DeceasedName: "Ikinci yol kapali mi",
            PhotoFileId: null,
            FuneralDate: DateTime.UtcNow.Date,
            FuneralTime: new TimeSpan(14, 0, 0),
            CemeteryId: null,
            MosqueId: null,
            NeighborhoodId: null,
            CondolenceAddress: null,
            CondolenceLatitude: null,
            CondolenceLongitude: null,
            Status: "approved")));

        await act.Should().ThrowAsync<AppException>();

        (await LoadDeathNoticeAsync(noticeId)).Status.Should().Be("pending");

        await DeleteDeathNoticeAsync(noticeId);
    }

    // ── Yardımcılar ────────────────────────────────────────────────────────────

    /// <summary>
    /// Panelin Düzenle formunun gönderdiğinin aynısı.
    /// ⚠️ <c>CategoryId</c> kaydın kendisinden okunur: boş bırakılırsa <c>Guid.Empty</c>
    /// FK'yi ihlal eder ve test, denetlediği kuraldan bağımsız bir sebeple kırmızıya döner.
    /// </summary>
    private async Task<UpdateAdCommand> BuildUpdateAsync(Guid adId, string title, string? status) => new()
    {
        Id = adId,
        CategoryId = (await LoadAdAsync(adId)).CategoryId,
        Title = title,
        Description = "12.10 testi.",
        Price = 100m,
        ContactPhone = "+905550000000",
        Status = status
    };

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp =>
            result = await sp.GetRequiredService<ISender>().Send(request));
        return result;
    }

    private async Task<Guid> AdminIdAsync()
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            id = (await db.Users.FirstAsync(u => u.Role == UserRole.SuperAdmin)).Id;
        });
        return id;
    }

    private async Task<Guid> SeedAdAsync(string title, string status, DateTime expiresAt, string? rejectedReason = null)
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var ad = new Ad
            {
                UserId = (await db.Users.FirstAsync()).Id,
                CategoryId = (await db.AdCategories.FirstAsync()).Id,
                Title = title,
                Description = "12.10 testi.",
                Price = 100m,
                ContactPhone = "+905550000000",
                Status = status,
                ExpiresAt = expiresAt,
                RejectedReason = rejectedReason,
                CreatedAt = DateTime.UtcNow
            };
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            id = ad.Id;
        });
        return id;
    }

    private async Task<Ad> LoadAdAsync(Guid id)
    {
        Ad ad = null!;
        await _factory.WithScopeAsync(async sp =>
            ad = await sp.GetRequiredService<AppDbContext>().Ads.AsNoTracking().FirstAsync(a => a.Id == id));
        return ad;
    }

    private Task DeleteAdAsync(Guid id) => _factory.WithScopeAsync(async sp =>
        await sp.GetRequiredService<AppDbContext>().Ads.Where(a => a.Id == id).ExecuteDeleteAsync());

    private async Task<Guid> SeedDeathNoticeAsync(string name, string status)
    {
        Guid id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var notice = new DeathNotice
            {
                DeceasedName = name,
                FuneralDate = DateTime.UtcNow.Date,
                FuneralTime = new TimeSpan(14, 0, 0),
                AddedBy = (await db.Users.FirstAsync()).Id,
                Status = status,
                AutoArchiveAt = DateTime.UtcNow.Date.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
            db.DeathNotices.Add(notice);
            await db.SaveChangesAsync();
            id = notice.Id;
        });
        return id;
    }

    private async Task<DeathNotice> LoadDeathNoticeAsync(Guid id)
    {
        DeathNotice notice = null!;
        await _factory.WithScopeAsync(async sp =>
            notice = await sp.GetRequiredService<AppDbContext>().DeathNotices.AsNoTracking().FirstAsync(d => d.Id == id));
        return notice;
    }

    private Task DeleteDeathNoticeAsync(Guid id) => _factory.WithScopeAsync(async sp =>
        await sp.GetRequiredService<AppDbContext>().DeathNotices.Where(d => d.Id == id).ExecuteDeleteAsync());
}
