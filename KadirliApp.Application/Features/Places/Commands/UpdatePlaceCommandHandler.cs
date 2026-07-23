using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Places.Commands;

public class UpdatePlaceCommandHandler : IRequestHandler<UpdatePlaceCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdatePlaceCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdatePlaceCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Place>();
        var place = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (place == null) return false;

        place.CategoryId = request.CategoryId;
        place.Name = request.Name;
        place.Description = request.Description;
        place.Address = request.Address;
        place.Latitude = request.Latitude;
        place.Longitude = request.Longitude;
        place.EntranceFee = request.EntranceFee;
        place.IsFree = request.IsFree;
        place.OpeningHours = request.OpeningHours;
        place.BestSeason = request.BestSeason;
        place.HowToGetThere = request.HowToGetThere;
        place.DistanceFromCenter = request.DistanceFromCenter;
        place.Amenities = request.Amenities;
        place.IsActive = request.IsActive;

        if (request.RemoveCoverImage)
            place.CoverImageId = null;
        else if (request.CoverImageId.HasValue)
            place.CoverImageId = request.CoverImageId;

        repo.Update(place);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
