using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Transport.Queries;

// Faz 11.17: paneldeki hat detay ekranları (kalkış saati / durak yönetimi) için tek kayıt sorguları.
// Liste sorguları zaten çocuk koleksiyonları döndürüyor ama detay ekranında tüm listeyi
// çekip aramak gereksiz; ayrıca panel kalkış saatlerini PASİF olanlar dâhil görmeli
// (liste sorgusu mobilin ihtiyacına göre yalnız aktifleri döndürür — bu bilinçli fark).

/// <summary>Şehirlerarası hat + kalkış saatleri. Bulunamazsa null.</summary>
public record GetIntercityRouteByIdQuery(Guid Id) : IRequest<IntercityRouteResponseDto?>;

public class GetIntercityRouteByIdQueryHandler : IRequestHandler<GetIntercityRouteByIdQuery, IntercityRouteResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetIntercityRouteByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IntercityRouteResponseDto?> Handle(GetIntercityRouteByIdQuery request, CancellationToken cancellationToken)
    {
        // TimeSpan.ToString("hh\\:mm") SQL'e çevrilemez (10.4 notu) → ham çekip bellekte biçimle.
        // Faz 12.5: projeksiyon artık liste sorgusuyla ORTAK (IntercityRouteProjection) —
        // burada ikinci bir Select bloğu yazılırsa yeni alanlar birine eklenip diğerine
        // eklenmediğinde panel sessizce eksik veri gösterir (görünmez sözleşme #43'ün sınıfı).
        var raw = await _uow.Repository<IntercityRoute>().Query()
            .Where(x => x.Id == request.Id)
            .Select(IntercityRouteProjection.Select(onlyActiveSchedules: false))
            .FirstOrDefaultAsync(cancellationToken);

        return raw == null ? null : IntercityRouteProjection.Finish(raw);
    }
}

/// <summary>Şehir içi hat + duraklar (StopOrder sıralı). Bulunamazsa null.</summary>
public record GetIntracityRouteByIdQuery(Guid Id) : IRequest<IntracityRouteResponseDto?>;

public class GetIntracityRouteByIdQueryHandler : IRequestHandler<GetIntracityRouteByIdQuery, IntracityRouteResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetIntracityRouteByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IntracityRouteResponseDto?> Handle(GetIntracityRouteByIdQuery request, CancellationToken cancellationToken)
        => await _uow.Repository<IntracityRoute>().Query()
            .Where(x => x.Id == request.Id)
            .Select(x => new IntracityRouteResponseDto
            {
                Id = x.Id,
                RouteNumber = x.RouteNumber,
                RouteName = x.RouteName,
                FirstDeparture = x.FirstDeparture,
                LastDeparture = x.LastDeparture,
                FrequencyMinutes = x.FrequencyMinutes,
                IsActive = x.IsActive,
                Stops = x.Stops
                    .OrderBy(s => s.StopOrder)
                    .Select(s => new IntracityRouteResponseDto.StopDto(s.Id, s.StopName, s.StopOrder, s.TimeFromStart))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
}
