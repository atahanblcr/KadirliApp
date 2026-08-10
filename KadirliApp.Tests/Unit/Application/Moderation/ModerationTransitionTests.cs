using FluentAssertions;
using KadirliApp.Application.Features.Ads;
using KadirliApp.Application.Features.Campaigns;
using KadirliApp.Application.Features.Deaths;
using KadirliApp.Application.Features.Events;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Moderation;

/// <summary>
/// Faz 12.10 — dört moderasyonlu modülün <b>geçiş sınıflarının</b> saf testleri
/// (<c>AdSubmissionRules</c> / <c>OperatingDays</c> deseni: container yok, milisaniyeler).
/// </summary>
/// <remarks>
/// Bu testler kuralın <i>kendisini</i> kilitliyor; <c>PanelModerationOwnershipTests</c>
/// ise komutların gerçekten buraya delege ettiğini ve ikinci yolun kapandığını
/// gerçek Postgres üzerinde kanıtlıyor. İkisi ayrı olmak zorunda: kuralı doğru yazıp
/// handler'a <b>bağlamayı unutmak</b> mümkün ve o durumda yalnız bu dosya yeşil kalır.
/// </remarks>
public class ModerationTransitionTests
{
    private static readonly Guid Admin = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    // ── İlan ───────────────────────────────────────────────────────────────────

    /// <summary>Görünmez sözleşme #25: onay ilanı GERÇEKTEN görünür kılar.</summary>
    [Fact]
    public void AdApprove_GivesAnExpiredAdAFreshWindow()
    {
        var ad = new Ad { Status = "expired", ExpiresAt = Now.AddDays(-3) };

        AdModeration.Approve(ad, Admin, Now);

        ad.Status.Should().Be("approved");
        ad.ExpiresAt.Should().Be(Now.AddDays(AdModeration.PublishDays));
    }

    /// <summary>
    /// Koşul <b>duruma değil TARİHE</b> bakar: onay kuyruğunda 30 günden fazla bekleyen
    /// bir <c>pending</c> ilan da onaylandığı anda süresi dolmuş olurdu.
    /// </summary>
    [Fact]
    public void AdApprove_LooksAtTheDateNotTheStatus()
    {
        var ad = new Ad { Status = "pending", ExpiresAt = Now.AddMinutes(-1) };

        AdModeration.Approve(ad, Admin, Now);

        ad.ExpiresAt.Should().BeAfter(Now);
    }

    /// <summary>
    /// Ters yön — onay bir <b>uzatma aracı değil</b>. Bu iddia olmasaydı "her onayda
    /// +30 gün" gibi bir gerçekleme de yeşil kalırdı.
    /// </summary>
    [Fact]
    public void AdApprove_DoesNotExtendAnAdThatIsStillLive()
    {
        var expires = Now.AddDays(10);
        var ad = new Ad { Status = "pending", ExpiresAt = expires };

        AdModeration.Approve(ad, Admin, Now);

        ad.ExpiresAt.Should().Be(expires);
    }

    /// <summary>10.14(1): "Onaylandı" rozetiyle bayat red gerekçesi yan yana durmamalı.</summary>
    [Fact]
    public void AdApprove_ClearsAStaleRejectionAndRecordsWhoApproved()
    {
        var ad = new Ad
        {
            Status = "rejected",
            ExpiresAt = Now.AddDays(5),
            RejectedReason = "Uygunsuz gorsel.",
            RejectedAt = Now.AddDays(-1)
        };

        AdModeration.Approve(ad, Admin, Now);

        ad.RejectedReason.Should().BeNull();
        ad.RejectedAt.Should().BeNull();
        ad.ApprovedBy.Should().Be(Admin);
        ad.ApprovedAt.Should().Be(Now);
    }

    /// <summary>Bir kayıt aynı anda hem onaylı hem reddedilmiş olamaz.</summary>
    [Fact]
    public void AdReject_ClearsTheApprovalTrail()
    {
        var ad = new Ad
        {
            Status = "approved",
            ExpiresAt = Now.AddDays(5),
            ApprovedBy = Admin,
            ApprovedAt = Now.AddDays(-2)
        };

        AdModeration.Reject(ad, "Iletisim bilgisi eksik", Now);

        ad.Status.Should().Be("rejected");
        ad.RejectedReason.Should().Be("Iletisim bilgisi eksik");
        ad.RejectedAt.Should().Be(Now);
        ad.ApprovedBy.Should().BeNull();
        ad.ApprovedAt.Should().BeNull();
    }

    /// <summary>Red, ilanın süresine <b>dokunmaz</b> — reddetmek bir yayın kararı değil.</summary>
    [Fact]
    public void AdReject_DoesNotTouchTheExpiryWindow()
    {
        var expires = Now.AddDays(4);
        var ad = new Ad { Status = "pending", ExpiresAt = expires };

        AdModeration.Reject(ad, null, Now);

        ad.ExpiresAt.Should().Be(expires);
    }

    // ── Kampanya ───────────────────────────────────────────────────────────────

    [Fact]
    public void CampaignApprove_ClearsAStaleRejectionAndRecordsWhoApproved()
    {
        var campaign = new Campaign { Status = "rejected", RejectedReason = "Kosullar belirsiz." };

        CampaignModeration.Approve(campaign, Admin, Now);

        campaign.Status.Should().Be("approved");
        campaign.RejectedReason.Should().BeNull();
        campaign.ApprovedBy.Should().Be(Admin);
        campaign.ApprovedAt.Should().Be(Now);
    }

    /// <summary>
    /// 🐛 <b>12.10'da düzeltilen simetri hatası.</b> Red, onay izlerini temizlemiyordu:
    /// reddedilmiş bir kampanyanın kaydında hâlâ "onaylayan yönetici" duruyordu —
    /// denetim izi doğru, <b>kaydın kendisi yalan</b>. İlanlarda 10.14(1)'de çözülmüş,
    /// kampanyaya taşınmamıştı.
    /// </summary>
    [Fact]
    public void CampaignReject_ClearsTheApprovalTrail()
    {
        var campaign = new Campaign { Status = "approved", ApprovedBy = Admin, ApprovedAt = Now.AddDays(-1) };

        CampaignModeration.Reject(campaign, "Kosullar belirsiz.", Now);

        campaign.Status.Should().Be("rejected");
        campaign.RejectedReason.Should().Be("Kosullar belirsiz.");
        campaign.ApprovedBy.Should().BeNull();
        campaign.ApprovedAt.Should().BeNull();
    }

    // ── Vefat ──────────────────────────────────────────────────────────────────

    [Fact]
    public void DeathNoticeApprove_RecordsWhoApprovedAndClearsAStaleRejection()
    {
        var notice = new DeathNotice { Status = "rejected", RejectedReason = "Dogrulanamadi." };

        DeathNoticeModeration.Approve(notice, Admin, Now);

        notice.Status.Should().Be("approved");
        notice.ApprovedBy.Should().Be(Admin);
        notice.ApprovedAt.Should().Be(Now);
        notice.RejectedReason.Should().BeNull();
    }

    /// <summary>12.10'da doğdu: bu geçişin daha önce hiçbir komutu yoktu.</summary>
    [Fact]
    public void DeathNoticeReject_ClearsTheApprovalTrail()
    {
        var notice = new DeathNotice { Status = "approved", ApprovedBy = Admin, ApprovedAt = Now.AddDays(-1) };

        DeathNoticeModeration.Reject(notice, "Dogrulanamadi.", Now);

        notice.Status.Should().Be("rejected");
        notice.RejectedReason.Should().Be("Dogrulanamadi.");
        notice.ApprovedBy.Should().BeNull();
        notice.ApprovedAt.Should().BeNull();
    }

    /// <summary>
    /// ⚠️ <c>AutoArchiveAt</c>'e dokunulmaz: "ne zaman kendiliğinden arşivlenecekti"
    /// bilgisidir ve elle arşivleme onu geçersiz kılmaz — kayıt sonradan tekrar
    /// onaylanırsa <c>ArchiveDeathsJob</c> yine doğru tarihte devreye girmeli.
    /// </summary>
    [Fact]
    public void DeathNoticeArchive_KeepsTheAutoArchiveSchedule()
    {
        var autoArchive = Now.AddDays(7);
        var notice = new DeathNotice { Status = "approved", AutoArchiveAt = autoArchive };

        DeathNoticeModeration.Archive(notice);

        notice.Status.Should().Be("archived");
        notice.AutoArchiveAt.Should().Be(autoArchive);
    }

    /// <summary>Arşivden çıkarmanın yolu <c>Approve</c>'dur — ikinci bir geçiş sahibi yok.</summary>
    [Fact]
    public void DeathNoticeApprove_BringsAnArchivedNoticeBack()
    {
        var notice = new DeathNotice { Status = "archived" };

        DeathNoticeModeration.Approve(notice, Admin, Now);

        notice.Status.Should().Be("approved");
    }

    // ── Etkinlik ───────────────────────────────────────────────────────────────

    [Fact]
    public void EventTransitions_SetOnlyTheStatus()
    {
        var ev = new Event { Status = "pending" };

        EventModeration.Approve(ev);
        ev.Status.Should().Be("approved");

        EventModeration.Reject(ev);
        ev.Status.Should().Be("rejected");
    }
}
