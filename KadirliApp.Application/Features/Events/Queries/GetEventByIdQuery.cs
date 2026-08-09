using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Events.Queries;

/// <summary>OnlyPublished=true (public uç): yalnız approved etkinlik döner, diğerine null → 404. Admin/panel varsayılanla (false) her statüyü görür.</summary>
public record GetEventByIdQuery(Guid Id, bool OnlyPublished = false) : IRequest<EventResponseDto?>;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetEventByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<EventResponseDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _uow.Repository<Event>().Query()
            .Where(x => x.Id == request.Id);

        // Faz 10.7 düzeltmesi: id bilinirse pending/rejected etkinlik dönüyordu (liste zaten approved zorluyor).
        if (request.OnlyPublished)
            query = query.Where(x => x.Status == "approved");

        // Faz 12.4: projeksiyon liste sorgusuyla ORTAK (EventProjection) — konum alanları
        // yalnız birine eklenseydi detay ekranı sessizce konumsuz kalırdı.
        var row = await query
            .Select(EventProjection.Select)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : EventProjection.Finish(row);
    }
}
