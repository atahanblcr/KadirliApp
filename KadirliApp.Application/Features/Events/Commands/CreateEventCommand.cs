using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Events.Commands;

public class CreateEventCommand : IRequest<Guid>
{
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

    /// <summary>Oluşturan kullanıcı; controller claim'lerden set eder, formdan bind edilmez.</summary>
    public Guid CreatedBy { get; set; }

    /// <summary>Admin panelden oluşturulan etkinlikler doğrudan onaylı başlar.</summary>
    public bool AutoApprove { get; set; }
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateEventCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var ev = new Event
        {
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            EventDate = DateTime.SpecifyKind(request.EventDate, DateTimeKind.Utc),
            EventTime = request.EventTime,
            VenueName = request.VenueName,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Organizer = request.Organizer,
            TicketPrice = request.TicketPrice,
            IsFree = request.IsFree,
            IsLocal = request.IsLocal,
            CoverImageId = request.CoverImageId,
            Status = request.AutoApprove ? "approved" : "pending",
            CreatedBy = request.CreatedBy
        };

        await _uow.Repository<Event>().AddAsync(ev, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ev.Id;
    }
}
