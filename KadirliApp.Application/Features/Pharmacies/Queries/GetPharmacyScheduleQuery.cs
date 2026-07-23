using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Pharmacies.Queries;

/// <summary>Faz 10.4: GET /v1/pharmacies/schedule?year&month — aylık nöbet takvimi (public + admin aynı query'yi kullanır).</summary>
public sealed record GetPharmacyScheduleQuery(int Year, int Month)
    : IRequest<IReadOnlyList<Dtos.PharmacyScheduleEntryDto>>, ICacheableQuery
{
    public string CacheKey => $"pharmacies:schedule:{Year:D4}-{Month:D2}";
    public string CacheGroup => CacheGroups.Pharmacies;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetPharmacyScheduleQueryHandler
    : IRequestHandler<GetPharmacyScheduleQuery, IReadOnlyList<Dtos.PharmacyScheduleEntryDto>>
{
    private readonly IUnitOfWork _uow;

    public GetPharmacyScheduleQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<Dtos.PharmacyScheduleEntryDto>> Handle(
        GetPharmacyScheduleQuery request, CancellationToken cancellationToken)
    {
        if (request.Month is < 1 or > 12 || request.Year is < 2000 or > 2100)
            throw new AppException("Geçersiz yıl/ay.", "VALIDATION_ERROR");

        var monthStart = DateTime.SpecifyKind(new DateTime(request.Year, request.Month, 1), DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var rows = await _uow.Repository<PharmacySchedule>().Query()
            .Where(s => s.DutyDate >= monthStart && s.DutyDate < monthEnd)
            .OrderBy(s => s.DutyDate).ThenBy(s => s.Pharmacy.Name)
            .Select(s => new { Schedule = s, s.Pharmacy.Name })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new Dtos.PharmacyScheduleEntryDto(
            r.Schedule.Id,
            r.Schedule.DutyDate,
            r.Schedule.StartTime.ToString(@"hh\:mm"),
            r.Schedule.EndTime.ToString(@"hh\:mm"),
            r.Schedule.PharmacyId,
            r.Name,
            r.Schedule.Source)).ToList();
    }
}
