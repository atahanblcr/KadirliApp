using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Application.Features.Transport;

/// <summary>
/// Faz 12.5 — <c>IntercityRoute</c> → <see cref="IntercityRouteResponseDto"/> projeksiyonunun
/// <b>tek sahibi</b>.
/// </summary>
/// <remarks>
/// 🔑 <b>Neden ortaklaştırıldı:</b> aynı <c>Select</c> bloğu liste
/// (<c>GetIntercityRoutesQueryHandler</c>) ve detay (<c>GetIntercityRouteByIdQueryHandler</c>)
/// sorgularında iki kez yazılıydı — 12.4'te etkinlikte bulunan hatanın birebir aynısı
/// (görünmez sözleşme #43). 12.5'in beş yeni alanı yalnız birine eklenseydi panelin hat
/// düzenleme ekranı <b>sessizce araç tipsiz/kalkış noktasız</b> kalırdı: ekran açılır, hata
/// vermez, yalnız bir satır eksiktir.
///
/// ⚠️ İki bilinçli fark parametreleştirildi, kopyalanmadı:
/// <list type="bullet">
///   <item><paramref name="onlyActiveSchedules"/> — mobil yalnız aktif seferi görür,
///         panel pasifleri de görmek zorundadır (yoksa panel, mobilde <b>görünmeyen</b> bir
///         saati yayındaymış gibi gösterir);</item>
///   <item>saat biçimi <c>"HH:mm"</c> (görünmez sözleşme #7) ve gün kodları bellekte üretilir —
///         <c>TimeSpan.ToString</c> ve maske çözümü SQL'e çevrilemez (10.4 notu), bu yüzden
///         ifade ağacı <b>ham alanları</b> döndürür ve hesap tek bir <see cref="Finish"/>
///         adımında yapılır (12.4'ün <c>EventProjection</c> deseni).</item>
/// </list>
/// </remarks>
public static class IntercityRouteProjection
{
    /// <param name="Schedules">Ham sefer satırları — biçimleme <see cref="Finish"/>'te.</param>
    public sealed record ScheduleRow(Guid Id, TimeSpan DepartureTime, bool IsActive, int OperatingDays);

    /// <param name="Dto">Seferleri henüz yazılmamış DTO.</param>
    public sealed record Row(IntercityRouteResponseDto Dto, IReadOnlyList<ScheduleRow> Schedules);

    public static Expression<Func<IntercityRoute, Row>> Select(bool onlyActiveSchedules) => x => new Row(
        new IntercityRouteResponseDto
        {
            Id = x.Id,
            Destination = x.Destination,
            Price = x.Price,
            DurationMinutes = x.DurationMinutes,
            Company = x.Company,
            IsActive = x.IsActive,
            VehicleType = x.VehicleType,
            DeparturePointId = x.DeparturePointId,
            DeparturePointName = x.DeparturePoint != null ? x.DeparturePoint.Name : null,
            DeparturePointAddress = x.DeparturePoint != null ? x.DeparturePoint.Address : null,
            DeparturePointLatitude = x.DeparturePoint != null ? x.DeparturePoint.Latitude : null,
            DeparturePointLongitude = x.DeparturePoint != null ? x.DeparturePoint.Longitude : null
        },
        x.Schedules
            .Where(s => !onlyActiveSchedules || s.IsActive)
            .OrderBy(s => s.DepartureTime)
            .Select(s => new ScheduleRow(s.Id, s.DepartureTime, s.IsActive, s.OperatingDays))
            .ToList());

    public static IntercityRouteResponseDto Finish(Row row)
    {
        row.Dto.Schedules = row.Schedules
            .Select(s =>
            {
                var days = new OperatingDays(s.OperatingDays);
                return new IntercityRouteResponseDto.ScheduleDto(
                    s.Id,
                    // Görünmez sözleşme #7: şehirlerarası saat biçimi "07:00" (şehir içi "06:30:00").
                    s.DepartureTime.ToString(@"hh\:mm"),
                    s.IsActive,
                    days.Codes(),
                    days.RunsDaily);
            })
            .ToList();

        return row.Dto;
    }
}
