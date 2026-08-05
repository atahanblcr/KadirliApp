using System.Text.RegularExpressions;

namespace KadirliApp.Application.Common.Observability;

/// <summary>
/// Faz 12.1 — **hata kaydına PII sızmasını önler.**
///
/// <c>CODE_REVIEW_CHECKLIST</c> §7: "Hassas veri (telefon, adres, TC vb.) loglanmıyor mu?"
/// Hata kaydı bu kuralın en kolay ihlal edildiği yer, çünkü <c>Path</c> alanı isteğin
/// sorgu dizesini de taşıyor ve OTP akışında telefon oraya düşebiliyor
/// (<c>/v1/auth/login?phone=+905001112233</c>). Tablo bir kez dolduktan sonra
/// "sonra temizleriz" diye bir şey yok — maskeleme <b>yazmadan önce</b> yapılır.
///
/// ⚠️ Bu bir güvenlik sınırı, kozmetik değil: hata kayıtları panelden görülüyor,
/// CSV olarak <b>dışa aktarılıyor</b> ve saklama süresi 90 gün.
/// </summary>
public static class SensitiveDataMasker
{
    public const string Mask = "***";

    /// <summary>
    /// Sorgu dizesinde değeri gizlenecek anahtarlar. Karşılaştırma harf duyarsız ve
    /// <b>içerik bazlı</b> (<c>userPhone</c>, <c>access_token</c> da yakalanır).
    /// </summary>
    private static readonly string[] SensitiveKeys =
    {
        "phone", "otp", "token", "password", "email", "secret", "authorization", "apikey", "api_key"
    };

    private static readonly Regex QueryPairPattern = new(@"([^?&=]+)=([^&]*)", RegexOptions.Compiled);

    /// <summary>
    /// Yolu ve sorgu dizesini maskeler. Yol kısmına dokunulmaz (rota şekli tanılama için
    /// gerekli), yalnız hassas <b>değerler</b> gizlenir.
    /// </summary>
    public static string? MaskPath(string? pathWithQuery)
    {
        if (string.IsNullOrWhiteSpace(pathWithQuery))
            return pathWithQuery;

        var separator = pathWithQuery.IndexOf('?');
        if (separator < 0)
            return pathWithQuery;

        var path = pathWithQuery[..separator];
        var query = pathWithQuery[(separator + 1)..];

        var masked = QueryPairPattern.Replace(query, match =>
        {
            var key = match.Groups[1].Value;
            return IsSensitive(key) ? $"{key}={Mask}" : match.Value;
        });

        return $"{path}?{masked}";
    }

    public static bool IsSensitive(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        SensitiveKeys.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase));
}
