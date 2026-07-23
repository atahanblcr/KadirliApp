using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Places.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Places.Queries;

public class GetPlaceByIdQueryHandler : IRequestHandler<GetPlaceByIdQuery, PlaceResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetPlaceByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PlaceResponseDto?> Handle(GetPlaceByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _uow.Repository<Place>().Query()
            .Where(x => x.Id == request.Id);

        // Faz 10.7 düzeltmesi: pasif mekan public uçta dönmez.
        if (request.OnlyActive)
            query = query.Where(x => x.IsActive);

        return await query
            .Select(place => new PlaceResponseDto
            {
                Id = place.Id,
                CategoryId = place.CategoryId,
                Name = place.Name,
                Description = place.Description,
                Address = place.Address,
                Latitude = place.Latitude,
                Longitude = place.Longitude,
                EntranceFee = place.EntranceFee,
                IsFree = place.IsFree,
                OpeningHours = place.OpeningHours,
                BestSeason = place.BestSeason,
                HowToGetThere = place.HowToGetThere,
                DistanceFromCenter = place.DistanceFromCenter,
                Amenities = place.Amenities,
                CoverImageId = place.CoverImageId,
                CoverImageUrl = place.CoverImage != null ? place.CoverImage.CdnUrl : null,
                IsActive = place.IsActive,
                CreatedBy = place.CreatedBy,
                CreatedAt = place.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
