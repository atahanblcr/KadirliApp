using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
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
        var route = new IntercityRoute
        {
            Destination = request.Destination,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            Company = request.Company,
            IsActive = true
        };

        await _uow.Repository<IntercityRoute>().AddAsync(route, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return route.Id;
    }
}
