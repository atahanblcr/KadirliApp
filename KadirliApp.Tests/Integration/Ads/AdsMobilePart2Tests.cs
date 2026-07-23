using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KadirliApp.Infrastructure.Jobs;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KadirliApp.Tests.Integration.Ads;

/// <summary>
/// Faz 10.6 doğrulaması: kendi ilanını güncelleme/silme (sahiplik izolasyonu), favoriler (idempotent),
/// süre uzatma (ExpireAdsJob etkileşimi dahil), me-scoped listeler ve iletişim tıklama sayaçları.
/// </summary>
public class AdsMobilePart2Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdsMobilePart2Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private async Task<string> GetAdminTokenAsync()
    {
        const string phone = "+905000000001";
        (await _client.PostAsJsonAsync("/v1/auth/login", new { phone })).StatusCode.Should().Be(HttpStatusCode.OK);
        var verify = await _client.PostAsJsonAsync("/v1/auth/verify-otp", new { phone, otp = "123456" });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await verify.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
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
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        using var regDoc = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        return regDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    /// <summary>Property'siz bir kök kategoriyle ilan oluşturur; approve=true ise admin onayından geçirir.</summary>
    private async Task<Guid> CreateAdAsync(string ownerToken, string title, bool approve)
    {
        Guid categoryId;
        using (var doc = JsonDocument.Parse(await (await _client.GetAsync("/v1/ads/categories")).Content.ReadAsStringAsync()))
            categoryId = doc.RootElement.GetProperty("data").EnumerateArray()
                .First(x => x.GetProperty("subCategoryCount").GetInt32() == 0).GetProperty("id").GetGuid();

        var create = await _client.SendAsync(Authorized(HttpMethod.Post, "/v1/ads", ownerToken, new
        {
            categoryId,
            title,
            description = "Faz 10.6 test ilanı.",
            price = 1000,
            contactPhone = "+905331112233"
        }));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid adId;
        using (var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync()))
            adId = doc.RootElement.GetProperty("data").GetGuid();

        if (approve)
        {
            var adminToken = await GetAdminTokenAsync();
            (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/admin/ads/{adId}/approve", adminToken)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }
        return adId;
    }

    [Fact]
    public async Task UpdateAndDelete_EnforceOwnership_And_UpdateResetsToPending()
    {
        var ownerToken = await GetUserTokenAsync("+905055560001", "claudetest_p2owner");
        var otherToken = await GetUserTokenAsync("+905055560002", "claudetest_p2other");
        var adId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Sahiplik", approve: true);

        var updateBody = new { title = "CLAUDE-TEST P2 Güncellendi", description = "Yeni açıklama.", price = 2000, contactPhone = "+905331112234" };

        // Token'sız → 401; başka kullanıcı → 403 (update + delete)
        (await _client.PutAsJsonAsync($"/v1/ads/{adId}", updateBody)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var forbiddenPut = await _client.SendAsync(Authorized(HttpMethod.Put, $"/v1/ads/{adId}", otherToken, updateBody));
        forbiddenPut.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var doc = JsonDocument.Parse(await forbiddenPut.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("FORBIDDEN");
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/ads/{adId}", otherToken)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Sahibi günceller → alanlar değişir + yeniden moderasyon (pending, onay izleri temiz)
        (await _client.SendAsync(Authorized(HttpMethod.Put, $"/v1/ads/{adId}", ownerToken, updateBody)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ad = await db.Ads.FirstAsync(a => a.Id == adId);
            ad.Title.Should().Be("CLAUDE-TEST P2 Güncellendi");
            ad.Price.Should().Be(2000);
            ad.Status.Should().Be("pending", "kullanıcı düzenlemesi yeniden onaya düşmeli");
            ad.ApprovedBy.Should().BeNull();
            ad.ApprovedAt.Should().BeNull();
        }

        // Geçersiz içerik → 400 (paylaşılan AdSubmissionRules çalışıyor)
        (await _client.SendAsync(Authorized(HttpMethod.Put, $"/v1/ads/{adId}", ownerToken,
                new { title = "ab", description = "x", contactPhone = "+905331112234" })))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Sahibi siler → soft delete; detay artık sahibine bile 404
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/ads/{adId}", ownerToken)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ad = await db.Ads.IgnoreQueryFilters().FirstAsync(a => a.Id == adId);
            ad.DeletedAt.Should().NotBeNull();
        }
        (await _client.SendAsync(Authorized(HttpMethod.Get, $"/v1/ads/{adId}", ownerToken)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        // Silinmiş ilana tekrar işlem → 404 (soft-delete query filter)
        (await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/ads/{adId}", ownerToken)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Favorites_AddRemoveList_AreIdempotent_And_HiddenAdsNotFavoritable()
    {
        var ownerToken = await GetUserTokenAsync("+905055560003", "claudetest_p2fav1");
        var favToken = await GetUserTokenAsync("+905055560004", "claudetest_p2fav2");
        var approvedId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Favori", approve: true);
        var pendingId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Favori Pending", approve: false);

        // Token'sız → 401; pending ilan favorilenemez → 404 (varlık sızdırılmaz)
        (await _client.PostAsync($"/v1/ads/{approvedId}/favorite", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{pendingId}/favorite", favToken)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Ekle → data=true; ikinci ekleme idempotent → 200 + data=false
        var add = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{approvedId}/favorite", favToken));
        add.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await add.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetBoolean().Should().BeTrue();
        var addAgain = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{approvedId}/favorite", favToken));
        addAgain.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await addAgain.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetBoolean().Should().BeFalse("ikinci favori isteği idempotent olmalı");

        // DB'de tek satır; me/favorites listesinde görünür
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.AdFavorites.CountAsync(f => f.AdId == approvedId)).Should().Be(1);
        }
        var list = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/favorites", favToken));
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
        {
            var items = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
            items.Should().ContainSingle(i => i.GetProperty("adId").GetGuid() == approvedId);
            var row = items.Single(i => i.GetProperty("adId").GetGuid() == approvedId);
            row.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
            row.GetProperty("title").GetString().Should().Be("CLAUDE-TEST P2 Favori");
        }

        // Sahibin me/ads satırında favoriteCount=1 görünür
        var myAds = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/ads?status=approved", ownerToken));
        using (var doc = JsonDocument.Parse(await myAds.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Single(i => i.GetProperty("id").GetGuid() == approvedId)
                .GetProperty("favoriteCount").GetInt32().Should().Be(1);

        // Çıkar → data=true; tekrar çıkar → data=false; liste boşalır
        var remove = await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/ads/{approvedId}/favorite", favToken));
        remove.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await remove.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetBoolean().Should().BeTrue();
        var removeAgain = await _client.SendAsync(Authorized(HttpMethod.Delete, $"/v1/ads/{approvedId}/favorite", favToken));
        using (var doc = JsonDocument.Parse(await removeAgain.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetBoolean().Should().BeFalse();
        using (var doc = JsonDocument.Parse(await (await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/favorites", favToken))).Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task MyAds_ListsAllStatuses_FiltersByStatus_And_RejectsInvalidFilter()
    {
        var token = await GetUserTokenAsync("+905055560005", "claudetest_p2myads");
        var approvedId = await CreateAdAsync(token, "CLAUDE-TEST P2 MyAds Onaylı", approve: true);
        var pendingId = await CreateAdAsync(token, "CLAUDE-TEST P2 MyAds Bekleyen", approve: false);

        // Token'sız → 401
        (await _client.GetAsync("/v1/users/me/ads")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Filtresiz: iki ilan da (pending dahil) sahibine görünür
        var all = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/ads", token));
        all.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await all.Content.ReadAsStringAsync()))
        {
            var items = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
            items.Select(i => i.GetProperty("id").GetGuid()).Should().Contain(new[] { approvedId, pendingId });
            var pendingRow = items.Single(i => i.GetProperty("id").GetGuid() == pendingId);
            pendingRow.GetProperty("status").GetString().Should().Be("pending");
            pendingRow.GetProperty("categoryName").GetString().Should().NotBeNullOrEmpty();
            pendingRow.GetProperty("maxExtensions").GetInt32().Should().BeGreaterThan(0);
        }

        // status=pending filtresi yalnız bekleyeni döner
        using (var doc = JsonDocument.Parse(await (await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/ads?status=pending", token))).Content.ReadAsStringAsync()))
        {
            var ids = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("id").GetGuid()).ToList();
            ids.Should().Contain(pendingId).And.NotContain(approvedId);
        }

        // Başka kullanıcı kendi listesinde bu ilanları görmez (izolasyon)
        var otherToken = await GetUserTokenAsync("+905055560006", "claudetest_p2myads2");
        using (var doc = JsonDocument.Parse(await (await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/ads", otherToken))).Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("data").GetProperty("totalCount").GetInt32().Should().Be(0);

        // Geçersiz status → 400 VALIDATION_ERROR
        var invalid = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/ads?status=bozuk", token));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using (var doc = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Extend_ProlongsExpiry_ReactivatesExpired_EnforcesLimit_And_JobRespectsExtension()
    {
        var ownerToken = await GetUserTokenAsync("+905055560007", "claudetest_p2ext");
        var otherToken = await GetUserTokenAsync("+905055560008", "claudetest_p2ext2");
        var adId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Uzatma", approve: true);
        var pendingId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Uzatma Pending", approve: false);

        // Sahiplik: başkası uzatamaz → 403; pending uzatılamaz → 400
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{adId}/extend", otherToken)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{pendingId}/extend", ownerToken)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // İlanın süresini geçmişe çek → ExpireAdsJob onu expired yapar
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Ads.Where(a => a.Id == adId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.ExpiresAt, DateTime.UtcNow.AddDays(-1)));
            await new ExpireAdsJob(db, NullLogger<ExpireAdsJob>.Instance).RunAsync();
            (await db.Ads.AsNoTracking().FirstAsync(a => a.Id == adId)).Status.Should().Be("expired");
        }

        // Süresi dolmuş ilanı uzat → yeniden approved + bitiş ~30 gün sonrası + ad_extensions satırı
        var extend = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{adId}/extend", ownerToken, new { adsWatched = 2 }));
        extend.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await extend.Content.ReadAsStringAsync()))
        {
            var data = doc.RootElement.GetProperty("data");
            data.GetProperty("status").GetString().Should().Be("approved", "süresi dolmuş ilan uzatmayla yeniden yayına girmeli");
            data.GetProperty("extensionCount").GetInt32().Should().Be(1);
            data.GetProperty("remainingExtensions").GetInt32().Should().Be(2);
        }
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ad = await db.Ads.AsNoTracking().FirstAsync(a => a.Id == adId);
            ad.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(5));
            var ext = await db.AdExtensions.SingleAsync(e => e.AdId == adId);
            ext.AdsWatched.Should().Be(2);
            ext.DaysExtended.Should().Be(30);

            // Job tekrar koşar → uzatılmış ilan expired YAPILMAZ
            await new ExpireAdsJob(db, NullLogger<ExpireAdsJob>.Instance).RunAsync();
            (await db.Ads.AsNoTracking().FirstAsync(a => a.Id == adId)).Status.Should().Be("approved");
        }

        // Yayındaki ilanda uzatma bitişe EKLENİR (bugüne değil) — 2. uzatma sonrası ~60 gün
        var extend2 = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{adId}/extend", ownerToken));
        extend2.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Ads.AsNoTracking().FirstAsync(a => a.Id == adId)).ExpiresAt
                .Should().BeCloseTo(DateTime.UtcNow.AddDays(60), TimeSpan.FromMinutes(5));
        }

        // 3. uzatma OK (hak 3), 4.sü → 409 CONFLICT
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{adId}/extend", ownerToken)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var overLimit = await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{adId}/extend", ownerToken));
        overLimit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var doc = JsonDocument.Parse(await overLimit.Content.ReadAsStringAsync()))
            doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("CONFLICT");
    }

    [Fact]
    public async Task TrackCounters_IncrementAnonymously_OnlyForPublishedAds()
    {
        var ownerToken = await GetUserTokenAsync("+905055560009", "claudetest_p2track");
        var adId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Sayaç", approve: true);
        var pendingId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Sayaç Pending", approve: false);

        // Anonim: telefon ×2, whatsapp ×1
        (await _client.PostAsync($"/v1/ads/{adId}/track-phone", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsync($"/v1/ads/{adId}/track-phone", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsync($"/v1/ads/{adId}/track-whatsapp", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Yayında olmayan / olmayan ilan → 404
        (await _client.PostAsync($"/v1/ads/{pendingId}/track-phone", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.PostAsync($"/v1/ads/{Guid.NewGuid()}/track-whatsapp", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ad = await db.Ads.AsNoTracking().FirstAsync(a => a.Id == adId);
            ad.PhoneClickCount.Should().Be(2);
            ad.WhatsappClickCount.Should().Be(1);
        }

        // Sahibin me/ads satırında sayaçlar görünür
        var myAds = await _client.SendAsync(Authorized(HttpMethod.Get, "/v1/users/me/ads", ownerToken));
        using (var doc = JsonDocument.Parse(await myAds.Content.ReadAsStringAsync()))
        {
            var row = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray()
                .Single(i => i.GetProperty("id").GetGuid() == adId);
            row.GetProperty("phoneClickCount").GetInt32().Should().Be(2);
            row.GetProperty("whatsappClickCount").GetInt32().Should().Be(1);
        }
    }

    /// <summary>
    /// Süresi geçmiş ama ExpireAdsJob'un henüz expired'a çevirmediği (status hâlâ approved) ilan,
    /// listeyle aynı kuralla detay/track/favorite'ta da gizlenmeli — job saatlik koştuğundan bu
    /// pencerede ilan detaydan sızıyordu.
    /// </summary>
    [Fact]
    public async Task DateExpiredButStillApprovedAd_IsHiddenFromDetailTrackAndFavorite()
    {
        var ownerToken = await GetUserTokenAsync("+905055560010", "claudetest_p2exp");
        var otherToken = await GetUserTokenAsync("+905055560011", "claudetest_p2exp2");
        var adId = await CreateAdAsync(ownerToken, "CLAUDE-TEST P2 Süresi Geçmiş", approve: true);

        // Yayındayken herkes görür
        (await _client.GetAsync($"/v1/ads/{adId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Süreyi geçmişe çek ama job'ı KOŞTURMA — status approved kalır
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Ads.Where(a => a.Id == adId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.ExpiresAt, DateTime.UtcNow.AddDays(-1)));
            (await db.Ads.AsNoTracking().FirstAsync(a => a.Id == adId)).Status.Should().Be("approved");
        }

        // Anonim detay 404, sahibi görmeye devam eder
        (await _client.GetAsync($"/v1/ads/{adId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.SendAsync(Authorized(HttpMethod.Get, $"/v1/ads/{adId}", ownerToken)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Sayaç ve favori de aynı görünürlük kuralına uyar
        (await _client.PostAsync($"/v1/ads/{adId}/track-phone", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.SendAsync(Authorized(HttpMethod.Post, $"/v1/ads/{adId}/favorite", otherToken)))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
