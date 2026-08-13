using KadirliApp.Infrastructure.Identity.Social;
using KadirliApp.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KadirliApp.Tests.Integration.Auth;

/// <summary>
/// Faz 12.7 — sosyal girişin uçtan uca koştuğu factory.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Sahte olan tek şey ANAHTAR SUNUCUSU.</b> <c>ISocialTokenVerifier</c>'ın kendisi
/// <b>gerçek</b> gerçekleme — yani bu testlerde imza, <c>iss</c>, <b><c>aud</c></b>, süre ve
/// algoritma kontrolleri hepsi <b>gerçekten koşuyor</b>. Doğrulayıcıyı sahtelemek kolay
/// olurdu ama o zaman testler yalnız kendi akışımızı denerdi ve fazın bir numaralı kuralı
/// (§7 madde 68) uçtan uca hiç kanıtlanmamış olurdu.
/// </para>
/// <para>
/// ⚠️ Google <b>açık</b>, Apple <b>KAPALI</b> bırakıldı — böylece "sağlayıcı kapalıyken uç
/// ne diyor?" sorusu da aynı süitte cevaplanıyor (12.8'de Apple aboneliği gelene kadar
/// canlıdaki durum bu olacak).
/// </para>
/// </remarks>
public class SocialLoginWebApplicationFactory : CustomWebApplicationFactory
{
    protected override IDictionary<string, string?> ExtraConfiguration => new Dictionary<string, string?>
    {
        ["Auth:Social:Enabled"] = "true",
        ["Auth:Social:Google:ClientIds"] = SocialTokenTestKit.OurGoogleClientId
        // Apple bilerek yok → kapalı sağlayıcı dalı da test ediliyor.
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IJsonWebKeySetProvider>();
            services.AddSingleton<IJsonWebKeySetProvider>(new FakeJsonWebKeySetProvider());
        });
    }
}
