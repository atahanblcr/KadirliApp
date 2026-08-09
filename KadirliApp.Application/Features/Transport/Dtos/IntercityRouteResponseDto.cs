using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Transport.Dtos;

public class IntercityRouteResponseDto
{
    public Guid Id { get; set; }
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Faz 12.5: <c>"bus"</c> | <c>"minibus"</c> — dönüşümün tek sahibi
    /// <c>TransportVehicleTypes</c>. Türkçe karşılığı istemcide üretilir (kısa, sabit ve
    /// çevrilmesi gereken bir sözcük; <c>locationLabel</c>'ın aksine kural içermiyor).
    /// </summary>
    public string VehicleType { get; set; } = Domain.Enums.TransportVehicleTypes.Default;

    /// <summary>Faz 12.5: kalkış noktası — <c>null</c> ise girilmemiş demektir.</summary>
    public Guid? DeparturePointId { get; set; }
    public string? DeparturePointName { get; set; }
    public string? DeparturePointAddress { get; set; }

    /// <summary>
    /// Faz 12.5: mobildeki "Yol tarifi" butonunun kaynağı (12.6). Koordinat yoksa istemci
    /// <see cref="DeparturePointAddress"/> ile harita araması yapar (<c>ContactActions</c> deseni).
    /// </summary>
    public decimal? DeparturePointLatitude { get; set; }
    public decimal? DeparturePointLongitude { get; set; }

    /// <summary>Faz 10.8: kalkış saatleri — "HH:mm", sıralı; yalnız aktif seferler (mobil "Adana otobüsü kaçta?").</summary>
    public List<ScheduleDto> Schedules { get; set; } = new();

    /// <summary>
    /// Faz 11.17: <c>IsActive</c> additive olarak eklendi. Liste sorgusu (mobil) yalnız aktifleri
    /// döndürdüğü için orada her zaman <c>true</c>'dur; panelin tek-kayıt sorgusu pasifleri de
    /// döndürür ve bu bayrakla işaretler — aksi hâlde panel, mobilde <b>görünmeyen</b> bir saati
    /// yayındaymış gibi gösterirdi (görünmez sözleşme #23'ün aynı sınıfı).
    ///
    /// Faz 12.5: <c>Days</c> (<c>["mon","tue",…]</c>) + <c>RunsDaily</c> additive olarak eklendi.
    /// 🔴 <b>Uç seferleri günlere göre ELEMEZ</b>, yalnız bildirir: mağazadaki eski sürümler
    /// <c>days</c>'i tanımadığı için Pazar günü de tüm saatleri gösterir — bu bugünkü doğruluk
    /// seviyesinin aynısıdır, regresyon değildir. Sunucuda elenseydi eski sürümler için liste
    /// <b>sebepsiz boşalırdı</b>.
    /// </summary>
    public record ScheduleDto(
        Guid Id,
        string DepartureTime,
        bool IsActive = true,
        IReadOnlyList<string>? Days = null,
        bool RunsDaily = true);
}
