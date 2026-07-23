using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Transport.Commands;

public class CreateIntracityRouteCommandHandler : IRequestHandler<CreateIntracityRouteCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateIntracityRouteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateIntracityRouteCommand request, CancellationToken cancellationToken)
    {
        var route = new IntracityRoute
        {
            Id = Guid.NewGuid(),
            RouteNumber = request.RouteNumber,
            RouteName = request.RouteName,
            FirstDeparture = request.FirstDeparture,
            LastDeparture = request.LastDeparture,
            FrequencyMinutes = request.FrequencyMinutes,
            IsActive = request.IsActive
        };

        await _unitOfWork.Repository<IntracityRoute>().AddAsync(route);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return route.Id;
    }
}
