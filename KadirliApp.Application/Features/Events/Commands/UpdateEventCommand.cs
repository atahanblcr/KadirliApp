using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Events.Commands;

public class UpdateEventCommand : IRequest<bool>, IAuditableCommand
{
    public Guid Id { get; set; }
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

    /// <summary>☠️ Faz 12.4'ten beri yok sayılır — <c>IsLocal</c> <see cref="DistrictId"/>'den türetilir.</summary>
    public bool IsLocal { get; set; } = true;

    public Guid? CoverImageId { get; set; }
    public bool RemoveCoverImage { get; set; }
    public string Status { get; set; } = "pending";

    // Faz 12.4 (plan dışı): etkinlik düzenlemesi de ize düşer. Kapsam kararı "salt içerik
    // düzenlemesi gürültüdür" diyordu; etkinlikte durum başka — 12.4'ten sonra düzenleme
    // etkinliğin KONUMUNU değiştirebiliyor ve konum, kimin hangi listede göründüğünü belirliyor.
    public string AuditModule => "events";
    public string AuditAction => "update";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Event";
    public object? AuditDetails => new { title = Title };
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

        // Faz 12.4 — Create ile AYNI kuraldan geçer; ikinci bir gerçekleme yazılsaydı
        // kayıt "ilçesi Kadirli ama IsLocal=false" hâline düşebilirdi (bkz. EventDistrictResolver).
        // 🐛 12.5 canlı denetimi: kaydın ŞU ANKİ ilçesi de veriliyor. Verilmezse, ilçesi
        // sonradan pasifleştirilen bir etkinlik hiç düzenlenemez hâle gelir — yönetici yalnız
        // başlığı düzeltmek istese bile dokunmadığı bir alan yüzünden hata alır.
        var district = await EventDistrictResolver.ResolveAsync(
            _uow, request.DistrictId, cancellationToken, currentDistrictId: ev.DistrictId);
        if (district.Missing)
            throw new AppException(EventDistrictResolver.MissingMessage, "VALIDATION_ERROR");
        if (district.NotFound)
            throw new AppException(EventDistrictResolver.NotFoundMessage, "VALIDATION_ERROR");

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
        ev.DistrictId = district.Id;
        ev.IsLocal = district.IsLocal;
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
