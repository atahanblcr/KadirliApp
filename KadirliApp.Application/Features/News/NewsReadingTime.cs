using System;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 (plan dışı ek) — düz metinden <b>tahmini okuma süresi</b>.
///
/// 🔑 Sunucuda üretiliyor ve kolonda saklanıyor; istemcide hesaplanmıyor. Sebep §7 madde 43'ün
/// sınıfı: iki yerde hesaplansaydı panel "3 dk", mobil "4 dk" derdi ve <b>kimse hata almazdı</b>.
/// Ayrıca liste ucu gövdeyi <b>taşımadığı</b> için istemcinin hesaplayacak verisi zaten yok.
///
/// 📌 Ölçü: <b>dakikada 200 kelime</b> (Türkçe için yaygın kabul; kelime uzunluğu farkı
/// tahmini bir alanda anlamlı sapma üretmiyor). Sonuç <b>en az 1</b> — "0 dk okuma" bir
/// bilgi değil, bir hata gibi görünür.
/// </summary>
public static class NewsReadingTime
{
    public const int WordsPerMinute = 200;

    public static int Minutes(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return 1;

        var words = plainText.Split(
            new[] { ' ', '\t', '\n', '\r', ' ' },
            StringSplitOptions.RemoveEmptyEntries).Length;

        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }
}
