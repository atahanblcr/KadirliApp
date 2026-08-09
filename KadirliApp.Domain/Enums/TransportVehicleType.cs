namespace KadirliApp.Domain.Enums;

/// <summary>
/// Faz 12.5 — şehirlerarası hattın araç tipi. Kadirli'de "Adana minibüsü" ile
/// "Adana otobüsü" ayrı işlerdir: farklı yerden kalkar, farklı sıklıkta gider.
/// </summary>
public enum TransportVehicleType
{
    Bus,
    Minibus
}

/// <summary>
/// 🔴 Enum ↔ DB/DTO metni dönüşümünün <b>tek sahibi</b>.
/// </summary>
/// <remarks>
/// Değer veritabanında ve DTO'da <b>metin</b> olarak durur (<c>"bus"</c> / <c>"minibus"</c>),
/// enum'un sayısal sırası değil: sıra değişirse (araya üçüncü bir tip girerse) sayı saklayan
/// bir kolon <b>bütün kayıtları sessizce kaydırır</b> ve mağazadaki eski sürümler yanlış tip
/// gösterir. Metin ayrıca DTO'yu okunur kılar (görünmez sözleşme #16'nın "her şey metin" kararı).
///
/// ⚠️ <see cref="Parse"/> <b>bilinmeyen değeri varsayılana düşürür</b>, hata fırlatmaz
/// (<c>ARCHITECTURE.md</c> §5: bilinmeyen filtre değeri 400 değil varsayılan olmalı — bir
/// yazım hatası listeyi boşaltmamalı).
/// </remarks>
public static class TransportVehicleTypes
{
    public const string Bus = "bus";
    public const string Minibus = "minibus";

    /// <summary>12.5 öncesinden kalan satırların ve formu doldurulmamış kayıtların değeri.</summary>
    public const string Default = Bus;

    public static IReadOnlyList<string> All { get; } = new[] { Bus, Minibus };

    public static string ToValue(this TransportVehicleType type) => type switch
    {
        TransportVehicleType.Minibus => Minibus,
        _ => Bus
    };

    /// <summary>Metni enum'a çevirir; tanınmayan/boş değer <see cref="TransportVehicleType.Bus"/>.</summary>
    public static TransportVehicleType Parse(string? raw) =>
        string.Equals(raw?.Trim(), Minibus, StringComparison.OrdinalIgnoreCase)
            ? TransportVehicleType.Minibus
            : TransportVehicleType.Bus;

    /// <summary>Ham metni <b>kanonik</b> hâle getirir (kayıt yolunda kullanılır).</summary>
    public static string Normalize(string? raw) => Parse(raw).ToValue();

    /// <summary>
    /// Süzgeç için: tanınan bir değerse kanoniği, değilse <c>null</c> ("süzme yok").
    /// <see cref="Parse"/>'tan farkı bilinçli — <c>?vehicleType=otobus</c> yazan bir istemciye
    /// "yalnız otobüsler" değil <b>tüm liste</b> dönmeli.
    /// </summary>
    public static string? NormalizeFilter(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant();
        return value is Bus or Minibus ? value : null;
    }
}
