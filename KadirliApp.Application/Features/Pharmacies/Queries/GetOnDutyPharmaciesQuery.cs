using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Pharmacies.Queries;

/// <summary>
/// Faz 10.4: GET /v1/pharmacies/on-duty — verilen günün (varsayılan: Türkiye saatiyle bugün) nöbetçileri.
/// duty_date satırları amaçlanan YEREL günün UTC gece yarısında tutulur (DbSeeder/MockDataSeeder deseni).
/// </summary>
public sealed record GetOnDutyPharmaciesQuery(DateOnly? Date) : IRequest<IReadOnlyList<Dtos.OnDutyPharmacyDto>>, ICacheableQuery
{
    /// <summary>Sorgulanan gün — parametre yoksa Türkiye günü (cache anahtarı gece yarısında kendiliğinden değişir).</summary>
    public DateOnly EffectiveDate => Date ?? TurkeyClock.Today;

    public string CacheKey => $"pharmacies:on-duty:{EffectiveDate:yyyy-MM-dd}";
    public string CacheGroup => CacheGroups.Pharmacies;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

/// <summary>"Bugün" kavramı için Türkiye saati (Kadirli). 2016'dan beri DST yok — sabit UTC+3 fallback güvenli.</summary>
internal static class TurkeyClock
{
    public static DateOnly Today
    {
        get
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
                return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
            }
            catch (TimeZoneNotFoundException)
            {
                return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
            }
        }
    }
}

public sealed class GetOnDutyPharmaciesQueryHandler
    : IRequestHandler<GetOnDutyPharmaciesQuery, IReadOnlyList<Dtos.OnDutyPharmacyDto>>
{
    private readonly IUnitOfWork _uow;

    public GetOnDutyPharmaciesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<Dtos.OnDutyPharmacyDto>> Handle(
        GetOnDutyPharmaciesQuery request, CancellationToken cancellationToken)
    {
        var dayStart = DateTime.SpecifyKind(
            request.EffectiveDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        // TimeSpan format'ı SQL'e çevrilemez → ham değerler çekilip bellek tarafında formatlanır
        var rows = await _uow.Repository<PharmacySchedule>().Query()
            .Where(s => s.DutyDate >= dayStart && s.DutyDate < dayEnd && s.Pharmacy.IsActive)
            .OrderBy(s => s.Pharmacy.Name)
            .Select(s => new { Schedule = s, s.Pharmacy })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new Dtos.OnDutyPharmacyDto(
            r.Schedule.Id,
            r.Schedule.DutyDate,
            r.Schedule.StartTime.ToString(@"hh\:mm"),
            r.Schedule.EndTime.ToString(@"hh\:mm"),
            r.Pharmacy.Id,
            r.Pharmacy.Name,
            r.Pharmacy.Address,
            r.Pharmacy.Phone,
            r.Pharmacy.Latitude,
            r.Pharmacy.Longitude,
            r.Pharmacy.PharmacistName,
            r.Pharmacy.WorkingHours)).ToList();
    }
}
