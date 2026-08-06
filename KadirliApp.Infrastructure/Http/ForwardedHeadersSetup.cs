using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ⚠️ .NET 8 ile `System.Net.IPNetwork` doğdu ve `Microsoft.AspNetCore.HttpOverrides.IPNetwork`
// ile ADI ÇAKIŞIYOR. `KnownNetworks` listesi ikincisini bekliyor; takma ad olmadan derleyici
// "ambiguous reference" der. (Takma ad kaldırılırsa hata derleme zamanında çıkar — sessiz değil.)
using ProxyNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace KadirliApp.Infrastructure.Http;

/// <summary>
/// Faz 12.2 — **gerçek istemci IP'sini görmenin tek doğru yolu.**
///
/// 12.2 öncesinde bu, <c>Api/Program.cs</c>'te bir <b>yorum satırıydı</b>. Reverse proxy
/// (Nginx/Traefik) arkasında <c>RemoteIpAddress</c> her istek için <b>proxy'nin</b> IP'si
/// olur; bu da şu üç şeyi aynı anda bozar:
/// <list type="number">
///   <item><b>Giriş denemesi kaydı:</b> R2 (aynı IP'den çok hesap) <b>herkeste</b> yanar,
///         R3 (hiç görülmemiş IP) <b>hiç</b> yanmaz → gözlem katmanının ürettiği veri
///         gürültüden ibaret kalır ve üstüne kurulan e-posta uyarısı yanlış alarm makinesine döner.</item>
///   <item><b>Hız sınırı (10.7):</b> IP bazlı partition'ların tamamı <b>tek</b> partition'a
///         düşer — bir istemci herkesin kotasını yer.</item>
///   <item><b>Hangfire panosu:</b> "yalnız yerel istek" dalı çöker (her istek yerel görünür).</item>
/// </list>
///
/// 🔴 <b><c>KnownProxies</c>/<c>KnownNetworks</c> BOŞ BIRAKILMAZ.</b> ASP.NET Core'un
/// varsayılanı yalnız loopback'e güvenmektir; listeyi "herkese güven" diye açmak, istemcinin
/// kendi <c>X-Forwarded-For</c> başlığını uydurup güvenlik kaydını <b>zehirlemesine</b> izin
/// verir: saldırgan kendi IP'sini gizler, başkasınınkini yazdırır ve <c>login_attempts</c>
/// masum bir kullanıcıyı işaret eder. Bu yüzden ayar açıkken liste boşsa
/// <c>ProductionReadinessGuard</c> uygulamayı Production'da <b>açtırmaz</b>.
///
/// ⚠️ Kurulum Api ve Web'de <b>aynı</b> sınıftan geçer. İki host iki ayrı gerçekleme
/// yazsaydı biri güncellenip diğeri unutulurdu ve panelin gördüğü IP ile API'nin gördüğü
/// IP <b>ayrışırdı</b> — aynı saldırı iki tabloda iki farklı kaynaktan gelmiş görünürdü.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string EnabledKey = "ForwardedHeaders:Enabled";
    public const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";
    public const string KnownNetworksKey = "ForwardedHeaders:KnownNetworks";
    public const string ForwardLimitKey = "ForwardedHeaders:ForwardLimit";

    /// <summary>Yapılandırma bölümü hiç yoksa ara katman kurulmaz (yerel geliştirme).</summary>
    public static bool IsEnabled(IConfiguration cfg) => cfg.GetValue(EnabledKey, false);

    /// <summary>Güvenilen proxy IP'leri — <c>"10.0.0.5"</c> gibi.</summary>
    public static IReadOnlyList<string> ConfiguredProxies(IConfiguration cfg) =>
        cfg.GetSection(KnownProxiesKey).Get<string[]>() ?? Array.Empty<string>();

    /// <summary>Güvenilen ağlar — CIDR (<c>"10.0.0.0/8"</c>).</summary>
    public static IReadOnlyList<string> ConfiguredNetworks(IConfiguration cfg) =>
        cfg.GetSection(KnownNetworksKey).Get<string[]>() ?? Array.Empty<string>();

    /// <summary>
    /// Ayar açık ama güvenilen proxy/ağ listesi boş mu? Bu kombinasyon
    /// <b>açık bir güvenlik açığıdır</b>, yanlış yapılandırma değil.
    /// </summary>
    public static bool IsEnabledWithoutTrustedSources(IConfiguration cfg) =>
        IsEnabled(cfg) && ConfiguredProxies(cfg).Count == 0 && ConfiguredNetworks(cfg).Count == 0;

    /// <summary>
    /// Ara katmanı kurar. Ayar kapalıysa hiçbir şey yapmaz — yerel geliştirmede
    /// <c>RemoteIpAddress</c> zaten gerçek istemcidir.
    /// </summary>
    /// <remarks>
    /// ⚠️ Çağrı <b>pipeline'ın EN BAŞINDA</b> olmalı: IP'ye bakan her şey (hız sınırı,
    /// giriş denemesi kaydı, hata kaydı, Hangfire filtresi) ondan sonra gelir. Sonraya
    /// konursa ara katman çalışır ama <b>kimse değişmiş IP'yi görmez</b> — hata vermeyen,
    /// tamamen sessiz bir arıza (12.1'in <c>PanelErrorLoggingMiddleware</c> sıra dersinin
    /// aynısı, ters yönü).
    /// </remarks>
    public static void UseConfiguredForwardedHeaders(this IApplicationBuilder app, IConfiguration cfg, ILogger logger)
    {
        if (!IsEnabled(cfg))
            return;

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // Kaç proxy atlanacak. Varsayılan 1: tek bir ters vekil. Zincir daha uzunsa
            // (CDN + yük dengeleyici) bilinçli olarak artırılır — körlemesine büyütmek
            // saldırganın uydurduğu başlıkların da okunmasına yol açar.
            ForwardLimit = cfg.GetValue(ForwardLimitKey, 1)
        };

        // 🔴 Varsayılan loopback girdileri TEMİZLENİR: liste "loopback + benimkiler"
        // olarak kalsaydı, uygulamayla aynı makinede koşan herhangi bir süreç
        // X-Forwarded-For uydurabilirdi.
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();

        var proxies = 0;
        foreach (var raw in ConfiguredProxies(cfg))
        {
            if (IPAddress.TryParse(raw, out var address))
            {
                options.KnownProxies.Add(address);
                proxies++;
            }
            else
            {
                // Sessizce atlamak, "yapılandırdım ama çalışmıyor" sınıfının ta kendisi.
                logger.LogError("ForwardedHeaders:KnownProxies içindeki '{Value}' geçerli bir IP değil — YOK SAYILDI.", raw);
            }
        }

        var networks = 0;
        foreach (var raw in ConfiguredNetworks(cfg))
        {
            if (TryParseNetwork(raw, out var network))
            {
                options.KnownNetworks.Add(network!);
                networks++;
            }
            else
            {
                logger.LogError("ForwardedHeaders:KnownNetworks içindeki '{Value}' geçerli bir CIDR değil — YOK SAYILDI.", raw);
            }
        }

        if (proxies + networks == 0)
        {
            // Bu durumda ara katman HİÇBİR kaynağa güvenmez, yani X-Forwarded-For hiç
            // okunmaz. Sessiz kalmak yanlış olurdu: yönetici ayarı açtı ve çalıştığını sanıyor.
            logger.LogError(
                "ForwardedHeaders açık ama güvenilen proxy/ağ YOK — başlıklar yok sayılacak, " +
                "istemci IP'si proxy'nin IP'si olarak kalacak. KnownProxies veya KnownNetworks doldurun.");
        }

        app.UseForwardedHeaders(options);

        logger.LogInformation(
            "ForwardedHeaders etkin: {Proxies} proxy, {Networks} ağ güvenilir, ForwardLimit={Limit}.",
            proxies, networks, options.ForwardLimit);
    }

    /// <summary><c>"10.0.0.0/8"</c> → güvenilen ağ.</summary>
    private static bool TryParseNetwork(string raw, out ProxyNetwork? network)
    {
        network = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var parts = raw.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out var prefix)) return false;
        if (!int.TryParse(parts[1], out var length)) return false;

        var maxLength = prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32;
        if (length < 0 || length > maxLength) return false;

        network = new ProxyNetwork(prefix, length);
        return true;
    }
}
