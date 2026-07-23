using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Pharmacies.Queries;

public class GetPharmaciesQueryHandler : IRequestHandler<GetPharmaciesQuery, PagedResult<PharmacyResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetPharmaciesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<PharmacyResponseDto>> Handle(GetPharmaciesQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<Pharmacy>().Query();

        // Faz 10.7 düzeltmesi: IsActive istemciye bırakılmıştı — pasif eczaneler public uçta dönüyordu.
        if (request.OnlyActive)
            query = query.Where(x => x.IsActive);
        else if (dto.IsActive.HasValue)
            query = query.Where(x => x.IsActive == dto.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(dto.Search))
            query = query.Where(x => x.Name.ToLower().Contains(dto.Search.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyActive ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new PharmacyResponseDto(
                x.Id,
                x.Name,
                x.Address,
                x.Phone,
                x.Latitude,
                x.Longitude,
                x.WorkingHours,
                x.PharmacistName,
                x.IsActive
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<PharmacyResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
