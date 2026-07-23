using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Taxis.Queries;

public record TaxiAdminStatsDto(int CallCount, DateTime? LastCallAt);

/// <summary>
/// Faz 10.10-A (vizyon turu): TaxiAdmin Index'in "Çağrı / Son Çağrı" kolonları — panel-only,
/// public TaxiDriverResponseDto'ya bilinçli alan EKLENMEDİ (kontrat donmak üzere; sayaç public'e sızmaz).
/// KARAR: tek kaynak taxi_calls (COUNT + MAX aynı group-by'dan) — denormalize total_calls ile
/// çift kaynaklı tutarsızlık görünümü olmasın. Cache'siz: admin-only + "şu anki sayı" beklentisi.
/// </summary>
public record GetTaxiAdminStatsQuery : IRequest<Dictionary<Guid, TaxiAdminStatsDto>>;

public class GetTaxiAdminStatsQueryHandler : IRequestHandler<GetTaxiAdminStatsQuery, Dictionary<Guid, TaxiAdminStatsDto>>
{
    private readonly IUnitOfWork _uow;

    public GetTaxiAdminStatsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Dictionary<Guid, TaxiAdminStatsDto>> Handle(GetTaxiAdminStatsQuery request, CancellationToken cancellationToken)
    {
        return await _uow.Repository<TaxiCall>().Query()
            .GroupBy(c => c.DriverId)
            .Select(g => new { DriverId = g.Key, Count = g.Count(), Last = (DateTime?)g.Max(c => c.CalledAt) })
            .ToDictionaryAsync(x => x.DriverId, x => new TaxiAdminStatsDto(x.Count, x.Last), cancellationToken);
    }
}
