using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Application.Common.Utils;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Contracts;

/// <summary>
/// Faz 11.14 — **GÖRÜNMEZ SÖZLEŞMELER.** Bu dosyadaki her test, koda bakarak anlaşılmayan ama
/// bozulunca mobil istemcinin **sessizce** yanlış davranmasına yol açan bir bağımlılığı kilitler.
/// Hepsi <c>ARCHITECTURE.md</c> → "Görünmez sözleşmeler" bölümünde de yazılı; doküman ile test
/// birlikte değişmeli. Bir test kırmızıya döndüyse ya sözleşme bilinçli değişmiştir (o zaman
/// ARCHITECTURE.md ve mobil istemci aynı commit'te güncellenir) ya da kaza olmuştur.
///
/// ⚠️ "Bu testler neyi test ediyor ki?" diye silmeyin: hiçbiri yeni davranış eklemez, hepsi
/// **mevcut ve bilerek seçilmiş** davranışın kaza sonucu değişmesini engeller.
/// </summary>
public class InvisibleContractsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Marker = "CLAUDE-11.14";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InvisibleContractsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <summary>
    /// Test veritabanı yalnız <c>DbSeeder</c>'ın lookup verisiyle geliyor (kategori, mahalle,
    /// mezarlık, admin); modül kayıtları YOK. Bu testler kendi verisini kurar ve sonunda siler —
    /// başka test sınıflarının saydığı toplamları bozmamak için her satır <see cref="Marker"/>
    /// taşır.
    /// </summary>
    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        db.PowerOutages.AddRange(
            new PowerOutage
            {
                Neighborhood = $"{Marker} Yenimahalle", StartTime = now.AddHours(-1), EndTime = now.AddHours(1),
                Reason = "Süren kesinti", Source = Marker
            },
            new PowerOutage
            {
                Neighborhood = $"{Marker} Karataş", StartTime = now.AddDays(2), EndTime = now.AddDays(2).AddHours(3),
                Reason = "Planlı kesinti", Source = Marker
            });

        var intercity = new IntercityRoute
        {
            Destination = $"Adana {Marker}", Price = 220, DurationMinutes = 105, Company = Marker, IsActive = true
        };
        db.IntercityRoutes.Add(intercity);
        db.IntercitySchedules.AddRange(
            new IntercitySchedule { Route = intercity, DepartureTime = new TimeSpan(7, 0, 0), IsActive = true },
            new IntercitySchedule { Route = intercity, DepartureTime = new TimeSpan(14, 0, 0), IsActive = true });

        var intracity = new IntracityRoute
        {
            RouteNumber = "99", RouteName = $"Merkez - Hastane {Marker}",
            FirstDeparture = new TimeSpan(6, 30, 0), LastDeparture = new TimeSpan(22, 0, 0),
            FrequencyMinutes = 20, IsActive = true
        };
        db.IntracityRoutes.Add(intracity);
        db.IntracityStops.AddRange(
            new IntracityStop { Route = intracity, StopName = "Meydan", StopOrder = 1, TimeFromStart = 0 },
            new IntracityStop { Route = intracity, StopName = "Hastane", StopOrder = 2, TimeFromStart = 7 });

        var placeCategoryId = await db.PlaceCategories.Select(c => c.Id).FirstAsync();
        db.Places.Add(new Place
        {
            CategoryId = placeCategoryId, Name = $"Ala Cami {Marker}", Description = Marker,
            Latitude = 37.3735m, Longitude = 36.0961m, IsFree = true, IsActive = true
        });

        db.Pharmacies.Add(new Pharmacy
        {
            Name = $"Merkez Eczanesi {Marker}", Address = Marker, Phone = "+903281110000", IsActive = true
        });

        db.TaxiDrivers.Add(new TaxiDriver
        {
            Name = $"Ali Şoför {Marker}", Phone = "+905331110000", Plaka = "80 AB 001",
            IsVerified = true, VerifiedAt = now, IsActive = true
        });

        var guideCategoryId = await db.GuideCategories.Select(c => c.Id).FirstAsync();
        db.GuideItems.Add(new GuideItem
        {
            CategoryId = guideCategoryId, Name = $"Devlet Hastanesi {Marker}", Phone = "+903281110001", IsActive = true
        });

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.IntercitySchedules.Where(s => s.Route.Company == Marker).ExecuteDeleteAsync();
        await db.IntercityRoutes.Where(r => r.Company == Marker).ExecuteDeleteAsync();
        await db.IntracityStops.Where(s => s.Route.RouteName.Contains(Marker)).ExecuteDeleteAsync();
        await db.IntracityRoutes.Where(r => r.RouteName.Contains(Marker)).ExecuteDeleteAsync();
        await db.PowerOutages.Where(o => o.Source == Marker).ExecuteDeleteAsync();
        await db.Places.Where(p => p.Name.Contains(Marker)).ExecuteDeleteAsync();
        await db.PharmacySchedules.Where(s => s.Pharmacy.Name.Contains(Marker)).ExecuteDeleteAsync();
        await db.Pharmacies.Where(p => p.Name.Contains(Marker)).ExecuteDeleteAsync();
        await db.TaxiDrivers.Where(t => t.Name.Contains(Marker)).ExecuteDeleteAsync();
        await db.GuideItems.Where(g => g.Name.Contains(Marker)).ExecuteDeleteAsync();
    }

    private async Task<JsonDocument> GetJsonAsync(string url)
    {
        var response = await _client.GetAsync(url);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>Kendi kapsamında bir <c>AppDbContext</c> açar (Faz 0 denetiminde eklenen testler kullanıyor).</summary>
    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task<string> GetUserTokenAsync(string phone, string username)
    {
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        if (!data.GetProperty("isNewUser").GetBoolean())
            return data.GetProperty("accessToken").GetString()!;

        Guid neighborhoodId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            neighborhoodId = await db.Neighborhoods.Where(n => n.IsActive).Select(n => n.Id).FirstAsync();
        }
        var register = await _client.PostAsJsonAsync("/v1/auth/register",
            new { tempToken = data.GetProperty("tempToken").GetString(), username, primaryNeighborhoodId = neighborhoodId });
        using var regDoc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        return regDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    // ------------------------------------------------------------------ 1) Sayfalama

    /// <summary>
    /// 📌 <c>GET /v1/power-outages</c> **bilerek sayfalamıyor** ve düz dizi döndürüyor.
    /// Mobil (11.4 acil şerit + 11.6 kesinti ekranı) "süren / planlı / geçmiş" ayrımını
    /// **tüm listeye bakarak** yapıyor; uç sayfalanırsa acil şeritte süren kesinti
    /// görünmez olur ve kimse hata almaz — sadece bilgi kaybolur.
    /// </summary>
    [Fact]
    public async Task PowerOutages_ReturnFlatArray_NotPagedResult()
    {
        using var doc = await GetJsonAsync("/v1/power-outages");

        var data = doc.RootElement.GetProperty("data");
        data.ValueKind.Should().Be(JsonValueKind.Array,
            "kesinti listesi sayfalanmıyor — mobil süren/planlı ayrımını tam listeden türetiyor");
        data.EnumerateArray().Should().NotBeEmpty("seed en az bir kesinti içeriyor");

        // Sayfalama parametreleri verilse bile davranış değişmemeli (sessizce kırpma yok).
        using var pagedAttempt = await GetJsonAsync("/v1/power-outages?page=1&limit=1");
        pagedAttempt.RootElement.GetProperty("data").EnumerateArray().Count()
            .Should().Be(data.EnumerateArray().Count(), "uç page/limit'i yok sayar");
    }

    // ------------------------------------------------- 2) Announcements 200-quirk'i

    /// <summary>
    /// 📌 Duyuru uçları bulunamayan kayıt için **HTTP 200 + <c>success:false</c>** döndürüyor
    /// (diğer tüm modüller 404 veriyor). Mobil ağ katmanı (11.2 EnvelopeInterceptor) bunu
    /// normalleştiriyor. Uç "düzeltilip" 404'e çevrilirse istemci çalışmaya devam eder ama
    /// bu testin kırmızıya dönmesi, kontrat dokümanının ve interceptor yorumunun güncellenmesi
    /// gerektiğini haber verir.
    /// </summary>
    [Fact]
    public async Task Announcements_UnknownId_Returns200_WithSuccessFalse_NotHttp404()
    {
        var response = await _client.GetAsync($"/v1/announcements/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "duyuru ucunun bilinen aykırılığı");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("NOT_FOUND");
        doc.RootElement.GetProperty("meta").GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();

        // Karşılaştırma: başka bir modül aynı durumda gerçek 404 veriyor.
        (await _client.GetAsync($"/v1/places/{Guid.NewGuid()}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound, "aykırılık YALNIZ duyurulara özgü");
    }

    // -------------------------------------------------------- 3) view_count semantiği

    /// <summary>
    /// 📌 <c>GET /v1/ads/{id}</c> **her çağrıda** <c>view_count</c>'u artırır ve gövdede
    /// **artıştan ÖNCEKİ** değeri döndürür. Mobil detay ekranı (11.8) bu yüzden gelen sayıya
    /// +1 eklemiyor ve provider'ı gereksiz yere invalidate etmiyor. Semantik "artıştan sonraki
    /// değer"e çevrilirse ekranda sayı bir fazla görünmeye başlar.
    /// </summary>
    [Fact]
    public async Task AdDetail_IncrementsViewCount_AndReturnsPreIncrementValue()
    {
        var ownerToken = await GetUserTokenAsync("+905077770001", "claudetest_ic_view");
        var adId = await CreatePublishedAdAsync(ownerToken, "CLAUDE-TEST Görünmez Sözleşme Sayaç");

        using (var doc = await GetJsonAsync($"/v1/ads/{adId}"))
            doc.RootElement.GetProperty("data").GetProperty("viewCount").GetInt32()
                .Should().Be(0, "ilk okuma artıştan ÖNCEKİ değeri (0) döndürmeli");

        using (var doc = await GetJsonAsync($"/v1/ads/{adId}"))
            doc.RootElement.GetProperty("data").GetProperty("viewCount").GetInt32()
                .Should().Be(1, "ikinci okuma ilk okumanın artışını görmeli");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Ads.AsNoTracking().FirstAsync(a => a.Id == adId)).ViewCount
            .Should().Be(2, "iki okuma iki artış demek");
    }

    // -------------------------------------------------- 4) Arama parametresinin adı

    /// <summary>
    /// 📌 Arama parametresi **modülden modüle farklı**: taksi ve ulaşım <c>searchTerm</c>,
    /// diğerleri <c>search</c> bekliyor. Yanlış ad **sessizce yok sayılır** (400 gelmez,
    /// liste filtrelenmemiş döner) — 11.11 ve 11.12'de tam olarak bu tuzağa düşüldü.
    /// </summary>
    [Fact]
    public async Task SearchParameterName_IsSearchTerm_ForTaxisAndTransport_ButSearch_Elsewhere()
    {
        int unfiltered, correctParam, wrongParam;

        using (var doc = await GetJsonAsync("/v1/transport/intercity-routes"))
            unfiltered = doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32();
        unfiltered.Should().BeGreaterThan(0, "test kendi hattını kurdu");

        // Ulaşım: `searchTerm` gerçekten filtreler…
        using (var doc = await GetJsonAsync("/v1/transport/intercity-routes?searchTerm=zzzz-olmayan"))
            correctParam = doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32();
        correctParam.Should().Be(0, "ulaşım araması `searchTerm` ile çalışır");

        // …ama `search` **sessizce yok sayılır** (400 gelmez, liste filtrelenmemiş döner).
        using (var doc = await GetJsonAsync("/v1/transport/intercity-routes?search=zzzz-olmayan"))
            wrongParam = doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32();
        wrongParam.Should().Be(unfiltered, "yanlış parametre adı sessizce yok sayılır — hata beklemeyin");

        // Taksi de `searchTerm` kullanıyor; `search` orada da yok sayılır.
        using (var doc = await GetJsonAsync("/v1/taxis/drivers?searchTerm=zzzz-olmayan"))
            doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32()
                .Should().Be(0, "taksi araması `searchTerm` ile çalışmalı");
        using (var doc = await GetJsonAsync("/v1/taxis/drivers?search=zzzz-olmayan"))
            doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32()
                .Should().BeGreaterThan(0, "taksi ucunda `search` yok sayılır");

        // Rehber ise `search` kullanıyor — isim birliği YOK.
        using (var doc = await GetJsonAsync("/v1/guide/items?search=zzzz-olmayan"))
            doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32()
                .Should().Be(0, "rehber araması `search` ile çalışmalı");
    }

    // --------------------------------------------- 5) places.amenities = JSON içeren METİN

    /// <summary>
    /// 📌 <c>places.amenities</c> veritabanında <c>jsonb</c> ama DTO'da <c>string</c> →
    /// yanıtta **JSON nesnesi değil, JSON içeren METİN** gelir. Mobil (11.11) bu metni ayrıca
    /// <c>jsonDecode</c> ediyor. DTO tipi bir gün nesneye çevrilirse istemci sessizce
    /// "olanaklar yok" göstermeye başlar.
    /// </summary>
    [Fact]
    public async Task Places_Amenities_IsJsonEncodedText_NotAJsonObject()
    {
        Guid placeId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var place = await db.Places.FirstAsync(p => p.Name.Contains(Marker));
            placeId = place.Id;
            place.Amenities = """{"Otopark":true,"WC":true,"Wi-Fi":false}""";
            await db.SaveChangesAsync();
        }

        using var doc = await GetJsonAsync($"/v1/places/{placeId}");
        var amenities = doc.RootElement.GetProperty("data").GetProperty("amenities");

        amenities.ValueKind.Should().Be(JsonValueKind.String,
            "amenities istemciye JSON *metni* olarak geliyor, nesne olarak değil");

        // Metnin kendisi ayrıca ayrıştırılabilir olmalı (istemci böyle okuyor).
        using var inner = JsonDocument.Parse(amenities.GetString()!);
        inner.RootElement.GetProperty("Otopark").GetBoolean().Should().BeTrue();
        inner.RootElement.TryGetProperty("Klima", out _)
            .Should().BeFalse("anahtarda olmayan olanak \"yok\" değil, \"belirtilmemiş\" demektir");
    }

    // ------------------------------------- 6) "TR günü, 00:00 UTC" tarih konvansiyonu

    /// <summary>
    /// 📌 <c>dutyDate</c> / <c>eventDate</c> / <c>funeralDate</c> "**Türkiye günü, saat 00:00 UTC**"
    /// olarak yazılıyor; saat ayrı bir alanda taşınıyor. İstemci bu alanları **saat dilimine
    /// çevirmemeli** — çevirirse gün bir geri kayar. 11.7 / 11.10 / 11.11'de bu ders üç kez
    /// yeniden öğrenildi (ve 11.10 testleri bu yüzden yalnız geceleri kırılıyordu).
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Faz 0 denetiminin bulgusu (B7):</b> bu test 13 Ağustos 2026'ya kadar sözleşmenin
    /// saydığı <b>üç</b> alandan yalnız ikisini ölçüyordu — <c>funeralDate</c> hiç
    /// iddia edilmiyordu. Vefat, gün alanının en çok görüldüğü modül (ilan listesi ve detay
    /// aynı alanı basıyor) ve tam da 11.11'de bir kez kaymıştı. "Kilidin adı değil
    /// <b>kapsamı</b> önemlidir."
    /// </remarks>
    [Fact]
    public async Task DayOnlyDateFields_AreStoredAsMidnightUtc_WithTimeInASeparateField()
    {
        var day = new DateTime(2027, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        Guid eventId, scheduleId, pharmacyId, deathId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var categoryId = await db.EventCategories.Select(c => c.Id).FirstAsync();
            var ev = new Event
            {
                Id = Guid.NewGuid(),
                Title = "CLAUDE-TEST Görünmez Sözleşme Tarih",
                Description = "Gün alanı UTC gece yarısı olmalı.",
                CategoryId = categoryId,
                EventDate = day,
                EventTime = new TimeSpan(20, 30, 0),
                Status = "approved",
                IsFree = true,
                CreatedBy = Guid.Empty
            };
            db.Events.Add(ev);

            pharmacyId = await db.Pharmacies.Where(p => p.Name.Contains(Marker)).Select(p => p.Id).FirstAsync();
            var schedule = new PharmacySchedule
            {
                Id = Guid.NewGuid(),
                PharmacyId = pharmacyId,
                DutyDate = day,
                StartTime = new TimeSpan(19, 0, 0),
                EndTime = new TimeSpan(9, 0, 0),
                Source = "claude-test-11.14"
            };
            db.PharmacySchedules.Add(schedule);

            // B7: sözleşmenin üçüncü alanı — vefat ilanının cenaze günü.
            var death = new DeathNotice
            {
                Id = Guid.NewGuid(),
                DeceasedName = "CLAUDE-TEST Görünmez Sözleşme Merhum",
                FuneralDate = day,
                FuneralTime = new TimeSpan(13, 0, 0),
                Status = "approved",
                AddedBy = Guid.Empty
            };
            db.DeathNotices.Add(death);

            await db.SaveChangesAsync();
            eventId = ev.Id;
            scheduleId = schedule.Id;
            deathId = death.Id;
        }

        try
        {
            using (var doc = await GetJsonAsync($"/v1/events/{eventId}"))
            {
                var data = doc.RootElement.GetProperty("data");
                var raw = data.GetProperty("eventDate").GetString()!;
                DateTime.Parse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind)
                    .ToUniversalTime().TimeOfDay.Should().Be(TimeSpan.Zero,
                        "gün alanı UTC gece yarısı olmalı; saat ayrı alanda");
                data.GetProperty("eventTime").GetString().Should().StartWith("20:30",
                    "saat gün alanına GÖMÜLMEZ, ayrı alanda TimeSpan olarak gelir");
            }

            using (var doc = await GetJsonAsync($"/v1/pharmacies/on-duty?date=2027-03-15"))
            {
                var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();
                items.Should().NotBeEmpty("verilen gün için nöbet kaydı var");
                var raw = items[0].GetProperty("dutyDate").GetString()!;
                DateTime.Parse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind)
                    .ToUniversalTime().Should().Be(day, "nöbet günü kaydırılmadan aynen dönmeli");
            }

            // B7 — üçüncü alan: funeralDate. Saat AYRI alanda; gün UTC gece yarısı.
            using (var doc = await GetJsonAsync($"/v1/deaths/{deathId}"))
            {
                var data = doc.RootElement.GetProperty("data");
                var raw = data.GetProperty("funeralDate").GetString()!;
                DateTime.Parse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind)
                    .ToUniversalTime().Should().Be(day,
                        "cenaze günü kaydırılmadan aynen dönmeli; istemci saat dilimine çevirirse gün bir geri kayar");
                data.GetProperty("funeralTime").GetString().Should().StartWith("13:00",
                    "cenaze saati gün alanına GÖMÜLMEZ, ayrı alanda taşınır");
            }
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Events.Where(e => e.Id == eventId).ExecuteDeleteAsync();
            await db.PharmacySchedules.Where(s => s.Id == scheduleId).ExecuteDeleteAsync();
            await db.DeathNotices.IgnoreQueryFilters().Where(d => d.Id == deathId).ExecuteDeleteAsync();
        }
    }

    // ---------------------- 15 + 26) İlan listesinin iki sessiz kuralı (Faz 0 denetimi: B2/B3)

    /// <summary>
    /// 🔴 <b>Madde 26 (B2) — <c>?status=</c> public uçta ETKİSİZDİR.</b>
    ///
    /// <para>
    /// <c>QueryAdDto.Status</c> yalnız panel/admin yolunda okunur; public uç
    /// (<c>OnlyPublished=true</c>) onu yok sayar. Handler'daki <c>else if</c> bir gün
    /// <c>if</c>'e çevrilirse <c>GET /v1/ads?status=pending</c> **onaylanmamış ilanları
    /// iletişim telefonlarıyla** herkese açar — 10.5'te bir kez yaşandı.
    /// </para>
    ///
    /// <para>
    /// 🔑 <b>Bu test Faz 0 denetiminde doğdu.</b> Kural <c>PublicVisibilityTests</c> içinde
    /// <b>vefat</b> modülü için ölçülüydü; sözleşmenin adını taşıdığı <b>ilan</b> ucunda
    /// hiçbir iddia yoktu. Yani sızıntının gerçekten yaşandığı modül korumasızdı ve
    /// "testi var" cevabı doğruydu — <i>başka bir modülün testi</i>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task PublicAdsList_IgnoresTheStatusFilter_SoPendingAdsCanNeverLeak()
    {
        var categoryId = await InDbAsync(db => db.AdCategories.Where(c => c.ParentId == null).Select(c => c.Id).FirstAsync());
        var ownerId = await InDbAsync(db => db.Users.Select(u => u.Id).FirstAsync());
        var pendingId = Guid.NewGuid();

        await InDbAsync(async db =>
        {
            db.Ads.Add(new Ad
            {
                Id = pendingId,
                CategoryId = categoryId,
                UserId = ownerId,
                Title = $"{Marker} Onay bekleyen ilan",
                Description = "Bu ilan moderasyondan geçmedi.",
                ContactPhone = "+905331110099",
                Price = 1234,
                Status = "pending",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
            return 0;
        });

        try
        {
            foreach (var url in new[]
                     {
                         "/v1/ads?status=pending&limit=50",
                         "/v1/ads?status=rejected&limit=50",
                         "/v1/ads?limit=50"
                     })
            {
                using var doc = await GetJsonAsync(url);
                var ids = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                    .Select(i => i.GetProperty("id").GetGuid()).ToList();
                ids.Should().NotContain(pendingId,
                    $"public uç ({url}) moderasyondan geçmemiş ilanı ASLA döndürmemeli — " +
                    "istemcinin gönderdiği status süzgeci burada yok sayılır (§7 madde 26)");
            }
        }
        finally
        {
            await InDbAsync(async db =>
            {
                await db.Ads.IgnoreQueryFilters().Where(a => a.Id == pendingId).ExecuteDeleteAsync();
                return 0;
            });
        }
    }

    /// <summary>
    /// 🔴 <b>Madde 15 (B3) — kategori süzgeci TAM EŞLEŞMEDİR.</b>
    ///
    /// <para>
    /// Kök kategori, alt kategorilerindeki ilanları <b>getirmez</b>. Mobil kategori şeridi
    /// tam bu yüzden "içeri iniyor" (11.x): filtre hiyerarşik yapılsaydı şerit tasarımı
    /// gereksizleşir, üstelik kök seçimi bir anda çok daha büyük bir liste döndürürdü —
    /// hiçbir hata vermeden, yalnız ekran değişerek.
    /// </para>
    ///
    /// <para>
    /// 🔑 <b>Faz 0 denetiminin bulgusu:</b> bu semantiği ölçen tek bir test yoktu. En yakını
    /// <c>AdsMobileTests.Categories_ReturnSeededHierarchy_Anonymously</c>'ydi ve o
    /// <b>kategori ağacı ucunu</b> denetliyor, ilan süzgecini değil.
    /// </para>
    /// </summary>
    [Fact]
    public async Task AdCategoryFilter_IsAnExactMatch_NotAHierarchicalOne()
    {
        var (rootId, childId) = await InDbAsync(async db =>
        {
            var child = await db.AdCategories
                .Where(c => c.ParentId != null)
                .Select(c => new { c.Id, ParentId = c.ParentId!.Value })
                .FirstAsync();
            return (child.ParentId, child.Id);
        });
        var ownerId = await InDbAsync(db => db.Users.Select(u => u.Id).FirstAsync());
        var childAdId = Guid.NewGuid();

        await InDbAsync(async db =>
        {
            db.Ads.Add(new Ad
            {
                Id = childAdId,
                CategoryId = childId,
                UserId = ownerId,
                Title = $"{Marker} Alt kategori ilanı",
                Description = "Alt kategoride yayında.",
                ContactPhone = "+905331110098",
                Price = 4321,
                Status = "approved",
                ApprovedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
            return 0;
        });

        try
        {
            using (var doc = await GetJsonAsync($"/v1/ads?categoryId={childId}&limit=50"))
            {
                doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                    .Select(i => i.GetProperty("id").GetGuid())
                    .Should().Contain(childAdId, "kendi kategorisinde görünmeli");
            }

            using (var doc = await GetJsonAsync($"/v1/ads?categoryId={rootId}&limit=50"))
            {
                doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                    .Select(i => i.GetProperty("id").GetGuid())
                    .Should().NotContain(childAdId,
                        "kök kategori alt kategori ilanlarını GETİRMEZ (§7 madde 15) — " +
                        "mobil kategori şeridi bu yüzden hiyerarşide içeri iniyor");
            }
        }
        finally
        {
            await InDbAsync(async db =>
            {
                await db.Ads.IgnoreQueryFilters().Where(a => a.Id == childAdId).ExecuteDeleteAsync();
                return 0;
            });
        }
    }

    // ------------------- 21) Slug üretiminin TEK SAHİBİ (Faz 0 denetimi: B5)

    /// <summary>
    /// 🔴 <b>Madde 21 (B5) — slug üretiminin tek sahibi <c>SlugHelper</c>'dır ve
    /// SARMALAYICILARI ona delege eder.</b>
    ///
    /// <para>
    /// Projede iki sarmalayıcı var: <c>DbSeeder.Slugify</c> ve <c>BusinessRules.Slugify</c>.
    /// İkisi de bugün tek satırlık delegasyon — ama <b>hiçbir test bunu söylemiyordu</b>:
    /// <c>SlugAndPaginationTests</c> yalnız helper'ın <i>kendi</i> davranışını ölçüyor.
    /// Bir sarmalayıcıya "hızlıca" bir <c>ToLowerInvariant()</c> kopyası geri gelse
    /// 10.9–11.15b arasında yaşanan hata dirilirdi: Türkçe <c>'İ'</c> (U+0130)
    /// <c>ToLowerInvariant()</c> ile küçülmediği için slug'a ham girer ve
    /// <b>"İstasyon" ≠ "istasyon"</b> olur → aynı mahalle iki kayıt olarak yaşar.
    /// </para>
    ///
    /// <para>
    /// 🔑 İki ayak birden gerekli: (a) doğrudan çağrılabilen sarmalayıcı <b>aynı çıktıyı</b>
    /// vermeli, (b) seeder'ın <b>veritabanına yazdığı</b> satırlar helper'ın ürettiğiyle
    /// birebir aynı olmalı — ikincisi <c>DbSeeder.Slugify</c> <c>internal</c> olduğu için
    /// davranışla ölçülüyor (kaynak taraması değil: taramanın kapsamı da elle tutulan bir
    /// listedir, §7 madde 53).
    /// </para>
    /// </summary>
    [Fact]
    public async Task SlugGeneration_HasASingleOwner_EvenThroughItsWrappers()
    {
        // (a) Sarmalayıcı gerçekten delege ediyor mu — tuzağın tam merkezindeki girdilerle.
        foreach (var name in new[] { "İstasyon", "ĞÜŞİÖÇ", "Ilıca", "Çukurova Mahallesi", "Şehit Öğretmen" })
        {
            KadirliApp.Application.Features.Businesses.Commands.BusinessRules.Slugify(name)
                .Should().Be(SlugHelper.Slugify(name),
                    "slug üretiminin tek sahibi SlugHelper'dır; sarmalayıcı kendi gerçeklemesini yazamaz " +
                    "(§7 madde 21 — 'İ' ToLowerInvariant ile küçülmez ve mükerrer kayıt doğar)");
        }

        // (b) Seeder'ın DB'ye yazdığı satırlar da aynı kuraldan geçmiş olmalı.
        // ⚠️ GuideCategories bilerek DIŞARIDA: orada slug'ı yönetici elle verebiliyor
        // (`CreateGuideCategoryCommand.Slug`), yani eşitsizlik hata değil veridir.
        var mismatches = await InDbAsync(async db =>
        {
            var rows = new List<(string Table, string Name, string Slug)>();
            rows.AddRange((await db.Neighborhoods.Select(x => new { x.Name, x.Slug }).ToListAsync())
                .Select(x => ("neighborhoods", x.Name, x.Slug)));
            rows.AddRange((await db.AnnouncementTypes.Select(x => new { x.Name, x.Slug }).ToListAsync())
                .Select(x => ("announcement_types", x.Name, x.Slug)));
            rows.AddRange((await db.EventCategories.Select(x => new { x.Name, x.Slug }).ToListAsync())
                .Select(x => ("event_categories", x.Name, x.Slug)));
            rows.AddRange((await db.PlaceCategories.Select(x => new { x.Name, x.Slug }).ToListAsync())
                .Select(x => ("place_categories", x.Name, x.Slug)));
            rows.AddRange((await db.BusinessCategories.Select(x => new { x.Name, x.Slug }).ToListAsync())
                .Select(x => ("business_categories", x.Name, x.Slug)));

            return rows
                .Where(r => r.Slug != SlugHelper.Slugify(r.Name))
                .Select(r => $"{r.Table}: \"{r.Name}\" → \"{r.Slug}\" (beklenen \"{SlugHelper.Slugify(r.Name)}\")")
                .ToList();
        });

        mismatches.Should().BeEmpty(
            "seed'lenen kayıtla panelden eklenen kayıt AYNI slug kuralından geçmeli; " +
            "ayrıştıkları an aynı ad iki farklı slug alır ve fark hiçbir yerde görünmez");
    }

    // ------------------------------------------- 7) Ulaşım saatleri: tarihsiz duvar saati

    /// <summary>
    /// 📌 Ulaşım kalkış saatleri **tarihsiz "duvar saati"** ve **iki farklı biçimde** geliyor:
    /// şehirlerarası <c>"07:00"</c> (düz metin), şehir içi <c>"06:30:00"</c> (TimeSpan
    /// serileştirmesi). Mobilin "sıradaki kalkış" hesabı (11.12) ikisini de tek çözümleyiciyle
    /// okuyor ve **saat dilimi kaydırması yapmıyor**.
    /// </summary>
    [Fact]
    public async Task TransportDepartureTimes_AreDatelessWallClock_InTwoDifferentFormats()
    {
        using (var doc = await GetJsonAsync("/v1/transport/intercity-routes"))
        {
            var schedules = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .SelectMany(r => r.GetProperty("schedules").EnumerateArray()).ToList();
            schedules.Should().NotBeEmpty();
            foreach (var s in schedules)
                s.GetProperty("departureTime").GetString().Should().MatchRegex(@"^\d{2}:\d{2}$",
                    "şehirlerarası saatler HH:mm metni — tarih taşımaz");
        }

        using (var doc = await GetJsonAsync("/v1/transport/intracity-routes"))
        {
            var routes = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
            routes.Should().NotBeEmpty();
            foreach (var r in routes)
            {
                r.GetProperty("firstDeparture").GetString().Should().MatchRegex(@"^\d{2}:\d{2}:\d{2}$",
                    "şehir içi saatler TimeSpan → HH:mm:ss (şehirlerarasından FARKLI biçim)");
                r.GetProperty("lastDeparture").GetString().Should().MatchRegex(@"^\d{2}:\d{2}:\d{2}$");
            }
        }
    }

    // --------------------------------------------------- 8) Yol biçimi: kebab-case kanonik

    /// <summary>
    /// 📌 Faz 10.13'te tüm yollar <c>SlugifyParameterTransformer</c> ile kebab-case'e çevrildi.
    /// Eski PascalCase yollar **404** veriyor. Yeni bir controller eklenirken bu dönüşümün
    /// çalıştığı varsayılıyor — kapatılırsa mobilin tüm çok kelimeli uçları kırılır.
    /// </summary>
    [Fact]
    public async Task RoutePaths_AreKebabCase_AndPascalCaseIs404()
    {
        (await _client.GetAsync("/v1/power-outages")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.GetAsync("/v1/PowerOutages")).StatusCode.Should().Be(HttpStatusCode.NotFound,
            "eski PascalCase yol bilinçli olarak yaşatılmıyor");
    }

    // ------------------------------------------------- 9) Görsel URL'leri GÖRELİ gelir

    /// <summary>
    /// 📌 Görsel URL'leri **göreli** (<c>/uploads/…</c>) dönüyor; origin'i istemci ekliyor
    /// (mobilde <c>AppImage.url</c>). Sunucu mutlak URL döndürmeye başlarsa istemci
    /// <c>http://10.0.2.2:5005http://…</c> gibi bozuk adresler üretir.
    /// </summary>
    [Fact]
    public async Task ImageUrls_AreReturnedRelative_SoTheClientPrependsOrigin()
    {
        var token = await GetUserTokenAsync("+905077770002", "claudetest_ic_file");

        using var content = new MultipartFormDataContent();
        // 1x1 PNG (magic-byte doğrulaması gerçek PNG başlığı istiyor — 10.1).
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "contract.png");
        content.Add(new StringContent("ad"), "moduleType");

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/files/upload") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var url = doc.RootElement.GetProperty("data").GetProperty("cdnUrl").GetString()!;
        url.Should().StartWith("/uploads/", "URL göreli olmalı — origin'i istemci ekler");
        url.Should().NotStartWith("http", "mutlak URL istemcide çift-origin'e yol açar");
    }

    // ----------------------------------------- 10) Zarf + meta her yanıtta dolu

    /// <summary>
    /// 📌 Her yanıt <c>{success, data, meta}</c> zarfıyla sarılı ve <c>meta</c> **her zaman**
    /// <c>timestamp/path/traceId</c> taşıyor (10.13'te kendi kendini saran filtre eklendi).
    /// Mobilin hata ekranı traceId'yi gösteriyor; boş kalırsa destek "hangi istek?" diye soramaz.
    /// </summary>
    [Fact]
    public async Task EveryResponse_IsEnvelopedAndCarriesMeta()
    {
        foreach (var url in new[] { "/v1/neighborhoods", "/v1/ads?page=1&limit=1", "/v1/power-outages" })
        {
            using var doc = await GetJsonAsync(url);
            var root = doc.RootElement;
            root.GetProperty("success").GetBoolean().Should().BeTrue(url);
            root.TryGetProperty("data", out _).Should().BeTrue(url);

            var meta = root.GetProperty("meta");
            meta.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty(url);
            meta.GetProperty("path").GetString().Should().StartWith("/v1/", url);
            meta.GetProperty("timestamp").GetString().Should().NotBeNullOrEmpty(url);
        }
    }

    // ------------------------------------ 11) Şikayet türü sunucuda SERBEST METİN

    /// <summary>
    /// 📌 <c>complaints.type</c> sunucuda **doğrulanmıyor** — sözlük ucu da yok. Mobil 6 tür
    /// tanımlıyor ve tanımadığı değeri ham gösteriyor (11.12). Sunucuya bir gün doğrulayıcı
    /// eklenirse eski sürümdeki istemciler 400 almaya başlar; o değişiklik bilinçli olmalı.
    /// </summary>
    [Fact]
    public async Task ComplaintType_IsFreeText_ServerDoesNotValidateIt()
    {
        var response = await _client.PostAsJsonAsync("/v1/complaints", new
        {
            type = "gelecekte-eklenecek-tur",
            subject = "CLAUDE-TEST Görünmez Sözleşme",
            message = "Şikayet türü sunucuda serbest metindir."
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "tür doğrulanmıyor");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = doc.RootElement.GetProperty("data").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            var saved = await db.Complaints.AsNoTracking().FirstAsync(c => c.Id == id);
            saved.Type.Should().Be("gelecekte-eklenecek-tur", "tanınmayan tür ham hâliyle saklanır");
            saved.UserId.Should().BeNull("anonim şikayet user_id taşımaz → \"Bildirimlerim\"de görünmez");
        }
        finally
        {
            await db.Complaints.Where(c => c.Id == id).ExecuteDeleteAsync();
        }
    }

    // ------------------------- 12) UpdateMyAd görsel sırasını/kapağını bilmiyor

    /// <summary>
    /// 📌 <c>UpdateMyAdCommand</c> görsel **sırası** ve **kapak** kavramını bilmiyor: yalnız
    /// <c>newImageFileIds</c> (sona ekle, <c>isCover=false</c>) ve <c>removeImageIds</c> var.
    /// Kapak ancak "hiç kapak kalmadıysa en düşük sıradakine" veriliyor. Mobil (11.9) bu yüzden
    /// kullanıcı kapağı değiştirdiğinde mevcut görselleri **silip yeni sırada yeniden bağlıyor**.
    /// Uç bir gün sıra/kapak parametresi kabul ederse o hile kaldırılabilir.
    /// </summary>
    [Fact]
    public async Task UpdateMyAd_AppendsNewImagesAsNonCover_AndHasNoOrderingOrCoverParameter()
    {
        var token = await GetUserTokenAsync("+905077770003", "claudetest_ic_img");
        var fileA = await UploadFileAsync(token, "a.png");
        var fileB = await UploadFileAsync(token, "b.png");
        var adId = await CreatePublishedAdAsync(token, "CLAUDE-TEST Görsel Sırası", new[] { fileA });

        // Komutta "B kapak olsun" ya da "B başa gelsin" diyebilecek bir alan YOK —
        // sözleşmenin kendisi burada: gövdeye yazılabilecek tek şey ekle/çıkar.
        typeof(KadirliApp.Application.Features.Ads.Commands.UpdateMyAd.UpdateMyAdCommand)
            .GetProperties().Select(p => p.Name)
            .Should().NotContain(new[] { "CoverImageId", "CoverFileId", "ImageOrder", "ImageFileIds" },
                "sıra/kapak alanı eklenirse mobildeki yeniden-bağlama hilesi kaldırılmalı");

        var update = await _client.SendAsync(Authorized(HttpMethod.Put, $"/v1/ads/{adId}", token, new
        {
            title = "CLAUDE-TEST Görsel Sırası 2",
            description = "Görsel eklendi.",
            price = 1000,
            contactPhone = "+905331112233",
            newImageFileIds = new[] { fileB }
        }));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var images = await db.AdImages.AsNoTracking().Where(i => i.AdId == adId)
            .OrderBy(i => i.DisplayOrder).ToListAsync();

        images.Should().HaveCount(2);
        images[0].FileId.Should().Be(fileA);
        images[0].IsCover.Should().BeTrue("ilk görsel kapak kalır");
        images[1].FileId.Should().Be(fileB);
        images[1].IsCover.Should().BeFalse("sonradan eklenen görsel kapak OLMAZ");
    }

    // ------------------------------------------------------------------- yardımcılar

    private async Task<Guid> UploadFileAsync(string token, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", fileName);
        content.Add(new StringContent("ad"), "moduleType");

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/files/upload") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreatePublishedAdAsync(string ownerToken, string title, Guid[]? imageFileIds = null)
    {
        Guid categoryId;
        using (var doc = await GetJsonAsync("/v1/ads/categories"))
            categoryId = doc.RootElement.GetProperty("data").EnumerateArray()
                .First(x => x.GetProperty("subCategoryCount").GetInt32() == 0).GetProperty("id").GetGuid();

        var create = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/ads", ownerToken, new
        {
            categoryId,
            title,
            description = "Faz 11.14 görünmez sözleşme testi.",
            price = 1000,
            contactPhone = "+905331112233",
            imageFileIds = imageFileIds ?? Array.Empty<Guid>()
        }));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid adId;
        using (var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync()))
            adId = doc.RootElement.GetProperty("data").GetGuid();

        const string adminPhone = "+905000000001";
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone = adminPhone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone = adminPhone, otp = "123456" });
        using var adminDoc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        var adminToken = adminDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/ads/{adId}/approve", adminToken)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        return adId;
    }
}
