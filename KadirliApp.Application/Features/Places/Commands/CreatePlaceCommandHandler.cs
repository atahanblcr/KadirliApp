using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Places.Commands;

public class CreatePlaceCommandHandler : IRequestHandler<CreatePlaceCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreatePlaceCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreatePlaceCommand request, CancellationToken cancellationToken)
    {
        var place = new Place
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            EntranceFee = request.EntranceFee,
            IsFree = request.IsFree,
            OpeningHours = request.OpeningHours,
            BestSeason = request.BestSeason,
            HowToGetThere = request.HowToGetThere,
            DistanceFromCenter = request.DistanceFromCenter,
            Amenities = request.Amenities,
            CoverImageId = request.CoverImageId,
            IsActive = true
        };

        await _uow.Repository<Place>().AddAsync(place, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return place.Id;
    }
}
