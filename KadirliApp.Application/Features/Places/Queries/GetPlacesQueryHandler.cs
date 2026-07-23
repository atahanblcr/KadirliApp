using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Places.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Places.Queries;

public class GetPlacesQueryHandler : IRequestHandler<GetPlacesQuery, PagedResult<PlaceResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetPlacesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<PlaceResponseDto>> Handle(GetPlacesQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<Place>().Query();

        // Faz 10.7 düzeltmesi: IsActive filtresi hiç yoktu — pasif mekanlar public uçta dönüyordu.
        if (request.OnlyActive)
            query = query.Where(x => x.IsActive);

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == dto.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(dto.Search))
            query = query.Where(x => x.Name.ToLower().Contains(dto.Search.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyActive ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new PlaceResponseDto
            {
                Id = x.Id,
                CategoryId = x.CategoryId,
                Name = x.Name,
                Description = x.Description,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                EntranceFee = x.EntranceFee,
                IsFree = x.IsFree,
                OpeningHours = x.OpeningHours,
                BestSeason = x.BestSeason,
                HowToGetThere = x.HowToGetThere,
                DistanceFromCenter = x.DistanceFromCenter,
                Amenities = x.Amenities,
                CoverImageId = x.CoverImageId,
                CoverImageUrl = x.CoverImage != null ? x.CoverImage.CdnUrl : null,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PlaceResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
