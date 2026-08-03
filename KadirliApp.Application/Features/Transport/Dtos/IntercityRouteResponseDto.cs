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

    /// <summary>Faz 10.8: kalkış saatleri — "HH:mm", sıralı; yalnız aktif seferler (mobil "Adana otobüsü kaçta?").</summary>
    public List<ScheduleDto> Schedules { get; set; } = new();

    /// <summary>
    /// Faz 11.17: <c>IsActive</c> additive olarak eklendi. Liste sorgusu (mobil) yalnız aktifleri
    /// döndürdüğü için orada her zaman <c>true</c>'dur; panelin tek-kayıt sorgusu pasifleri de
    /// döndürür ve bu bayrakla işaretler — aksi hâlde panel, mobilde <b>görünmeyen</b> bir saati
    /// yayındaymış gibi gösterirdi (görünmez sözleşme #23'ün aynı sınıfı).
    /// </summary>
    public record ScheduleDto(Guid Id, string DepartureTime, bool IsActive = true);
}
