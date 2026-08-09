using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Events.Commands;

public class CreateEventCommand : IRequest<Guid>, IAuditableCommand
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }

    /// <summary>Faz 12.4: etkinliğin ilçesi — <b>zorunlu</b> (bkz. <c>EventDistrictResolver</c>).</summary>
    public Guid? DistrictId { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Organizer { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }

    /// <summary>
    /// ☠️ Faz 12.4'ten beri <b>yok sayılır</b>: <c>IsLocal</c> artık <see cref="DistrictId"/>'den
    /// türetiliyor. Alan, panelin eski form gönderimlerini kırmamak için duruyor.
    /// </summary>
    public bool IsLocal { get; set; } = true;

    public Guid? CoverImageId { get; set; }

    /// <summary>Oluşturan kullanıcı; controller claim'lerden set eder, formdan bind edilmez.</summary>
    public Guid CreatedBy { get; set; }

    /// <summary>Admin panelden oluşturulan etkinlikler doğrudan onaylı başlar.</summary>
    public bool AutoApprove { get; set; }

    // Faz 12.4 (plan dışı): etkinlik oluşturma/güncelleme denetim izine hiç düşmüyordu —
    // "bu etkinliği kim ekledi?" sorusunun cevabı panelde yoktu (onay/red düşüyordu).
    public string AuditModule => "events";
    public string AuditAction => "create";
    public string? AuditAffectedType => "Event";
    public object? AuditDetails => new { title = Title };
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
        // Faz 12.4 — konum tek kuraldan geçer: ilçe doğrulanır, IsLocal ondan TÜRETİLİR.
        var district = await EventDistrictResolver.ResolveAsync(_uow, request.DistrictId, cancellationToken);
        if (district.Missing)
            throw new AppException(EventDistrictResolver.MissingMessage, "VALIDATION_ERROR");
        if (district.NotFound)
            throw new AppException(EventDistrictResolver.NotFoundMessage, "VALIDATION_ERROR");

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
            DistrictId = district.Id,
            IsLocal = district.IsLocal,
            CoverImageId = request.CoverImageId,
            Status = request.AutoApprove ? "approved" : "pending",
            CreatedBy = request.CreatedBy
        };

        await _uow.Repository<Event>().AddAsync(ev, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ev.Id;
    }
}
