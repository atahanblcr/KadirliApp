using FluentAssertions;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Moderation;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Moderation;

/// <summary>
/// Faz 12.10 — görünmez sözleşme <b>#52</b>'nin saf, container'sız ayağı:
/// <c>Update*</c> komutlarının açtığı ikinci moderasyon yolunun kapısı.
/// </summary>
public class ModerationStatusGuardTests
{
    [Theory]
    [InlineData("pending", "pending")]
    [InlineData("approved", "approved")]
    [InlineData("rejected", "rejected")]
    public void SameStatus_IsAllowed(string current, string requested)
    {
        var act = () => ModerationStatusGuard.EnsureUnchanged(current, requested);

        act.Should().NotThrow(
            "düzenleme formu kaydın MEVCUT durumunu geri gönderiyorsa bir değişiklik talebi yoktur");
    }

    /// <summary>
    /// 🔑 Asıl kural. Bu iddia düşerse Düzenle formu yeniden bir moderasyon aracına döner:
    /// süresi dolmuş ilan "onaylı" görünür ama mobilde çıkmaz, bayat red gerekçesi
    /// "Onaylandı" rozetinin yanında durur ve karar denetim izine <b>hiç düşmez</b>.
    /// </summary>
    [Theory]
    [InlineData("pending", "approved")]
    [InlineData("pending", "rejected")]
    [InlineData("rejected", "approved")]
    [InlineData("approved", "expired")]
    [InlineData("approved", "archived")]
    public void ChangedStatus_IsRefused(string current, string requested)
    {
        var act = () => ModerationStatusGuard.EnsureUnchanged(current, requested);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == "VALIDATION_ERROR");
    }

    /// <summary>
    /// Sebep <b>Türkçe</b> ve <b>ne yapılacağını</b> söylüyor (Değişmez Kural #6).
    /// "Geçersiz durum" demek yöneticiyi ekranda çaresiz bırakırdı.
    /// </summary>
    [Fact]
    public void RefusalMessage_TellsTheAdminWhatToDoInstead()
    {
        var act = () => ModerationStatusGuard.EnsureUnchanged("pending", "approved");

        act.Should().Throw<AppException>()
            .WithMessage("*Onayla*")
            .And.Message.Should().Contain("Reddet");
    }

    /// <summary>
    /// ⚠️ Boş/eksik değer <b>sessizce kabul edilir</b> ve bu bilinçli: alan DTO'da duruyor
    /// (§5 — silmek kırıcı olurdu) ama form onu artık göndermiyor. "Boş = hiçbir şey isteme"
    /// saymak, additive bir alanın <i>yokluğunun</i> eski davranışı vermesi kuralıdır (#49).
    /// Reddedilseydi <b>hiçbir düzenleme kaydedilemezdi</b>.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingStatus_IsTreatedAsNoRequest(string? requested)
    {
        var act = () => ModerationStatusGuard.EnsureUnchanged("approved", requested);

        act.Should().NotThrow();
    }

    /// <summary>
    /// Harf/boşluk farkı bir <i>değişiklik talebi</i> değildir; reddetmek yalnız gürültü
    /// üretirdi (admin API'sinin eski istemcileri <c>"Approved"</c> gönderebilir).
    /// </summary>
    [Theory]
    [InlineData("approved", "Approved")]
    [InlineData("approved", " approved ")]
    [InlineData("PENDING", "pending")]
    public void CaseAndWhitespaceDifferences_AreNotAChange(string current, string requested)
    {
        var act = () => ModerationStatusGuard.EnsureUnchanged(current, requested);

        act.Should().NotThrow();
    }

    /// <summary>
    /// Kaydın durumu bir şekilde boşsa gelen değer yine de reddedilmeli — "boş kayda ne
    /// yazsan serbest" bir arka kapı olurdu.
    /// </summary>
    [Fact]
    public void EmptyCurrentStatus_StillRefusesAnIncomingValue()
    {
        var act = () => ModerationStatusGuard.EnsureUnchanged(null, "approved");

        act.Should().Throw<AppException>();
    }
}
