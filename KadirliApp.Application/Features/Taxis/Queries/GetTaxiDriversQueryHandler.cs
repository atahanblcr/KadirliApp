using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Taxis.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Taxis.Queries;

public class GetTaxiDriversQueryHandler : IRequestHandler<GetTaxiDriversQuery, PagedResult<TaxiDriverResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetTaxiDriversQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<TaxiDriverResponseDto>> Handle(GetTaxiDriversQuery request, CancellationToken cancellationToken)
    {
        var dto = request.QueryDto;
        var query = _uow.Repository<TaxiDriver>().Query();

        // Faz 10.7 düzeltmesi: bu filtreler istemciye bırakılmıştı — public uç doğrulanmamış/pasif
        // sürücüleri telefonlarıyla dönüyordu. Public'te IsVerified+IsActive zorunlu, istemci parametresi etkisiz.
        if (request.OnlyPublic)
        {
            query = query.Where(x => x.IsVerified && x.IsActive);
        }
        else
        {
            if (dto.IsVerified.HasValue)
                query = query.Where(x => x.IsVerified == dto.IsVerified.Value);

            if (dto.IsActive.HasValue)
                query = query.Where(x => x.IsActive == dto.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(dto.SearchTerm))
            query = query.Where(x =>
                x.Name.ToLower().Contains(dto.SearchTerm.ToLower()) ||
                (x.Plaka != null && x.Plaka.ToLower().Contains(dto.SearchTerm.ToLower())));

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyPublic ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new TaxiDriverResponseDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Name = x.Name,
                Phone = x.Phone,
                Plaka = x.Plaka,
                VehicleInfo = x.VehicleInfo,
                IsVerified = x.IsVerified,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TaxiDriverResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
