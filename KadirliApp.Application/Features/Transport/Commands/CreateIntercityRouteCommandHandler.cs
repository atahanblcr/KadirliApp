using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Commands;

public class CreateIntercityRouteCommandHandler : IRequestHandler<CreateIntercityRouteCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateIntercityRouteCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateIntercityRouteCommand request, CancellationToken cancellationToken)
    {
        // Kalkış noktası doğrulaması Create ve Update'te TEK metottan geçer (12.3/12.4 dersi).
        var point = await TransportDeparturePointResolver.ResolveAsync(_uow, request.DeparturePointId, cancellationToken);
        if (!point.IsValid)
            throw new AppException(TransportDeparturePointResolver.NotFoundMessage, "VALIDATION_ERROR");

        var route = new IntercityRoute
        {
            Destination = request.Destination,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            Company = request.Company,
            // Formdan gelen değere güvenilmez: kanonikleştirmenin tek sahibi TransportVehicleTypes.
            VehicleType = TransportVehicleTypes.Normalize(request.VehicleType),
            DeparturePointId = point.Id,
            IsActive = true
        };

        await _uow.Repository<IntercityRoute>().AddAsync(route, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return route.Id;
    }
}
