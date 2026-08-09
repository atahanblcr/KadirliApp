using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Transport.Dtos;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
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

        // Faz 12.5: araç tipi süzgeci. Tanınmayan değer null'a düşer → süzme yok (§5).
        var vehicleType = TransportVehicleTypes.NormalizeFilter(dto.VehicleType);
        if (vehicleType is not null)
            query = query.Where(x => x.VehicleType == vehicleType);

        if (!string.IsNullOrWhiteSpace(dto.SearchTerm))
            query = query.Where(x =>
                x.Destination.ToLower().Contains(dto.SearchTerm.ToLower()) ||
                (x.Company != null && x.Company.ToLower().Contains(dto.SearchTerm.ToLower())));

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyActive ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        // Faz 10.8: kalkış saatleri eklendi. TimeSpan "HH:mm" formatı SQL'e çevrilemediğinden (10.4 notu)
        // saatler TimeSpan olarak çekilir, formatlama bellek tarafında yapılır.
        // Faz 12.5: projeksiyon tek sahipli (IntercityRouteProjection) — liste ile detayın
        // ayrışması 12.4'te etkinlikte yaşandı ve hiçbir test yakalamamıştı.
        var raw = await query
            .OrderBy(x => x.Destination)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(IntercityRouteProjection.Select(onlyActiveSchedules: true))
            .ToListAsync(cancellationToken);

        var items = raw.Select(IntercityRouteProjection.Finish).ToList();

        return new PagedResult<IntercityRouteResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
