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

public class GetIntracityRoutesQueryHandler : IRequestHandler<GetIntracityRoutesQuery, PagedResult<IntracityRouteResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetIntracityRoutesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<IntracityRouteResponseDto>> Handle(GetIntracityRoutesQuery request, CancellationToken cancellationToken)
    {
        var dto = request.QueryDto;
        var query = _uow.Repository<IntracityRoute>().Query();

        // Faz 10.7 düzeltmesi: IsActive filtresi yoktu — pasif hatlar public uçta dönüyordu.
        if (request.OnlyActive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(dto.SearchTerm))
            query = query.Where(x =>
                x.RouteName.ToLower().Contains(dto.SearchTerm.ToLower()) ||
                x.RouteNumber.ToLower().Contains(dto.SearchTerm.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyActive ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        var items = await query
            .OrderBy(x => x.RouteNumber)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new IntracityRouteResponseDto
            {
                Id = x.Id,
                RouteNumber = x.RouteNumber,
                RouteName = x.RouteName,
                FirstDeparture = x.FirstDeparture,
                LastDeparture = x.LastDeparture,
                FrequencyMinutes = x.FrequencyMinutes,
                IsActive = x.IsActive,
                // Faz 10.8: güzergâh durakları (StopOrder sıralı)
                Stops = x.Stops
                    .OrderBy(s => s.StopOrder)
                    .Select(s => new IntracityRouteResponseDto.StopDto(s.Id, s.StopName, s.StopOrder, s.TimeFromStart))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<IntracityRouteResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
