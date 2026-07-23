namespace KadirliApp.Application.Common.Models;

/// <summary>
/// Faz 10.7: Page/Limit clamp'i — DoS koruması. Hiçbir liste handler'ı istemciden gelen
/// Page/Limit değerini clamp'lemeden Skip/Take'e vermemeli (?limit=1000000 tüm tabloyu çekiyordu).
/// Public uçlar varsayılan MaxLimit (50), admin/panel listeleri AdminMaxLimit (200) ile clamp'lenir.
/// </summary>
public static class Pagination
{
    public const int MaxLimit = 50;
    public const int AdminMaxLimit = 200;

    public static (int Page, int Limit) Clamp(int page, int limit, int maxLimit = MaxLimit)
        => (Math.Max(page, 1), Math.Clamp(limit, 1, maxLimit));
}
