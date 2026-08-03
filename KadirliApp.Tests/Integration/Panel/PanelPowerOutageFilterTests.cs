extern alias WebPanel;

using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using OutagePhase = WebPanel::KadirliApp.Web.Common.OutagePhase;
using PowerOutagePhaseRules = WebPanel::KadirliApp.Web.Common.PowerOutagePhaseRules;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.17 — **kesinti ekranında arama/filtre.**
///
/// 🔑 Asıl risk süzgecin kendisi değil, <b>zaman tanımı</b>: <c>GET /v1/power-outages</c>
/// bilinçli olarak sayfalamıyor ve tarih süzmüyor (görünmez sözleşme #1); süren/planlı
/// ayrımını mobil istemci yapıyor. Panel kendi tanımını yazarsa yönetici "sürüyor" derken
/// vatandaş "planlı" görür ve <b>kimse hata almaz</b> — 11.15c'de düzeltilen ayrışmanın aynısı.
///
/// Bu yüzden sınır anları (başlangıç dâhil, bitiş hariç) burada birebir kilitli.
/// Karşılığı: <c>mobile/lib/features/power_outages/data/models/power_outage.dart</c>
/// (<c>isActive</c>/<c>isUpcoming</c>/<c>isPast</c>).
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelPowerOutageFilterTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "OutageTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelPowerOutageFilterTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.PowerOutages.IgnoreQueryFilters()
                .Where(o => o.Neighborhood != null && o.Neighborhood.Contains(_marker))
                .ExecuteDeleteAsync();
        });
    }

    // ─────────────────── zaman tanımı: mobil ile birebir ───────────────────

    private static readonly DateTime Now = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void StartMoment_IsInclusive_OutageCountsAsOngoing()
        => PowerOutagePhaseRules.Phase(Now, Now.AddHours(2), Now)
            .Should().Be(OutagePhase.Ongoing, "mobil `!startTime.isAfter(now)` diyor — başlangıç anı DÂHİL");

    [Fact]
    public void EndMoment_IsExclusive_OutageCountsAsPast()
        => PowerOutagePhaseRules.Phase(Now.AddHours(-2), Now, Now)
            .Should().Be(OutagePhase.Past, "mobil `endTime.isAfter(now)` diyor — bitiş anı HARİÇ");

    [Fact]
    public void OneSecondBeforeStart_IsPlanned()
        => PowerOutagePhaseRules.Phase(Now.AddSeconds(1), Now.AddHours(3), Now)
            .Should().Be(OutagePhase.Planned);

    [Fact]
    public void OneSecondBeforeEnd_IsStillOngoing()
        => PowerOutagePhaseRules.Phase(Now.AddHours(-3), Now.AddSeconds(1), Now)
            .Should().Be(OutagePhase.Ongoing);

    [Theory]
    [InlineData("ongoing", OutagePhase.Ongoing)]
    [InlineData("planned", OutagePhase.Planned)]
    [InlineData("past", OutagePhase.Past)]
    public void Parse_MapsFilterKeys(string raw, OutagePhase expected)
        => PowerOutagePhaseRules.Parse(raw).Should().Be(expected);

    /// <summary>Tanınmayan anahtar süzmemeli — sessizce yanlış bir hâle düşmemeli.</summary>
    [Fact]
    public void Parse_UnknownKey_DoesNotFilter()
        => PowerOutagePhaseRules.Parse("bilinmeyen").Should().BeNull();

    // ─────────────────── ekran: süzgeç gerçekten süzüyor ───────────────────

    private async Task SeedAsync(string suffix, DateTime start, DateTime end)
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.PowerOutages.Add(new PowerOutage
            {
                Neighborhood = $"{_marker}-{suffix}",
                StartTime = start,
                EndTime = end,
                Reason = "Filtre testi"
            });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task PhaseFilter_SeparatesOngoingFromPlannedAndPast()
    {
        var client = await _factory.SuperAdminAsync();
        var now = DateTime.UtcNow;

        await SeedAsync("SUREN", now.AddHours(-1), now.AddHours(1));
        await SeedAsync("PLANLI", now.AddDays(2), now.AddDays(2).AddHours(3));
        await SeedAsync("BITEN", now.AddDays(-3), now.AddDays(-3).AddHours(2));

        var ongoing = await (await client.GetAsync("/PowerOutagesAdmin/Index?phase=ongoing&limit=200")).ReadDecodedBodyAsync();
        ongoing.Should().Contain($"{_marker}-SUREN");
        ongoing.Should().NotContain($"{_marker}-PLANLI", "planlı kesinti 'sürüyor' filtresinde görünmemeli");
        ongoing.Should().NotContain($"{_marker}-BITEN");

        var planned = await (await client.GetAsync("/PowerOutagesAdmin/Index?phase=planned")).ReadDecodedBodyAsync();
        planned.Should().Contain($"{_marker}-PLANLI");
        planned.Should().NotContain($"{_marker}-SUREN");

        var past = await (await client.GetAsync("/PowerOutagesAdmin/Index?phase=past")).ReadDecodedBodyAsync();
        past.Should().Contain($"{_marker}-BITEN");
        past.Should().NotContain($"{_marker}-SUREN");
    }

    [Fact]
    public async Task NeighbourhoodFilter_IsCaseInsensitiveAndPartial()
    {
        var client = await _factory.SuperAdminAsync();
        var now = DateTime.UtcNow;

        await SeedAsync("Istasyon", now.AddHours(-1), now.AddHours(1));
        await SeedAsync("Camikebir", now.AddHours(-1), now.AddHours(1));

        var html = await (await client.GetAsync($"/PowerOutagesAdmin/Index?neighborhood={_marker}-ista")).ReadDecodedBodyAsync();

        html.Should().Contain($"{_marker}-Istasyon", "arama parçalı ve harf duyarsız olmalı");
        html.Should().NotContain($"{_marker}-Camikebir");
    }

    /// <summary>
    /// 🔑 Tarih aralığı **kesişim** üzerinden çalışmalı: 1–3 Ağustos'u seçen yönetici,
    /// 31 Temmuz'da başlayıp 2 Ağustos'ta biten kesintiyi de görmek ister. Yalnız
    /// <c>StartTime</c>'a bakan bir süzgeç uzun kesintileri sessizce elerdi — ve tam da
    /// o kesintiler en önemlileridir.
    /// </summary>
    [Fact]
    public async Task DateRange_IncludesOutagesThatMerelyOverlapTheRange()
    {
        var client = await _factory.SuperAdminAsync();
        var anchor = DateTime.UtcNow.Date.AddDays(10); // geçmiş/gelecek karışmasın diye sabit pencere

        await SeedAsync("UZUN", anchor.AddDays(-1).AddHours(20), anchor.AddDays(1).AddHours(4));
        await SeedAsync("DISARDA", anchor.AddDays(20), anchor.AddDays(20).AddHours(2));

        var range = $"from={anchor:yyyy-MM-dd}&to={anchor.AddDays(1):yyyy-MM-dd}";
        var html = await (await client.GetAsync($"/PowerOutagesAdmin/Index?{range}")).ReadDecodedBodyAsync();

        html.Should().Contain($"{_marker}-UZUN", "aralığa taşan kesinti listede kalmalı");
        html.Should().NotContain($"{_marker}-DISARDA", "aralığın tamamen dışındaki kesinti elenmeli");
    }

    [Fact]
    public async Task NoFilter_ShowsEverything_AndClearLinkIsHidden()
    {
        var client = await _factory.SuperAdminAsync();
        await SeedAsync("HEPSI", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

        var html = await (await client.GetAsync("/PowerOutagesAdmin/Index")).ReadDecodedBodyAsync();

        html.Should().Contain($"{_marker}-HEPSI");
        html.Should().NotContain("Temizle", "filtre uygulanmamışken temizleme bağlantısı gereksiz");
    }

    [Fact]
    public async Task StatusColumn_IsTurkish_NotRaw()
    {
        var client = await _factory.SuperAdminAsync();
        await SeedAsync("ROZET", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

        var html = await (await client.GetAsync("/PowerOutagesAdmin/Index?phase=ongoing")).ReadDecodedBodyAsync();

        html.Should().Contain("Sürüyor");
        html.Should().NotContain(">ongoing<", "ham İngilizce durum ekrana sızmamalı");
    }
}
