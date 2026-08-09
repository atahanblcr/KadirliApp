extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Application.Features.Transport.Queries;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.5 — <b>ulaşım alan modeli: araç tipi · kalkış noktası · sefer günleri.</b>
///
/// İddia "form açılıyor" değil; üç yeni görünmez sözleşmenin gerçekten tutması:
/// <list type="number">
///   <item><b>#46</b> — gün maskesinin tek sahibi <c>OperatingDays</c>; <c>0</c> yasak ve
///         uç seferleri günlere göre <b>elemez</b> (mağazadaki eski sürümler için liste
///         sebepsiz boşalmasın).</item>
///   <item><b>#47</b> — araç tipi kanonikleştirilerek yazılır, süzgeçte bilinmeyen değer
///         <b>süzmez</b>; 12.5 öncesi satırlar <c>bus</c> + <c>runsDaily</c> ile göç etti,
///         yani <b>davranış değişmedi</b>.</item>
///   <item><b>#48</b> — kalkış noktası sözlükten gelir, pasif nokta <b>seçilemez</b> ama
///         var olan bağ korunur; liste ile detay <b>aynı</b> projeksiyondan geçer.</item>
/// </list>
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelTransportFieldModelTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    private const string Marker = "CLAUDE-TRANSPORT-125";

    private Guid _otogar;
    private Guid _passivePoint;

    public PanelTransportFieldModelTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await CleanAsync();

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            // Otogar seed'den gelir (uygulamanın sabit verisi); pasif noktayı test kurar.
            _otogar = (await db.TransportDeparturePoints.FirstAsync(p => p.Slug == "kadirli-otogari")).Id;

            var passive = new TransportDeparturePoint
            {
                Name = $"{Marker} Eski Garaj",
                Slug = $"{Marker.ToLowerInvariant()}-eski-garaj",
                IsActive = false,
                DisplayOrder = 99
            };
            db.TransportDeparturePoints.Add(passive);
            await db.SaveChangesAsync();
            _passivePoint = passive.Id;
        });
    }

    public Task DisposeAsync() => CleanAsync();

    private Task CleanAsync() => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        // Seferler FK cascade ile gider.
        await db.IntercityRoutes.Where(r => r.Destination.Contains(Marker)).ExecuteDeleteAsync();
        await db.TransportDeparturePoints.Where(p => p.Name.Contains(Marker)).ExecuteDeleteAsync();
    });

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await action(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await sp.GetRequiredService<ISender>().Send(request));
        return result;
    }

    /// <summary>Mobil ucun kullandığı sorgunun aynısı — panel ile vatandaş tek yerde buluşur.</summary>
    private async Task<IntercityRouteResponseDto?> AsMobileSeesAsync(string destination, string? vehicleType = null)
    {
        var result = await SendAsync(new GetIntercityRoutesQuery(
            new QueryTransportDto { SearchTerm = Marker, VehicleType = vehicleType, Page = 1, Limit = 50 },
            onlyActive: true));

        return result.Items.FirstOrDefault(r => r.Destination == destination);
    }

    private async Task<Guid> CreateRouteAsync(
        HttpClient client, string destination, string? vehicleType = null, Guid? departurePointId = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["Destination"] = destination,
            ["Company"] = $"{Marker} Turizm"
        };
        if (vehicleType is not null) fields["VehicleType"] = vehicleType;
        if (departurePointId is not null) fields["DeparturePointId"] = departurePointId.Value.ToString();

        var response = await client.PostFormAsync("/TransportAdmin/IntercityCreate", fields,
            tokenFromPath: "/TransportAdmin/IntercityCreate");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var id = await InDbAsync(db => db.IntercityRoutes
            .Where(r => r.Destination == destination).Select(r => r.Id).FirstOrDefaultAsync());
        id.Should().NotBe(Guid.Empty);
        return id;
    }

    private Task<HttpResponseMessage> AddScheduleAsync(
        HttpClient client, Guid routeId, string time, params int[] days)
    {
        var fields = new Dictionary<string, string> { ["routeId"] = routeId.ToString(), ["departureTime"] = time };
        for (var i = 0; i < days.Length; i++)
            fields[$"days[{i}]"] = days[i].ToString();

        return client.PostFormAsync("/TransportAdmin/AddSchedule", fields,
            tokenFromPath: $"/TransportAdmin/IntercityEdit/{routeId}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #46 — SEFER GÜNLERİ
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 12.5'in <b>bütün riski</b> bu testte: göç eden satırların davranışı değişmemeli.
    /// Gün seçilmeden eklenen sefer "her gün" olmalı — panel bir seferi sessizce
    /// hafta içine kısarsa Pazar günü mobilde <b>sefer yokmuş gibi</b> görünür.
    /// </summary>
    [Fact]
    public async Task Schedule_WithoutDaySelection_RunsDaily()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Adana";
        var routeId = await CreateRouteAsync(admin, destination);

        await AddScheduleAsync(admin, routeId, "07:00");

        var saved = await InDbAsync(db => db.IntercitySchedules.FirstAsync(s => s.RouteId == routeId));
        saved.OperatingDays.Should().Be(OperatingDays.Daily);

        var schedule = (await AsMobileSeesAsync(destination))!.Schedules.Single();
        schedule.RunsDaily.Should().BeTrue();
        schedule.Days.Should().Equal("mon", "tue", "wed", "thu", "fri", "sat", "sun");
    }

    /// <summary>Panelden seçilen günler mobilin gördüğü sorguya <b>kod olarak</b> düşmeli.</summary>
    [Fact]
    public async Task Schedule_WithWeekdaysOnly_ReachesMobileAsCodes()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Kozan";
        var routeId = await CreateRouteAsync(admin, destination);

        await AddScheduleAsync(admin, routeId, "06:30",
            OperatingDays.Monday, OperatingDays.Tuesday, OperatingDays.Wednesday,
            OperatingDays.Thursday, OperatingDays.Friday);

        var saved = await InDbAsync(db => db.IntercitySchedules.FirstAsync(s => s.RouteId == routeId));
        saved.OperatingDays.Should().Be(OperatingDays.Weekdays);

        var schedule = (await AsMobileSeesAsync(destination))!.Schedules.Single();
        schedule.Days.Should().Equal("mon", "tue", "wed", "thu", "fri");
        schedule.RunsDaily.Should().BeFalse();
        schedule.DepartureTime.Should().Be("06:30", "görünmez sözleşme #7: şehirlerarası saat 'HH:mm'");
    }

    /// <summary>
    /// 🔴 <b>Uç seferleri günlere göre ELEMEZ.</b> Mağazadaki eski sürümler <c>days</c>'i
    /// tanımıyor; sunucuda elenseydi onlar için liste <b>sebepsiz boşalırdı</b>. Bugünkü
    /// doğruluk seviyesi korunuyor — bu bilinçli bir uyumluluk kararı.
    /// </summary>
    [Fact]
    public async Task Endpoint_DoesNotFilterSchedulesByDay()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Ceyhan";
        var routeId = await CreateRouteAsync(admin, destination);

        // Yalnız Pazar çalışan bir sefer: sunucu günü süzseydi haftanın 6 günü kaybolurdu.
        await AddScheduleAsync(admin, routeId, "09:15", OperatingDays.Sunday);

        var schedule = (await AsMobileSeesAsync(destination))!.Schedules.Single();
        schedule.DepartureTime.Should().Be("09:15", "sefer hangi gün olursa olsun listede durmalı");
        schedule.Days.Should().Equal("sun");
    }

    /// <summary>
    /// 🔴 Hiçbir gün çalışmayan sefer <b>reddedilir</b>: panelde duran ama mobilde hiç
    /// görünmeyen bir kayıt, yöneticinin "girdim" sandığı bir boşluktur.
    /// </summary>
    [Fact]
    public async Task Schedule_WithNoDay_IsRejectedAndSaysWhy()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Mersin";
        var routeId = await CreateRouteAsync(admin, destination);

        // İlk sefer "her gün" olarak eklenir, sonra günleri boşaltmayı deneriz:
        // AddSchedule'da gün gönderilmemesi "her gün" demek (eski davranış), Update'te ise
        // gerçekten "hiçbir gün" demektir — asıl kapı burada.
        await AddScheduleAsync(admin, routeId, "10:00", OperatingDays.Monday);
        var scheduleId = await InDbAsync(db => db.IntercitySchedules
            .Where(s => s.RouteId == routeId).Select(s => s.Id).FirstAsync());

        var response = await admin.PostFormAsync("/TransportAdmin/UpdateSchedule",
            new Dictionary<string, string>
            {
                ["id"] = scheduleId.ToString(),
                ["routeId"] = routeId.ToString(),
                ["departureTime"] = "10:00",
                ["isActive"] = "true"
                // days YOK → maske 0
            },
            tokenFromPath: $"/TransportAdmin/IntercityEdit/{routeId}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var saved = await InDbAsync(db => db.IntercitySchedules.FirstAsync(s => s.Id == scheduleId));
        saved.OperatingDays.Should().Be(OperatingDays.Monday, "geçersiz gün seçimi kaydı EZMEMELİ");

        var page = await (await admin.GetAsync($"/TransportAdmin/IntercityEdit/{routeId}")).ReadDecodedBodyAsync();
        page.Should().Contain("en az bir gün", "sessizce yutulursa yönetici günü kaldırdığını sanır");
    }

    /// <summary>
    /// Sefer <b>düzenlenebilir</b> olmalı: 12.5 öncesinde tek yol "sil + yeniden ekle"ydi ve
    /// gün maskesiyle birlikte bir düzenleme denetim izinde <b>silme</b> olarak görünürdü.
    /// </summary>
    [Fact]
    public async Task UpdateSchedule_ChangesDaysWithoutRecreatingTheRow()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Gaziantep";
        var routeId = await CreateRouteAsync(admin, destination);

        await AddScheduleAsync(admin, routeId, "08:00");
        var scheduleId = await InDbAsync(db => db.IntercitySchedules
            .Where(s => s.RouteId == routeId).Select(s => s.Id).FirstAsync());

        await admin.PostFormAsync("/TransportAdmin/UpdateSchedule",
            new Dictionary<string, string>
            {
                ["id"] = scheduleId.ToString(),
                ["routeId"] = routeId.ToString(),
                ["departureTime"] = "08:30",
                ["days[0]"] = OperatingDays.Saturday.ToString(),
                ["days[1]"] = OperatingDays.Sunday.ToString(),
                ["isActive"] = "true"
            },
            tokenFromPath: $"/TransportAdmin/IntercityEdit/{routeId}");

        var saved = await InDbAsync(db => db.IntercitySchedules.FirstAsync(s => s.Id == scheduleId));
        saved.Id.Should().Be(scheduleId, "satır yeniden yaratılmamalı — kimlik korunur");
        saved.DepartureTime.Should().Be(TimeSpan.FromMinutes(8 * 60 + 30));
        saved.OperatingDays.Should().Be(OperatingDays.Weekend);

        (await AsMobileSeesAsync(destination))!.Schedules.Single().Days.Should().Equal("sat", "sun");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #47 — ARAÇ TİPİ
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task VehicleType_IsWrittenCanonicallyAndReachesMobile()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Minibus Hatti";

        await CreateRouteAsync(admin, destination, vehicleType: "MINIBUS");

        var saved = await InDbAsync(db => db.IntercityRoutes.FirstAsync(r => r.Destination == destination));
        saved.VehicleType.Should().Be(TransportVehicleTypes.Minibus, "kanonikleştirme kayıt yolunda yapılır");

        (await AsMobileSeesAsync(destination))!.VehicleType.Should().Be("minibus");
    }

    /// <summary>12.5 öncesi satırların ve tip seçilmeyen kayıtların değeri <b>otobüs</b>.</summary>
    [Fact]
    public async Task VehicleType_DefaultsToBus()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Tipsiz";

        await CreateRouteAsync(admin, destination);

        (await AsMobileSeesAsync(destination))!.VehicleType.Should().Be(TransportVehicleTypes.Bus);
    }

    [Fact]
    public async Task VehicleTypeFilter_SelectsOnlyThatType()
    {
        var admin = await _factory.SuperAdminAsync();
        var bus = $"{Marker} Otobus";
        var minibus = $"{Marker} Minibus";

        await CreateRouteAsync(admin, bus, TransportVehicleTypes.Bus);
        await CreateRouteAsync(admin, minibus, TransportVehicleTypes.Minibus);

        (await AsMobileSeesAsync(minibus, TransportVehicleTypes.Minibus)).Should().NotBeNull();
        (await AsMobileSeesAsync(bus, TransportVehicleTypes.Minibus)).Should().BeNull("otobüs hattı minibüs süzgecinde çıkmamalı");
    }

    /// <summary>
    /// 🔴 Tanınmayan süzgeç değeri <b>listeyi boşaltmaz</b> (<c>ARCHITECTURE.md</c> §5).
    /// 400 dönseydi ya da hiçbir şey döndürmeseydi, bir yazım hatası ekranı sessizce boşaltırdı.
    /// </summary>
    [Fact]
    public async Task UnknownVehicleTypeFilter_FallsBackToTheFullList()
    {
        var admin = await _factory.SuperAdminAsync();
        var bus = $"{Marker} Hatay";
        var minibus = $"{Marker} Hatay Minibus";

        await CreateRouteAsync(admin, bus, TransportVehicleTypes.Bus);
        await CreateRouteAsync(admin, minibus, TransportVehicleTypes.Minibus);

        // ⚠️ İddia HER İKİ TİPİ birden kapsamalı: yalnız otobüs kontrol edilseydi, bilinmeyen
        // değeri "bus"a düşüren bir gerçekleme testi geçerdi ve süzgeç sessizce yanlış çalışırdı.
        (await AsMobileSeesAsync(bus, "otobus")).Should().NotBeNull();
        (await AsMobileSeesAsync(minibus, "otobus")).Should().NotBeNull(
            "tanınmayan değer SÜZMEZ — minibüs hattı da listede kalmalı");

        var page = await admin.GetAsync("/TransportAdmin/Intercity?vehicleType=otobus");
        page.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await page.ReadDecodedBodyAsync();
        html.Should().Contain(bus);
        html.Should().Contain(minibus);
    }

    /// <summary>Ham <c>bus</c>/<c>minibus</c> panelde görünmemeli (Değişmez Kural #6).</summary>
    [Fact]
    public async Task Panel_ShowsTurkishVehicleLabels()
    {
        var admin = await _factory.SuperAdminAsync();
        await CreateRouteAsync(admin, $"{Marker} Etiket", TransportVehicleTypes.Minibus);

        var html = await (await admin.GetAsync("/TransportAdmin/Intercity")).ReadDecodedBodyAsync();

        html.Should().Contain("Minibüs");
        html.Should().NotContain(">minibus<", "ham İngilizce değer ekrana basılmamalı");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #48 — KALKIŞ NOKTASI
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeparturePoint_ComesFromTheLookupAndReachesMobile()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Otogardan";

        await CreateRouteAsync(admin, destination, departurePointId: _otogar);

        var dto = (await AsMobileSeesAsync(destination))!;
        dto.DeparturePointId.Should().Be(_otogar);
        dto.DeparturePointName.Should().Be("Kadirli Otogarı", "ad sözlükten gelir, forma yazılmaz");
    }

    /// <summary>
    /// 🔴 <b>Pasif nokta seçilemez.</b> Kabul edilseydi hat, panelde artık listelenmeyen bir
    /// noktaya bağlı kalır ve yönetici o bağı bir daha göremezdi.
    /// </summary>
    [Fact]
    public async Task PassiveDeparturePoint_IsRejected()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Pasif";

        var response = await admin.PostFormAsync("/TransportAdmin/IntercityCreate",
            new Dictionary<string, string>
            {
                ["Destination"] = destination,
                ["DeparturePointId"] = _passivePoint.ToString()
            },
            tokenFromPath: "/TransportAdmin/IntercityCreate");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "reddedilen form yeniden çizilir, yönlendirmez");

        (await InDbAsync(db => db.IntercityRoutes.AnyAsync(r => r.Destination == destination)))
            .Should().BeFalse("pasif kalkış noktalı hat oluşmamalı");
    }

    /// <summary>
    /// Pasif nokta <b>var olan bağı koparmaz</b> ve o kayıtta seçili kalır — düşseydi form
    /// kaydedildiğinde hattın kalkış noktası <b>sessizce boşalırdı</b>.
    /// </summary>
    [Fact]
    public async Task PassiveDeparturePoint_StaysSelectedOnAnExistingRoute()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Sonradan Pasif";
        var routeId = await CreateRouteAsync(admin, destination, departurePointId: _otogar);

        // Nokta sonradan pasifleştirilir (yönetici sözlükten kapatır).
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var point = await db.TransportDeparturePoints.FirstAsync(p => p.Id == _otogar);
            point.IsActive = false;
            await db.SaveChangesAsync();
        });

        try
        {
            var html = await (await admin.GetAsync($"/TransportAdmin/IntercityEdit/{routeId}")).ReadDecodedBodyAsync();
            html.Should().Contain(_otogar.ToString(), "seçili nokta pasif olsa da listede kalmalı");
            html.Should().Contain("(pasif)", "pasif olduğu söylenmeli — sessizce normal görünmemeli");

            (await InDbAsync(db => db.IntercityRoutes.FirstAsync(r => r.Id == routeId)))
                .DeparturePointId.Should().Be(_otogar, "pasifleştirme geçmişi silmez");
        }
        finally
        {
            await _factory.WithScopeAsync(async sp =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                var point = await db.TransportDeparturePoints.FirstAsync(p => p.Id == _otogar);
                point.IsActive = true;
                await db.SaveChangesAsync();
            });
        }
    }

    /// <summary>
    /// 🐛 <b>12.5 canlı denetiminde bulunan hata sınıfı.</b> Kalkış noktası sonradan
    /// pasifleştirilen bir hat, <b>başka hiçbir alanı</b> değiştirilemez hâle gelmemeli:
    /// yönetici yalnız fiyatı güncellemek istese bile <b>hiç dokunmadığı bir alan</b> yüzünden
    /// hata alırdı ve kaydedebilmesinin tek yolu hattı başka bir noktaya taşımaktı.
    /// Kural "pasif nokta <b>yeni olarak</b> seçilemez"tir, "pasif noktalı kayıt donar" değil.
    /// </summary>
    [Fact]
    public async Task RouteWithADeactivatedDeparturePoint_CanStillBeEdited()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Donmus";
        var routeId = await CreateRouteAsync(admin, destination, departurePointId: _otogar);

        await SetPointActiveAsync(_otogar, false);
        try
        {
            var response = await admin.PostFormAsync("/TransportAdmin/IntercityEdit",
                new Dictionary<string, string>
                {
                    ["Id"] = routeId.ToString(),
                    ["Destination"] = destination,
                    ["Company"] = $"{Marker} Turizm",
                    ["Price"] = "175",
                    ["VehicleType"] = TransportVehicleTypes.Bus,
                    // Form pasif noktayı SEÇİLİ tutuyor (doğru karar) — bu değer geri geliyor.
                    ["DeparturePointId"] = _otogar.ToString(),
                    ["IsActive"] = "true"
                },
                tokenFromPath: $"/TransportAdmin/IntercityEdit/{routeId}");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect, "düzenleme kabul edilmeli");

            var saved = await InDbAsync(db => db.IntercityRoutes.FirstAsync(r => r.Id == routeId));
            saved.Price.Should().Be(175m, "fiyat güncellenebilmeli");
            saved.DeparturePointId.Should().Be(_otogar, "pasif de olsa var olan bağ korunmalı");
        }
        finally
        {
            await SetPointActiveAsync(_otogar, true);
        }
    }

    private Task SetPointActiveAsync(Guid id, bool isActive) => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var point = await db.TransportDeparturePoints.FirstAsync(p => p.Id == id);
        point.IsActive = isActive;
        await db.SaveChangesAsync();
    });

    /// <summary>
    /// 🔴 Liste ve detay <b>aynı projeksiyondan</b> geçmeli (görünmez sözleşme #43'ün sınıfı).
    /// 12.4'te etkinlikte iki ayrı <c>Select</c> bloğu vardı ve yeni alanların yalnız birine
    /// eklenmesi detayı <b>sessizce eksik</b> bırakırdı — ne derleyici ne test yakalardı.
    /// </summary>
    [Fact]
    public async Task DetailAndList_ReturnTheSameFields()
    {
        var admin = await _factory.SuperAdminAsync();
        var destination = $"{Marker} Projeksiyon";
        var routeId = await CreateRouteAsync(admin, destination, TransportVehicleTypes.Minibus, _otogar);
        await AddScheduleAsync(admin, routeId, "11:45", OperatingDays.Friday);

        var fromList = (await AsMobileSeesAsync(destination))!;
        var fromDetail = (await SendAsync(new GetIntercityRouteByIdQuery(routeId)))!;

        fromDetail.VehicleType.Should().Be(fromList.VehicleType);
        fromDetail.DeparturePointId.Should().Be(fromList.DeparturePointId);
        fromDetail.DeparturePointName.Should().Be(fromList.DeparturePointName);
        fromDetail.DeparturePointAddress.Should().Be(fromList.DeparturePointAddress);
        fromDetail.DeparturePointLatitude.Should().Be(fromList.DeparturePointLatitude);
        fromDetail.DeparturePointLongitude.Should().Be(fromList.DeparturePointLongitude);

        fromDetail.Schedules.Single().Days.Should().Equal(fromList.Schedules.Single().Days!);
        fromDetail.Schedules.Single().RunsDaily.Should().Be(fromList.Schedules.Single().RunsDaily);
    }

    /// <summary>
    /// Kalkış noktası olmayan hat panelde <b>uyarı</b> ile görünmeli: sessizce boş bir hücre,
    /// yöneticinin bilgiyi girdiğini sanmasına yol açar (saatsiz hat uyarısının aynı gerekçesi).
    /// </summary>
    [Fact]
    public async Task Panel_WarnsAboutRoutesWithoutADeparturePoint()
    {
        var admin = await _factory.SuperAdminAsync();
        await CreateRouteAsync(admin, $"{Marker} Noktasiz");

        var html = await (await admin.GetAsync("/TransportAdmin/Intercity")).ReadDecodedBodyAsync();

        html.Should().Contain("Girilmemiş", "kalkış noktası boş olan hat uyarısız listelenmemeli");
    }

    /// <summary>
    /// Araç şeridi mevcut sorgu dizesini <b>aynen taşımalı</b>: 12.4'te bu şeritler
    /// <c>asp-route-*</c> ile elle sayılmış ve <c>search</c>/<c>sort</c> kaybolmuştu —
    /// hiçbir test kırılmadan liste yeniden sıralanıyordu.
    /// </summary>
    [Fact]
    public async Task VehicleChips_PreserveTheCurrentFilter()
    {
        var admin = await _factory.SuperAdminAsync();
        await CreateRouteAsync(admin, $"{Marker} Serit", TransportVehicleTypes.Minibus);

        var html = await (await admin.GetAsync($"/TransportAdmin/Intercity?search={Marker}")).ReadDecodedBodyAsync();

        html.Should().Contain($"search={Marker}&vehicleType=minibus",
            "şerit bağlantısı mevcut aramayı korumalı — kaybolursa süzgeç sessizce sıfırlanır");
    }

    /// <summary>
    /// 🐛 <b>12.5 canlı denetiminde bulunan hata.</b> Gün kutuları düz kardeş olduğunda
    /// Tailwind'in <c>peer-checked:</c> kuralı <b>genel kardeş seçicisi</b> (<c>~</c>) ürettiği
    /// için ilk kutu (Pzt) işaretlendiği anda kendisinden <b>sonraki bütün</b> etiketler seçili
    /// stilini alıyordu: "Hafta içi" seçildiğinde Cmt ve Paz da <b>seçili görünüyor</b> ama
    /// veride yoktu. Panel bir şey söyler, kayıt başka şey der, kimse hata almaz — bu fazın
    /// tam olarak savaştığı sınıf.
    ///
    /// ⚠️ CSS'in kendisi testlenemez; testlenebilen <b>yapısal kural</b> şudur:
    /// her <c>day-bit</c> girdisi <b>kendi sarmalayıcısında</b> olmalı.
    /// </summary>
    [Fact]
    public async Task DayPicker_WrapsEachCheckboxSoPeerStylingCannotLeak()
    {
        var admin = await _factory.SuperAdminAsync();
        var routeId = await CreateRouteAsync(admin, $"{Marker} Gun Secici");

        var html = await (await admin.GetAsync($"/TransportAdmin/IntercityEdit/{routeId}")).ReadDecodedBodyAsync();

        var checkboxes = System.Text.RegularExpressions.Regex.Matches(html, "class=\"peer sr-only day-bit\"").Count;
        var wrappers = System.Text.RegularExpressions.Regex.Matches(html, "<span class=\"inline-flex\">").Count;

        checkboxes.Should().Be(7, "yeni hatta yalnız ekleme formu var — yedi gün kutusu");
        wrappers.Should().Be(checkboxes,
            "her gün kutusu KENDİ sarmalayıcısında olmalı; düz kardeş olurlarsa `peer-checked:` " +
            "genel kardeş seçicisiyle sonraki bütün etiketlere sızar ve seçili OLMAYAN günler " +
            "seçili görünür");
    }

    // ── Sözlük yönetimi ─────────────────────────────────────────────────────

    [Fact]
    public async Task LookupsAdmin_CreatesADeparturePointWithCoordinates()
    {
        var admin = await _factory.SuperAdminAsync();
        var name = $"{Marker} Yeni Durak";

        var response = await admin.PostFormAsync("/LookupsAdmin/DeparturePointCreate",
            new Dictionary<string, string>
            {
                ["name"] = name,
                ["address"] = "Test Cad. No:1",
                ["latitude"] = "37.3742",
                ["longitude"] = "36.0965",
                ["displayOrder"] = "50"
            },
            tokenFromPath: "/LookupsAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var saved = await InDbAsync(db => db.TransportDeparturePoints.FirstOrDefaultAsync(p => p.Name == name));
        saved.Should().NotBeNull();
        saved!.Latitude.Should().Be(37.3742m, "koordinat mobildeki 'Yol tarifi' butonunun kaynağı");
        saved.Slug.Should().NotBeNullOrWhiteSpace("slug SlugHelper'dan türetilir, elle yazılmaz");
    }

    /// <summary>
    /// Aynı ad ikinci kez eklenememeli: "Kadirli Otogarı" iki satır olsaydı koordinat
    /// düzeltmesi hangi satıra yazıldığına göre bazı hatlarda görünür, bazılarında görünmezdi.
    /// </summary>
    [Fact]
    public async Task LookupsAdmin_RejectsDuplicateDeparturePointName()
    {
        var admin = await _factory.SuperAdminAsync();
        var name = $"{Marker} Tekil";

        var fields = new Dictionary<string, string> { ["name"] = name, ["displayOrder"] = "51" };
        await admin.PostFormAsync("/LookupsAdmin/DeparturePointCreate", fields, tokenFromPath: "/LookupsAdmin/Index");
        await admin.PostFormAsync("/LookupsAdmin/DeparturePointCreate", fields, tokenFromPath: "/LookupsAdmin/Index");

        (await InDbAsync(db => db.TransportDeparturePoints.CountAsync(p => p.Name == name)))
            .Should().Be(1);
    }
}
