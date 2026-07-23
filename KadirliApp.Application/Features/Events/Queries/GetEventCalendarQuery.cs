using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Events.Queries;

public class EventCalendarItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public string? VenueName { get; set; }
    public string? CategoryName { get; set; }
    public string Status { get; set; } = default!;
}

/// <summary>
/// Bir ayın etkinliklerini takvim görünümü için döndürür.
/// OnlyApproved=true mobil istemci içindir; admin tüm durumları görür.
/// </summary>
public record GetEventCalendarQuery(int Year, int Month, bool OnlyApproved = false)
    : IRequest<List<EventCalendarItemDto>>;

public class GetEventCalendarQueryHandler : IRequestHandler<GetEventCalendarQuery, List<EventCalendarItemDto>>
{
    private readonly IUnitOfWork _uow;

    public GetEventCalendarQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<EventCalendarItemDto>> Handle(GetEventCalendarQuery request, CancellationToken cancellationToken)
    {
        var monthStart = DateTime.SpecifyKind(new DateTime(request.Year, request.Month, 1), DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var query = _uow.Repository<Event>().Query()
            .Where(x => x.EventDate >= monthStart && x.EventDate < monthEnd);

        if (request.OnlyApproved)
            query = query.Where(x => x.Status == "approved");

        return await query
            .OrderBy(x => x.EventDate).ThenBy(x => x.EventTime)
            .Select(x => new EventCalendarItemDto
            {
                Id = x.Id,
                Title = x.Title,
                EventDate = x.EventDate,
                EventTime = x.EventTime,
                VenueName = x.VenueName,
                CategoryName = x.Category.Name,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }
}
