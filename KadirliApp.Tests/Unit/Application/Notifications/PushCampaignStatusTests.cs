using FluentAssertions;
using KadirliApp.Application.Features.PushCampaigns;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Notifications;

/// <summary>
/// Faz 12.2b — kampanya durumunun **saf** kuralları (container gerekmez).
///
/// 🔑 Durum ayrı bir kolonda tutulmuyor: tutulsaydı sayaçlarla ayrışabilirdi (job sayaçları
/// artırıp durumu yazmayı unutsa pano "Kuyrukta" derken tablo 500 gönderim gösterirdi —
/// görünmez sözleşme #23'ün aynı sınıfı). Türetilmiş olduğu için bu ayrışma imkânsız;
/// karşılığında türetme kuralının kendisi kilitlenmeli.
/// </summary>
public class PushCampaignStatusTests
{
    private static readonly DateTime Now = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FreshCampaignWithRecipients_IsQueued()
    {
        PushCampaignStatus.Of(recipientCount: 100, sentCount: 0, failedCount: 0, completedAt: null, cancelledAt: null)
            .Should().Be(PushCampaignStatuses.Queued);
    }

    [Fact]
    public void PartiallyProcessed_IsSending()
    {
        PushCampaignStatus.Of(100, sentCount: 40, failedCount: 2, completedAt: null, cancelledAt: null)
            .Should().Be(PushCampaignStatuses.Sending);
    }

    [Fact]
    public void CompletedAt_MakesItCompleted()
    {
        PushCampaignStatus.Of(100, 98, 2, completedAt: Now, cancelledAt: null)
            .Should().Be(PushCampaignStatuses.Completed);
    }

    /// <summary>
    /// "Alıcı yok" bir hata değil bir <b>cevap</b>: hedeflemeye uyan kimse çıkmamış.
    /// Kuyrukta gösterilseydi yönetici hiç gitmeyecek bir bildirimi beklerdi.
    /// </summary>
    [Fact]
    public void NoRecipients_IsEmpty_NotQueued()
    {
        PushCampaignStatus.Of(recipientCount: 0, 0, 0, completedAt: Now, cancelledAt: null)
            .Should().Be(PushCampaignStatuses.Empty);
    }

    /// <summary>
    /// 🔴 <b>Öncelik sırası.</b> İptal edilmiş bir kampanya "tamamlandı" diye okunmamalı —
    /// ikisi de <c>CompletedAt</c> taşır (iptal kampanyayı da kapatır), ayrım yalnız
    /// <c>CancelledAt</c>'te. Sıra bozulursa pano geri çekilmiş bir gönderimi başarıyla
    /// tamamlanmış gösterir ve <b>hiç hata vermez</b>.
    /// </summary>
    [Fact]
    public void Cancelled_OutranksEverythingElse()
    {
        PushCampaignStatus.Of(100, 40, 0, completedAt: Now, cancelledAt: Now)
            .Should().Be(PushCampaignStatuses.Cancelled);

        // Alıcısı olmayan bir kampanya iptal edilemez ama sıra yine de doğru olmalı:
        // kural "iptal > boş", tersi değil.
        PushCampaignStatus.Of(0, 0, 0, completedAt: Now, cancelledAt: Now)
            .Should().Be(PushCampaignStatuses.Cancelled);
    }

    [Fact]
    public void EveryProducedStatus_IsInTheKnownSet()
    {
        var produced = new[]
        {
            PushCampaignStatus.Of(0, 0, 0, Now, null),
            PushCampaignStatus.Of(100, 0, 0, null, null),
            PushCampaignStatus.Of(100, 5, 0, null, null),
            PushCampaignStatus.Of(100, 100, 0, Now, null),
            PushCampaignStatus.Of(100, 5, 0, Now, Now)
        };

        produced.Should().OnlyContain(s => PushCampaignStatuses.All.Contains(s));
        produced.Distinct().Should().HaveCount(5, "beş durumun beşi de üretilebilmeli");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Bekleyen sayısı
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Pending_IsWhatIsLeft()
    {
        PushCampaignStatus.Pending(recipientCount: 100, sentCount: 60, failedCount: 5).Should().Be(35);
    }

    /// <summary>
    /// ⚠️ Sayaçlar <b>artımlı</b> yazılıyor: job iki kez sayarsa ya da bir kampanyaya
    /// sonradan satır eklenirse işlenen sayısı alıcıyı geçebilir. Panel "-3 bekliyor"
    /// yazmamalı — sıfırla sınırlanır.
    /// </summary>
    [Fact]
    public void Pending_NeverGoesNegative()
    {
        PushCampaignStatus.Pending(recipientCount: 10, sentCount: 8, failedCount: 5).Should().Be(0);
    }
}
