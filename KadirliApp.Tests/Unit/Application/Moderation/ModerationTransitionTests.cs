using FluentAssertions;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Moderation;

/// <summary>
/// Faz 12.10/12.11 — dört moderasyonlu varlığın <b>geçiş metotlarının</b> saf testleri
/// (<c>AdSubmissionRules</c> / <c>OperatingDays</c> deseni: container yok, milisaniyeler).
/// </summary>
/// <remarks>
/// <para>
/// Bu testler kuralın <i>kendisini</i> kilitliyor; <c>PanelModerationOwnershipTests</c>
/// ise komutların gerçekten buraya delege ettiğini ve ikinci yolun kapandığını
/// gerçek Postgres üzerinde kanıtlıyor. İkisi ayrı olmak zorunda: kuralı doğru yazıp
/// handler'a <b>bağlamayı unutmak</b> mümkün ve o durumda yalnız bu dosya yeşil kalır.
/// </para>
/// <para>
/// 📌 <b>12.11'de dosya taşınmadı, çağrılar değişti.</b> Kurallar
/// <c>Application/Features/…/…Moderation.cs</c> saf sınıflarından <b>varlığın kendisine</b>
/// taşındı: alanlar <c>init</c> olduğu için tek sahiplik artık bir dosya taramasına değil
/// <b>derleyiciye</b> dayanıyor. Testin iddiaları birebir aynı kaldı — taşımanın davranışı
/// değiştirmediğinin kanıtı da bu.
/// </para>
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

        ad.Approve(Admin, Now);

        ad.Status.Should().Be("approved");
        ad.ExpiresAt.Should().Be(Now.AddDays(Ad.PublishDays));
    }

    /// <summary>
    /// Koşul <b>duruma değil TARİHE</b> bakar: onay kuyruğunda 30 günden fazla bekleyen
    /// bir <c>pending</c> ilan da onaylandığı anda süresi dolmuş olurdu.
    /// </summary>
    [Fact]
    public void AdApprove_LooksAtTheDateNotTheStatus()
    {
        var ad = new Ad { Status = "pending", ExpiresAt = Now.AddMinutes(-1) };

        ad.Approve(Admin, Now);

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

        ad.Approve(Admin, Now);

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

        ad.Approve(Admin, Now);

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

        ad.Reject("Iletisim bilgisi eksik", Now);

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

        ad.Reject(null, Now);

        ad.ExpiresAt.Should().Be(expires);
    }

    // ── İlan: uzatma (12.11'de bulunan BEŞİNCİ yazma yolu) ─────────────────────

    /// <summary>
    /// 🔴 <b>12.11'in bulgusu.</b> Bu geçiş <c>ExtendMyAdCommandHandler</c>'ın içinde ham
    /// <c>ad.Status = "approved"</c> olarak yazılıydı ve 12.10'un yapısal testi onu
    /// <b>hiç görmüyordu</b> (test yalnız <c>Update*</c>/<c>Approve*</c>/<c>Reject*</c>/
    /// <c>Archive*</c> dosyalarını tarıyor). Yani "moderasyon durumunun tek sahibi var"
    /// güvencesi bu yolda <i>tesadüfen</i> doğruydu, kurala dayanarak değil.
    /// </summary>
    [Fact]
    public void AdExtend_BringsAnExpiredAdBackToApproved()
    {
        var ad = new Ad { Status = "expired", ExpiresAt = Now.AddDays(-3) };

        ad.Extend(30, Now);

        ad.Status.Should().Be("approved");
        ad.ExpiresAt.Should().Be(Now.AddDays(30));
        ad.ExtensionCount.Should().Be(1);
    }

    /// <summary>
    /// Ters yön: uzatma <b>moderasyon kararı değildir</b>. Onay izi yazmak "bu ilanı falanca
    /// yönetici onayladı" diye yalan söylerdi; izi silmek ise gerçek onay bilgisini
    /// kaybettirirdi. İlan <c>expired</c>'a yalnız <c>approved</c> iken düşebildiği için
    /// iz zaten dolu gelir ve <b>öyle kalmalı</b>.
    /// </summary>
    [Fact]
    public void AdExtend_DoesNotForgeAnApprovalTrail()
    {
        var approvedAt = Now.AddDays(-40);
        var ad = new Ad
        {
            Status = "expired",
            ExpiresAt = Now.AddDays(-3),
            ApprovedBy = Admin,
            ApprovedAt = approvedAt
        };

        ad.Extend(30, Now);

        ad.ApprovedBy.Should().Be(Admin);
        ad.ApprovedAt.Should().Be(approvedAt);
    }

    /// <summary>
    /// Süresi geçmemiş ilan <b>mevcut bitişten</b> uzar — erken uzatan gün kaybetmez.
    /// Bu iddia olmasaydı "her uzatmada now+30" gibi bir gerçekleme de yeşil kalırdı.
    /// </summary>
    [Fact]
    public void AdExtend_AddsToTheRemainingWindowNotToToday()
    {
        var ad = new Ad { Status = "approved", ExpiresAt = Now.AddDays(10) };

        ad.Extend(30, Now);

        ad.ExpiresAt.Should().Be(Now.AddDays(40));
        ad.Status.Should().Be("approved");
    }

    // ── Kampanya ───────────────────────────────────────────────────────────────

    [Fact]
    public void CampaignApprove_ClearsAStaleRejectionAndRecordsWhoApproved()
    {
        var campaign = new Campaign { Status = "rejected", RejectedReason = "Kosullar belirsiz." };

        campaign.Approve(Admin, Now);

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

        campaign.Reject("Kosullar belirsiz.");

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

        notice.Approve(Admin, Now);

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

        notice.Reject("Dogrulanamadi.");

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

        notice.Archive();

        notice.Status.Should().Be("archived");
        notice.AutoArchiveAt.Should().Be(autoArchive);
    }

    /// <summary>Arşivden çıkarmanın yolu <c>Approve</c>'dur — ikinci bir geçiş sahibi yok.</summary>
    [Fact]
    public void DeathNoticeApprove_BringsAnArchivedNoticeBack()
    {
        var notice = new DeathNotice { Status = "archived" };

        notice.Approve(Admin, Now);

        notice.Status.Should().Be("approved");
    }

    // ── Etkinlik ───────────────────────────────────────────────────────────────

    [Fact]
    public void EventTransitions_SetOnlyTheStatus()
    {
        var ev = new Event { Status = "pending" };

        ev.Approve();
        ev.Status.Should().Be("approved");

        ev.Reject();
        ev.Status.Should().Be("rejected");
    }
}
