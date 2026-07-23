using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Transport.Commands;

public class UpdateIntracityRouteCommandHandler : IRequestHandler<UpdateIntracityRouteCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateIntracityRouteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateIntracityRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _unitOfWork.Repository<IntracityRoute>().GetByIdAsync(request.Id);
        if (route == null) return false;

        route.RouteNumber = request.RouteNumber;
        route.RouteName = request.RouteName;
        route.FirstDeparture = request.FirstDeparture;
        route.LastDeparture = request.LastDeparture;
        route.FrequencyMinutes = request.FrequencyMinutes;
        route.IsActive = request.IsActive;

        _unitOfWork.Repository<IntracityRoute>().Update(route);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
