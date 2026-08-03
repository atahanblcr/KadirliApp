using System.Net;
using FluentAssertions;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Application.Features.Transport.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.17 — **şehirlerarası ulaşımın panelden yönetilebilmesi.**
///
/// 11.15c'nin canlı denetiminde bulunan **tek gerçek işlevsel boşluk** buydu:
/// <c>CreateIntercityRouteCommand</c> / <c>CreateIntercityScheduleCommand</c> /
/// <c>CreateIntracityStopCommand</c> 10.8'den beri <c>Application</c>'da hazırdı ama
/// onları çağıran hiçbir istemci yoktu. Mobildeki "Şehirlerarası" sekmesi seed verisiyle
/// yaşıyordu; ilk saat değişikliğinde <c>psql</c> gerekiyordu.
///
/// 🔑 Buradaki testlerin ayırt edici iddiası **"kayıt oluştu"** değil,
/// **"panelden girilen veri mobilin gördüğü sorguya düşüyor"**. Panelin yazdığıyla
/// vatandaşın okuduğu ayrışırsa kimse hata almaz (görünmez sözleşme #23'ün sınıfı) —
/// bu yüzden iddia doğrudan mobil ucun kullandığı <c>OnlyActive: true</c> sorgusuyla kurulur.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelTransportTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "TransportTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelTransportTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            // Saatler/duraklar FK cascade ile gider.
            await db.IntercityRoutes.Where(r => r.Destination.Contains(_marker)).ExecuteDeleteAsync();
            await db.IntracityRoutes.Where(r => r.RouteName.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    private async Task<T?> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        T? result = default;
        await _factory.WithScopeAsync(async sp => result = await query(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    /// <summary>Mobil ucun kullandığı sorgunun aynısı — panel ile vatandaşın gördüğü tek yerde buluşur.</summary>
    private async Task<IntercityRouteResponseDto?> AsMobileSeesAsync(string destination)
    {
        IntercityRouteResponseDto? found = null;
        await _factory.WithScopeAsync(async sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            var result = await sender.Send(new GetIntercityRoutesQuery(
                new QueryTransportDto { SearchTerm = destination, Page = 1, Limit = 50 }, onlyActive: true));
            found = result.Items.FirstOrDefault(r => r.Destination == destination);
        });
        return found;
    }

    private async Task<Guid> CreateIntercityRouteAsync(HttpClient client, string destination)
    {
        var response = await client.PostFormAsync("/TransportAdmin/IntercityCreate",
            new Dictionary<string, string>
            {
                ["Destination"] = destination,
                ["Company"] = _marker + " Turizm",
                ["Price"] = "150",
                ["DurationMinutes"] = "90"
            },
            tokenFromPath: "/TransportAdmin/IntercityCreate");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "başarılı kayıt saat ekranına yönlendirir");

        var id = await QueryDbAsync(db => db.IntercityRoutes
            .Where(r => r.Destination == destination)
            .Select(r => r.Id)
            .FirstOrDefaultAsync());

        id.Should().NotBe(Guid.Empty, "panelden eklenen hat veritabanında olmalı");
        return id;
    }

    // ─────────────────────────── şehirlerarası hat ───────────────────────────

    [Fact]
    public async Task IntercityCreate_PersistsAndAppearsInTheList()
    {
        var client = await _factory.SuperAdminAsync();
        var destination = _marker + " Adana";

        await CreateIntercityRouteAsync(client, destination);

        var saved = await QueryDbAsync(db => db.IntercityRoutes.FirstOrDefaultAsync(r => r.Destination == destination));
        saved!.Price.Should().Be(150m, "form alanı modele bağlanmalı");
        saved.DurationMinutes.Should().Be(90);
        saved.IsActive.Should().BeTrue("yeni hat varsayılan olarak yayında olmalı");

        var list = await (await client.GetAsync("/TransportAdmin/Intercity")).ReadDecodedBodyAsync();
        list.Should().Contain(destination, "kaydedilen hat listede görünmeli");
    }

    /// <summary>
    /// 🔑 Bu fazın asıl iddiası: panelden girilen kalkış saati **mobilin gördüğü sorguya
    /// düşüyor**. Sadece "satır oluştu" demek yetmez — 11.15c'de panel "Aktif İlanlar 1"
    /// derken public uç 0 döndürmüştü ve kimse hata almamıştı.
    /// </summary>
    [Fact]
    public async Task AddSchedule_MakesTheDepartureVisibleToMobile()
    {
        var client = await _factory.SuperAdminAsync();
        var destination = _marker + " Osmaniye";
        var routeId = await CreateIntercityRouteAsync(client, destination);

        (await AsMobileSeesAsync(destination))!.Schedules
            .Should().BeEmpty("saat eklenmeden önce mobil hiçbir sefer görmemeli — sonraki iddia bu yüzden anlamlı");

        var response = await client.PostFormAsync("/TransportAdmin/AddSchedule",
            new Dictionary<string, string> { ["routeId"] = routeId.ToString(), ["departureTime"] = "14:30" },
            tokenFromPath: $"/TransportAdmin/IntercityEdit/{routeId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var asMobileSees = await AsMobileSeesAsync(destination);
        asMobileSees!.Schedules.Select(s => s.DepartureTime)
            .Should().Contain("14:30", "panelden girilen kalkış mobil listede görünmeli");
    }

    [Fact]
    public async Task AddSchedule_RejectsDuplicateDeparture()
    {
        var client = await _factory.SuperAdminAsync();
        var destination = _marker + " Kozan";
        var routeId = await CreateIntercityRouteAsync(client, destination);

        var fields = new Dictionary<string, string> { ["routeId"] = routeId.ToString(), ["departureTime"] = "08:00" };
        var editPath = $"/TransportAdmin/IntercityEdit/{routeId}";

        await client.PostFormAsync("/TransportAdmin/AddSchedule", fields, editPath);
        await client.PostFormAsync("/TransportAdmin/AddSchedule", fields, editPath);

        var count = await QueryDbAsync(db => db.IntercitySchedules.CountAsync(s => s.RouteId == routeId));
        count.Should().Be(1, "aynı hatta aynı saat iki kez yazılmamalı — mobilde sefer mükerrer görünürdü");

        // Hata kullanıcıya gösterilmeli; sessizce yutulursa yönetici saatin eklendiğini sanır.
        var page = await (await client.GetAsync(editPath)).ReadDecodedBodyAsync();
        page.Should().Contain("08:00");
    }

    [Fact]
    public async Task DeleteSchedule_RemovesTheDepartureFromMobile()
    {
        var client = await _factory.SuperAdminAsync();
        var destination = _marker + " Ceyhan";
        var routeId = await CreateIntercityRouteAsync(client, destination);
        var editPath = $"/TransportAdmin/IntercityEdit/{routeId}";

        await client.PostFormAsync("/TransportAdmin/AddSchedule",
            new Dictionary<string, string> { ["routeId"] = routeId.ToString(), ["departureTime"] = "17:45" }, editPath);

        var scheduleId = await QueryDbAsync(db => db.IntercitySchedules
            .Where(s => s.RouteId == routeId).Select(s => s.Id).FirstOrDefaultAsync());

        await client.PostFormAsync("/TransportAdmin/DeleteSchedule",
            new Dictionary<string, string> { ["id"] = scheduleId.ToString(), ["routeId"] = routeId.ToString() }, editPath);

        (await AsMobileSeesAsync(destination))!.Schedules
            .Should().BeEmpty("silinen kalkış mobilde kalmamalı");
    }

    /// <summary>
    /// Hat pasife çekilince mobil onu hiç görmemeli. Panel "kaydedildi" derken vatandaşın
    /// listesinde durmaya devam etmesi, 11.15c'de düzeltilen ayrışmanın aynısı olurdu.
    /// </summary>
    [Fact]
    public async Task IntercityEdit_DeactivatingHidesTheRouteFromMobile()
    {
        var client = await _factory.SuperAdminAsync();
        var destination = _marker + " Kahramanmaras";
        var routeId = await CreateIntercityRouteAsync(client, destination);

        (await AsMobileSeesAsync(destination)).Should().NotBeNull("hat önce yayında olmalı");

        var response = await client.PostFormAsync("/TransportAdmin/IntercityEdit",
            new Dictionary<string, string>
            {
                ["Id"] = routeId.ToString(),
                ["Destination"] = destination,
                ["Company"] = _marker + " Turizm",
                ["Price"] = "175",
                ["DurationMinutes"] = "120",
                ["IsActive"] = "false"
            },
            tokenFromPath: $"/TransportAdmin/IntercityEdit/{routeId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var saved = await QueryDbAsync(db => db.IntercityRoutes.FirstAsync(r => r.Id == routeId));
        saved!.Price.Should().Be(175m, "düzenleme gerçekten kaydedilmeli");
        saved.IsActive.Should().BeFalse();

        (await AsMobileSeesAsync(destination)).Should().BeNull("pasif hat mobil listede görünmemeli");
    }

    [Fact]
    public async Task IntercityDelete_RemovesRouteAndItsSchedules()
    {
        var client = await _factory.SuperAdminAsync();
        var destination = _marker + " Gaziantep";
        var routeId = await CreateIntercityRouteAsync(client, destination);
        var editPath = $"/TransportAdmin/IntercityEdit/{routeId}";

        await client.PostFormAsync("/TransportAdmin/AddSchedule",
            new Dictionary<string, string> { ["routeId"] = routeId.ToString(), ["departureTime"] = "06:15" }, editPath);

        await client.PostFormAsync("/TransportAdmin/IntercityDelete",
            new Dictionary<string, string> { ["id"] = routeId.ToString() }, "/TransportAdmin/Intercity");

        (await QueryDbAsync(db => db.IntercityRoutes.AnyAsync(r => r.Id == routeId)))
            .Should().BeFalse("hat silinmeli");
        (await QueryDbAsync(db => db.IntercitySchedules.AnyAsync(s => s.RouteId == routeId)))
            .Should().BeFalse("kalkış saatleri FK cascade ile gitmeli — öksüz satır kalırsa sorgu şişer");
    }

    /// <summary>Silme onayı 11.15c'den beri **kaydın adını** yazmak zorunda (checklist §4).</summary>
    [Fact]
    public async Task IntercityList_DeleteConfirmationNamesTheRoute()
    {
        var client = await _factory.SuperAdminAsync();
        var destination = _marker + " Mersin";
        await CreateIntercityRouteAsync(client, destination);

        var html = await (await client.GetAsync("/TransportAdmin/Intercity")).ReadDecodedBodyAsync();

        html.Should().Contain("data-confirm", "silme onayı inline confirm() ile değil data-confirm ile yazılır");
        html.Should().Contain($"{destination}\" hattını", "onay metni neyin silindiğini yazmalı");
    }

    /// <summary>
    /// Saati olmayan hat mobilde sefersiz görünür. Panel bunu sessizce normal bir satır gibi
    /// gösterirse yönetici hattı "girdim" sanır — bu boşluk 11.17'ye kadar tam olarak buydu.
    /// </summary>
    [Fact]
    public async Task IntercityList_WarnsAboutRoutesWithoutDepartures()
    {
        var client = await _factory.SuperAdminAsync();
        await CreateIntercityRouteAsync(client, _marker + " Icel");

        var html = await (await client.GetAsync("/TransportAdmin/Intercity")).ReadDecodedBodyAsync();

        html.Should().Contain("Saat girilmemiş", "saatsiz hat uyarısız listelenmemeli");
    }

    // ─────────────────────────── şehir içi duraklar ───────────────────────────

    private async Task<Guid> CreateIntracityRouteAsync(HttpClient client, string routeNumber)
    {
        var name = _marker + " Hattı";
        var response = await client.PostFormAsync("/TransportAdmin/Create",
            new Dictionary<string, string>
            {
                ["RouteNumber"] = routeNumber,
                ["RouteName"] = name,
                ["FrequencyMinutes"] = "20",
                ["IsActive"] = "true"
            },
            tokenFromPath: "/TransportAdmin/Create");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var id = await QueryDbAsync(db => db.IntracityRoutes
            .Where(r => r.RouteName == name).Select(r => r.Id).FirstOrDefaultAsync());
        id.Should().NotBe(Guid.Empty);
        return id;
    }

    [Fact]
    public async Task AddStop_PersistsInOrderAndAppearsOnThePage()
    {
        var client = await _factory.SuperAdminAsync();
        var routeId = await CreateIntracityRouteAsync(client, "T1");
        var stopsPath = $"/TransportAdmin/Stops/{routeId}";

        await client.PostFormAsync("/TransportAdmin/AddStop",
            new Dictionary<string, string>
            {
                ["routeId"] = routeId.ToString(),
                ["stopName"] = "Devlet Hastanesi",
                ["stopOrder"] = "1",
                ["timeFromStart"] = "12"
            }, stopsPath);

        var stop = await QueryDbAsync(db => db.IntracityStops.FirstOrDefaultAsync(s => s.RouteId == routeId));
        stop.Should().NotBeNull("panelden eklenen durak veritabanında olmalı");
        stop!.StopOrder.Should().Be(1);
        stop.TimeFromStart.Should().Be(12, "durak zaman çizelgesi bu alandan çiziliyor");

        var page = await (await client.GetAsync(stopsPath)).ReadDecodedBodyAsync();
        page.Should().Contain("Devlet Hastanesi");
    }

    [Fact]
    public async Task AddStop_RejectsDuplicateOrderOnTheSameRoute()
    {
        var client = await _factory.SuperAdminAsync();
        var routeId = await CreateIntracityRouteAsync(client, "T2");
        var stopsPath = $"/TransportAdmin/Stops/{routeId}";

        var fields = new Dictionary<string, string>
        {
            ["routeId"] = routeId.ToString(),
            ["stopName"] = "Otogar",
            ["stopOrder"] = "1"
        };

        await client.PostFormAsync("/TransportAdmin/AddStop", fields, stopsPath);
        await client.PostFormAsync("/TransportAdmin/AddStop", fields, stopsPath);

        var count = await QueryDbAsync(db => db.IntracityStops.CountAsync(s => s.RouteId == routeId));
        count.Should().Be(1, "sıra güzergâhı belirler; iki durak aynı sırada olamaz");
    }

    [Fact]
    public async Task DeleteStop_RemovesItFromTheRoute()
    {
        var client = await _factory.SuperAdminAsync();
        var routeId = await CreateIntracityRouteAsync(client, "T3");
        var stopsPath = $"/TransportAdmin/Stops/{routeId}";

        await client.PostFormAsync("/TransportAdmin/AddStop",
            new Dictionary<string, string>
            {
                ["routeId"] = routeId.ToString(),
                ["stopName"] = "Belediye",
                ["stopOrder"] = "1"
            }, stopsPath);

        var stopId = await QueryDbAsync(db => db.IntracityStops
            .Where(s => s.RouteId == routeId).Select(s => s.Id).FirstOrDefaultAsync());

        await client.PostFormAsync("/TransportAdmin/DeleteStop",
            new Dictionary<string, string> { ["id"] = stopId.ToString(), ["routeId"] = routeId.ToString() }, stopsPath);

        (await QueryDbAsync(db => db.IntracityStops.AnyAsync(s => s.RouteId == routeId)))
            .Should().BeFalse("silinen durak listede kalmamalı");
    }

    /// <summary>
    /// Ulaşımın iki tarafı tek menü satırından ulaşılabilir olmalı. Şehirlerarası sekmesi
    /// çizilmezse ekran erişilebilir ama **bulunamaz** olur ("gizli buton" — checklist §4).
    /// </summary>
    [Fact]
    public async Task IntracityList_LinksToTheIntercityTab()
    {
        var client = await _factory.SuperAdminAsync();

        var html = await (await client.GetAsync("/TransportAdmin/Index")).ReadDecodedBodyAsync();

        html.Should().Contain("/TransportAdmin/Intercity", "şehirlerarası sekmesine geçiş bağlantısı olmalı");
        html.Should().Contain("Şehirlerarası Hatlar");
    }
}
