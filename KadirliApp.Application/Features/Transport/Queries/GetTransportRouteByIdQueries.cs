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
        var raw = await _uow.Repository<IntercityRoute>().Query()
            .Where(x => x.Id == request.Id)
            .Select(x => new
            {
                x.Id,
                x.Destination,
                x.Price,
                x.DurationMinutes,
                x.Company,
                x.IsActive,
                Schedules = x.Schedules
                    .OrderBy(s => s.DepartureTime)
                    .Select(s => new { s.Id, s.DepartureTime, s.IsActive })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw == null) return null;

        return new IntercityRouteResponseDto
        {
            Id = raw.Id,
            Destination = raw.Destination,
            Price = raw.Price,
            DurationMinutes = raw.DurationMinutes,
            Company = raw.Company,
            IsActive = raw.IsActive,
            Schedules = raw.Schedules
                .Select(s => new IntercityRouteResponseDto.ScheduleDto(s.Id, s.DepartureTime.ToString(@"hh\:mm"), s.IsActive))
                .ToList()
        };
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
