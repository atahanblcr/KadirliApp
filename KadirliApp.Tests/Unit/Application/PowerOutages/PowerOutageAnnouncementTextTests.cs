using FluentAssertions;
using KadirliApp.Application.Features.PowerOutages;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.PowerOutages;

/// <summary>
/// Faz 12.3 — kesinti bildiriminin <b>metni</b>. Vatandaşın kilit ekranında okuyacağı şey.
///
/// 🔴 Asıl risk saat: kayıtlar UTC tutuluyor, gövde ham UTC yazsaydı 20:00'de başlayan
/// kesinti bildirimde <b>17:00</b> görünürdü — ve kimse hata almazdı (görünmez sözleşme
/// #6'nın aynı sınıfı, bu projede dört kez tekrarlamış bir hata).
/// </summary>
public class PowerOutageAnnouncementTextTests
{
    private static PowerOutage Outage(
        DateTime startUtc, DateTime endUtc,
        string? neighborhood = "Cengiz Topel", string? area = null, string? reason = null)
        => new()
        {
            Neighborhood = neighborhood,
            AreaDetail = area,
            Reason = reason,
            StartTime = startUtc,
            EndTime = endUtc
        };

    [Fact]
    public void Range_ShiftsUtcToTurkeyLocalTime()
    {
        var start = new DateTime(2026, 8, 7, 6, 0, 0, DateTimeKind.Utc);   // TR 09:00
        var end = new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc);    // TR 13:00

        PowerOutageAnnouncementText.Range(start, end)
            .Should().Be("7 Ağustos 09:00 – 13:00");
    }

    /// <summary>
    /// Gün aşan kesintide bitiş TARİHİ de yazılmalı: yalnız saat yazılsaydı
    /// "22:00 – 06:00" okuyan vatandaş kesintinin 8 saat değil eksi 16 saat sürdüğünü sanırdı.
    /// </summary>
    [Fact]
    public void Range_WritesTheEndDateWhenTheOutageCrossesMidnight()
    {
        var start = new DateTime(2026, 8, 7, 19, 0, 0, DateTimeKind.Utc);  // TR 7 Ağu 22:00
        var end = new DateTime(2026, 8, 8, 3, 0, 0, DateTimeKind.Utc);     // TR 8 Ağu 06:00

        PowerOutageAnnouncementText.Range(start, end)
            .Should().Be("7 Ağustos 22:00 – 8 Ağustos 06:00");
    }

    /// <summary>Yerel saate kaydırma günü de kaydırabilir — UTC 23:00 Türkiye'de ertesi gün 02:00'dir.</summary>
    [Fact]
    public void Range_RollsTheDayForwardWhenLocalTimePassesMidnight()
    {
        var start = new DateTime(2026, 8, 7, 22, 0, 0, DateTimeKind.Utc);  // TR 8 Ağu 01:00
        var end = new DateTime(2026, 8, 7, 23, 30, 0, DateTimeKind.Utc);   // TR 8 Ağu 02:30

        PowerOutageAnnouncementText.Range(start, end)
            .Should().Be("8 Ağustos 01:00 – 02:30");
    }

    [Fact]
    public void Title_NamesTheNeighbourhood()
        => PowerOutageAnnouncementText.Title(Outage(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)))
            .Should().Be("Elektrik kesintisi: Cengiz Topel");

    [Fact]
    public void Title_FallsBackToCityWide()
        => PowerOutageAnnouncementText.Title(
                Outage(DateTime.UtcNow, DateTime.UtcNow.AddHours(1), neighborhood: null))
            .Should().Be("Elektrik kesintisi: Kadirli geneli");

    [Fact]
    public void Body_AppendsAreaDetailAndReason()
    {
        var body = PowerOutageAnnouncementText.Body(Outage(
            new DateTime(2026, 8, 7, 6, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc),
            area: "Atatürk Caddesi ve çevresi",
            reason: "Trafo bakımı"));

        body.Should().Be("7 Ağustos 09:00 – 13:00 · Atatürk Caddesi ve çevresi · Trafo bakımı");
    }

    [Fact]
    public void Body_OmitsEmptyParts()
    {
        var body = PowerOutageAnnouncementText.Body(Outage(
            new DateTime(2026, 8, 7, 6, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc),
            area: "   ",
            reason: null));

        body.Should().Be("7 Ağustos 09:00 – 13:00", "boş alanlar ayraçla birlikte tamamen düşmeli");
    }

    /// <summary>
    /// Metin push gövdesine gidiyor: <c>NotificationDispatcher</c> 500 karakterde kırpıyor,
    /// yani bu metnin makul kalması gerekiyor. Uzun bir sebep bile tavanı zorlamamalı.
    /// </summary>
    [Fact]
    public void Body_StaysWellUnderThePushBodyLimit()
    {
        var body = PowerOutageAnnouncementText.Body(Outage(
            new DateTime(2026, 8, 7, 6, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc),
            area: new string('A', 120),
            reason: new string('B', 200)));

        body.Length.Should().BeLessThan(
            KadirliApp.Application.Features.Notifications.Services.NotificationDispatcher.MaxBodyLength);
    }
}
