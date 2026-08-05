using FluentAssertions;
using KadirliApp.Api.Controllers.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Security;

/// <summary>
/// Faz 11.14 — **Yapısal** yetki denetimi. <see cref="PublicEndpointAuthorizationTests"/> elle
/// yazılmış bir uç listesini deniyor; o liste yeni uç eklendiğinde güncellenmezse boşluk
/// görünmez kalıyor. Buradaki testler ASP.NET'in <see cref="EndpointDataSource"/>'undan
/// **gerçekte kayıtlı tüm uçları** okuyup kuralı topluca uyguluyor → yeni bir controller
/// eklendiğinde test kendiliğinden onu da kapsıyor, kimsenin listeyi güncellemesi gerekmiyor.
///
/// 📌 Kural: <c>/v1/admin/*</c> altındaki HİÇBİR uç anonim erişilebilir olamaz; public
/// yazma uçlarının anonim olanları ise bilinçli ve sayılıdır.
/// </summary>
public class EndpointAuthorizationSweepTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EndpointAuthorizationSweepTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Sunucunun ayağa kalkması (ve uç tablosunun kurulması) için bir istemci oluşturulmalı.
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private sealed record EndpointInfo(string Route, string Methods, bool AllowsAnonymous, bool HasAuthorize);

    private IReadOnlyList<EndpointInfo> AllEndpoints()
    {
        var source = _factory.Services.GetRequiredService<EndpointDataSource>();
        return source.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => new EndpointInfo(
                e.RoutePattern.RawText ?? string.Empty,
                string.Join(",", e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? new List<string>()),
                e.Metadata.GetMetadata<IAllowAnonymous>() is not null,
                e.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0))
            .ToList();
    }

    [Fact]
    public void EndpointTable_IsDiscoverable_AndNotEmpty()
    {
        // Bu testin kendisi diğerlerinin sessizce "0 uç denetledim" demesini engeller.
        AllEndpoints().Should().HaveCountGreaterThan(50, "uygulama 60+ uç barındırıyor");
    }

    /// <summary>
    /// Yeni bir admin controller'ı <see cref="AdminApiControllerBase"/>'den türemeyi unutur
    /// ya da üstüne <c>[AllowAnonymous]</c> düşerse panel uçları internete açılır.
    /// </summary>
    [Fact]
    public void EveryAdminEndpoint_RequiresAuthorization()
    {
        var adminEndpoints = AllEndpoints()
            .Where(e => e.Route.StartsWith("v1/admin/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        adminEndpoints.Should().NotBeEmpty("panel uçları kayıtlı olmalı");

        var unprotected = adminEndpoints.Where(e => e.AllowsAnonymous || !e.HasAuthorize).ToList();
        unprotected.Should().BeEmpty(
            "her v1/admin/* ucu yetki istemeli; korumasız bulunanlar: {0}",
            string.Join(" | ", unprotected.Select(e => $"{e.Methods} /{e.Route}")));
    }

    /// <summary>
    /// Anonim **yazma** uçları bilinçli bir listedir (10.7 kararı: şikayet bildirimi ve ilan
    /// sayaçları hesap istemez, yoksa kimse bildirmez). Listeye izinsiz yeni bir uç eklenirse
    /// bu test onu yakalar — "yanlışlıkla anonim bıraktım" senaryosu.
    /// </summary>
    [Fact]
    public void AnonymousWriteEndpoints_AreExactlyTheDeliberateOnes()
    {
        var writeMethods = new[] { "POST", "PUT", "PATCH", "DELETE" };

        var anonymousWrites = AllEndpoints()
            .Where(e => e.Route.StartsWith("v1/", StringComparison.OrdinalIgnoreCase))
            .Where(e => writeMethods.Any(m => e.Methods.Contains(m, StringComparison.OrdinalIgnoreCase)))
            .Where(e => e.AllowsAnonymous || !e.HasAuthorize)
            .Select(e => $"{e.Methods} /{e.Route}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var expected = new[]
        {
            "POST /v1/announcements/{id:guid}/click",   // görüntülenme/tıklama sayacı
            "POST /v1/announcements/{id:guid}/view",
            "POST /v1/ads/{id:guid}/track-phone",       // ilan iletişim sayaçları
            "POST /v1/ads/{id:guid}/track-whatsapp",
            "POST /v1/auth/login",                      // oturum açma akışı
            "POST /v1/auth/refresh",
            "POST /v1/auth/register",
            "POST /v1/auth/verify-otp",
            "POST /v1/complaints",                      // 10.7: hesapsız şikayet bildirimi
            // Faz 12.1: mobil hata/çökme bildirimi. Anonim olması BİLİNÇLİ — çökme çoğu
            // zaman oturum açılmadan önce olur (açılış ekranı, giriş akışı) ve [Authorize]
            // konsaydı raporlanamayan hatalar tam da en kritik olanlar olurdu.
            // Bedeli spam yüzeyi; karşılığı public-write hız sınırı + gövde tavanları +
            // parmak izi tekilleştirmesi (aynı yalan bin kez gönderilse tabloda tek satır).
            "POST /v1/client-errors"
        }.OrderBy(x => x, StringComparer.Ordinal).ToList();

        anonymousWrites.Should().BeEquivalentTo(expected,
            "anonim yazma uçları sayılı ve bilinçlidir; bu liste değiştiyse karar açıkça verilmiş olmalı");
    }

    /// <summary>
    /// <c>/v1/users/me*</c> uçları oturum sahibinin kendi verisine bakar; anonime açılırsa
    /// kimlik doğrulamasız profil/ilan/favori okuması demek olur.
    /// </summary>
    [Fact]
    public void MeScopedEndpoints_AllRequireAuthorization()
    {
        var meEndpoints = AllEndpoints()
            .Where(e => e.Route.StartsWith("v1/users/me", StringComparison.OrdinalIgnoreCase))
            .ToList();

        meEndpoints.Should().NotBeEmpty();
        meEndpoints.Should().OnlyContain(e => e.HasAuthorize && !e.AllowsAnonymous);
    }

    /// <summary>
    /// Panel controller'larının tamamı ortak tabandan türemeli — taban <c>[Authorize(Policy =
    /// "AdminPanel")]</c> taşıyor. Türemeyen bir sınıf yetki niteliğini elle yazmayı unutabilir.
    /// </summary>
    [Fact]
    public void EveryAdminController_DerivesFromAdminApiControllerBase()
    {
        var controllers = typeof(AdminApiControllerBase).Assembly.GetTypes()
            .Where(t => t.Namespace == "KadirliApp.Api.Controllers.Admin")
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        controllers.Should().NotBeEmpty("panel controller'ları bulunmalı — yoksa test hiçbir şey denetlemiyor demektir");

        var offenders = controllers
            .Where(t => !typeof(AdminApiControllerBase).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        offenders.Should().BeEmpty(
            "Admin klasöründeki her controller AdminApiControllerBase'den türemeli; türemeyenler: {0}",
            string.Join(", ", offenders));
    }
}
