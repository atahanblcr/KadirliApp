using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Events.Commands;

public class UpdateEventCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Organizer { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }
    public bool IsLocal { get; set; } = true;
    public Guid? CoverImageId { get; set; }
    public bool RemoveCoverImage { get; set; }
    public string Status { get; set; } = "pending";
}

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateEventCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Event>();
        var ev = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (ev == null) return false;

        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.CategoryId = request.CategoryId;
        ev.EventDate = DateTime.SpecifyKind(request.EventDate, DateTimeKind.Utc);
        ev.EventTime = request.EventTime;
        ev.VenueName = request.VenueName;
        ev.Address = request.Address;
        ev.Latitude = request.Latitude;
        ev.Longitude = request.Longitude;
        ev.Organizer = request.Organizer;
        ev.TicketPrice = request.TicketPrice;
        ev.IsFree = request.IsFree;
        ev.IsLocal = request.IsLocal;
        ev.Status = request.Status;

        if (request.RemoveCoverImage)
            ev.CoverImageId = null;
        else if (request.CoverImageId.HasValue)
            ev.CoverImageId = request.CoverImageId;

        repo.Update(ev);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
