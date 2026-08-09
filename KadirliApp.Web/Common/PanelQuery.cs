using Microsoft.AspNetCore.Http;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 12.4 — <b>"mevcut süzgeci koru, yalnız şu anahtarı değiştir"</b> adres üretimi.
/// </summary>
/// <remarks>
/// 🐛 <b>Neden var:</b> 12.4'ün konum şeridi ilk yazımda bağlantıları <c>asp-route-*</c> ile
/// <b>elle sayarak</b> kuruyordu (<c>Search</c> + <c>DistrictId</c>) ve <c>sort</c>'u sayması
/// unutulmuştu: başlığa göre sıralanmış bir listede "Çevre iller"e tıklamak sıralamayı
/// <b>sessizce varsayılana döndürüyordu</b>. Hata canlı denetimde yakalandı — hiçbir test
/// kırılmaz, hiçbir log düşmez, liste yalnız "bir şekilde" yeniden sıralanır.
///
/// 🔑 Kural <see cref="_ExportCsvButton"/>'ın yıllardır yazılı olan dersinin aynısı:
/// <i>"her listenin kendi filtre alanlarını elle sayması yerine mevcut sorgu dizesi aynen
/// taşınır — elle sayılsaydı yeni bir filtre eklendiğinde onu buraya yazmak unutulurdu."</i>
/// Bu sınıf o kuralı bir kez yazıp paylaşılabilir hâle getiriyor.
///
/// ⚠️ <c>page</c> <b>her zaman düşer</b>: süzgeç değiştikten sonra 7. sayfa artık bambaşka
/// kayıtların sayfasıdır (<c>_SortableHeader</c>'daki aynı karar).
///
/// 📌 <c>_Pagination</c>, <c>_SortableHeader</c> ve <c>_ExportCsvButton</c> hâlâ bu kuralın
/// kendi kopyalarını taşıyor (11.16b/11.18'den kalma, üçü de testli). Yeni bir süzgeç şeridi
/// yazan buradan geçmeli; o üçünün buraya çekilmesi ayrı bir temizlik adımıdır.
/// </remarks>
public static class PanelQuery
{
    /// <summary>
    /// Mevcut sorgu dizesini koruyarak <paramref name="key"/>'i <paramref name="value"/> yapar.
    /// <paramref name="value"/> boşsa anahtar <b>tamamen düşer</b> ("süzgeci kaldır").
    /// </summary>
    public static string With(HttpRequest request, string key, string? value)
    {
        var parts = request.Query
            .Where(q => !string.Equals(q.Key, key, StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(q.Key, "page", StringComparison.OrdinalIgnoreCase))
            .SelectMany(q => q.Value
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(v!)}"))
            .ToList();

        if (!string.IsNullOrEmpty(value))
            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");

        return parts.Count == 0
            ? request.Path.ToString()
            : $"{request.Path}?{string.Join("&", parts)}";
    }
}
