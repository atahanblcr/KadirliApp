using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.14 — <b>metin arası görsellerin</b> bulunması ve aynalanmış adrese
/// yeniden yazılması. Saf: ağ görmez, veritabanı görmez, yalnız metin işler.
/// </summary>
/// <remarks>
/// 🔴 <b>Neden 12.12'de yapılmadı, neden şimdi yapılıyor:</b> 12.12 bunu bilinçli bir borç
/// olarak erteledi (<c>NewsHtmlPolicy.MirrorsInlineImages = false</c>) çünkü alt-fazı ikiye
/// katlıyordu. Ama bu borcun bir <b>son kullanma tarihi</b> var: ölçüldüğü üzere gövde
/// görsellerinin <b>%9'u imzalı/süreli</b> <c>fbcdn</c>/<c>outlook</c> linki, yani zamanla
/// <b>mutlaka</b> 403'e düşecekler. Düştüklerinde istemci onları <i>zarifçe gizliyor</i>
/// (§7 madde 61) — yani hasarın belirtisi <b>hiç olmayacak</b>: haberler sessizce
/// görselsizleşecek ve kimse hata almayacak. Aynalama, kaynağın bizden bağımsız çürümesine
/// karşı tek gerçek koruma.
///
/// 🔑 <b>Neden regex</b> (HTML'i regex'le ayrıştırmak genelde yanlıştır): burada ayrıştırılan
/// şey <b>rastgele HTML değil</b>, kendi sanitizer'ımızın (<c>Ganss.Xss</c>) az önce ürettiği
/// çıktıdır — beyaz liste dar (<c>NewsHtmlPolicy</c>), <c>srcset</c>/<c>style</c> zaten
/// atılmış, öznitelikler normalleştirilmiş. Yani girdi bizim ürettiğimiz dar bir alt küme.
/// ⚠️ Bu bağımlılık görünmezdir: sanitizer'ı değiştiren biri buranın varsayımını da
/// değiştirir → <c>NewsBodyImagesTests</c> gerçek sanitizer çıktısıyla besleniyor.
/// </remarks>
public static class NewsBodyImages
{
    /// <summary>
    /// <c>&lt;img … src="…"&gt;</c>. Tek/çift tırnak ve öznitelik sırası serbest;
    /// <c>src</c> **her zaman** var (sanitizer <c>src</c>'siz <c>img</c> bırakmıyor ama
    /// eşleşmezse zaten dokunulmaz — sessiz bozma yok).
    /// </summary>
    private static readonly Regex ImgSrc = new(
        """<img\b[^>]*?\bsrc\s*=\s*(?<q>["'])(?<url>.*?)\k<q>""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    /// <summary>
    /// Gövdedeki <b>dış</b> görsel adresleri (benzersiz, geliş sırasında).
    /// </summary>
    /// <remarks>
    /// ⚠️ Yalnız <c>http(s)</c> döner. Zaten aynalanmış (<c>/uploads/…</c>) ya da göreli bir
    /// adres <b>dokunulmaz</b>: aksi hâlde her koşu kendi çıktısını yeniden indirmeye
    /// çalışır ve <c>uploads/</c> mükerrer dosyayla şişerdi.
    /// </remarks>
    public static IReadOnlyList<string> ExternalUrls(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (Match match in ImgSrc.Matches(html))
        {
            var url = match.Groups["url"].Value.Trim();
            if (!IsExternal(url)) continue;
            if (seen.Add(url)) result.Add(url);
        }

        return result;
    }

    /// <summary>
    /// <paramref name="mirrored"/> sözlüğündeki adresleri gövdede değiştirir.
    /// Sözlükte olmayan (indirilemeyen) görsel <b>olduğu gibi bırakılır</b>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Aynalanamayan görsel haberi düşürmez ve gövdeden silinmez.</b> Silmek iki
    /// sebeple yanlış olurdu: (a) kaynak bir dakika sonra ayağa kalkabilir ve o an elimizde
    /// içerik eksik kalır; (b) hotlink hâli bugünkü davranışın ta kendisi — yani "aynalayamadım"
    /// durumunda <b>12.14 öncesine düşmek</b> doğru yön ("şüphede kalınca göster").
    /// </remarks>
    public static string Rewrite(string? html, IReadOnlyDictionary<string, string> mirrored)
    {
        if (string.IsNullOrWhiteSpace(html) || mirrored.Count == 0) return html ?? string.Empty;

        return ImgSrc.Replace(html, match =>
        {
            var url = match.Groups["url"].Value.Trim();
            if (!mirrored.TryGetValue(url, out var replacement)) return match.Value;

            var group = match.Groups["url"];
            var prefix = match.Value[..(group.Index - match.Index)];
            var suffix = match.Value[(group.Index - match.Index + group.Length)..];
            return prefix + replacement + suffix;
        });
    }

    /// <summary>Gövdede hâlâ aynalanmamış bir dış görsel var mı (geri doldurma taraması).</summary>
    public static bool HasExternalImages(string? html) => ExternalUrls(html).Count > 0;

    private static bool IsExternal(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
