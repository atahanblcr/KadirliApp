using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Transport.Queries;

public class GetIntercityRoutesQueryHandler : IRequestHandler<GetIntercityRoutesQuery, PagedResult<IntercityRouteResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetIntercityRoutesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<IntercityRouteResponseDto>> Handle(GetIntercityRoutesQuery request, CancellationToken cancellationToken)
    {
        var dto = request.QueryDto;
        var query = _uow.Repository<IntercityRoute>().Query();

        // Faz 10.7 düzeltmesi: IsActive filtresi yoktu — pasif hatlar public uçta dönüyordu.
        if (request.OnlyActive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(dto.SearchTerm))
            query = query.Where(x =>
                x.Destination.ToLower().Contains(dto.SearchTerm.ToLower()) ||
                (x.Company != null && x.Company.ToLower().Contains(dto.SearchTerm.ToLower())));

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyActive ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        // Faz 10.8: kalkış saatleri eklendi. TimeSpan "HH:mm" formatı SQL'e çevrilemediğinden (10.4 notu)
        // saatler TimeSpan olarak çekilir, formatlama bellek tarafında yapılır.
        var raw = await query
            .OrderBy(x => x.Destination)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                x.Destination,
                x.Price,
                x.DurationMinutes,
                x.Company,
                x.IsActive,
                Schedules = x.Schedules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DepartureTime)
                    .Select(s => new { s.Id, s.DepartureTime })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(x => new IntercityRouteResponseDto
        {
            Id = x.Id,
            Destination = x.Destination,
            Price = x.Price,
            DurationMinutes = x.DurationMinutes,
            Company = x.Company,
            IsActive = x.IsActive,
            Schedules = x.Schedules
                .Select(s => new IntercityRouteResponseDto.ScheduleDto(s.Id, s.DepartureTime.ToString(@"hh\:mm")))
                .ToList()
        }).ToList();

        return new PagedResult<IntercityRouteResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
