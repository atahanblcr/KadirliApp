using KadirliApp.Application.Common.Auditing;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Transport.Commands;

// Faz 10.8: IntercitySchedule + IntracityStop ilk kez kullanıma açıldı (panel formu 10.9'da; şimdilik admin API).

/// <summary>Şehirlerarası hatta kalkış saati ekler. Saat "HH:mm" formatında; aynı hat+saat 409.</summary>
public record CreateIntercityScheduleCommand(Guid RouteId, string DepartureTime) : IRequest<Guid>;

public class CreateIntercityScheduleCommandHandler : IRequestHandler<CreateIntercityScheduleCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateIntercityScheduleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateIntercityScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!TimeSpan.TryParse(request.DepartureTime, out var time) || time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
            throw new AppException("Kalkış saati 'HH:mm' formatında olmalıdır.", "VALIDATION_ERROR");

        var routeExists = await _uow.Repository<IntercityRoute>().Query()
            .AnyAsync(x => x.Id == request.RouteId, cancellationToken);
        if (!routeExists)
            throw new NotFoundException(nameof(IntercityRoute), request.RouteId);

        var duplicate = await _uow.Repository<IntercitySchedule>().Query()
            .AnyAsync(x => x.RouteId == request.RouteId && x.DepartureTime == time, cancellationToken);
        if (duplicate)
            throw new ConflictException("Bu hatta aynı kalkış saati zaten tanımlı.");

        var schedule = new IntercitySchedule { RouteId = request.RouteId, DepartureTime = time, IsActive = true };
        await _uow.Repository<IntercitySchedule>().AddAsync(schedule, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return schedule.Id;
    }
}

/// <summary>Kalkış saatini siler (hard delete — sefer saati iz gerektirmeyen lookup verisidir).</summary>
public record DeleteIntercityScheduleCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "transport";
    public string AuditAction => "delete-schedule";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "IntercitySchedule";
}

public class DeleteIntercityScheduleCommandHandler : IRequestHandler<DeleteIntercityScheduleCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteIntercityScheduleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeleteIntercityScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _uow.Repository<IntercitySchedule>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IntercitySchedule), request.Id);

        _uow.Repository<IntercitySchedule>().Remove(schedule);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Şehir içi hatta durak ekler. Aynı hat+sıra numarası 409 (sıra güzergâhı belirler).</summary>
public record CreateIntracityStopCommand(Guid RouteId, string StopName, int StopOrder, int? TimeFromStart) : IRequest<Guid>;

public class CreateIntracityStopCommandHandler : IRequestHandler<CreateIntracityStopCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateIntracityStopCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateIntracityStopCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StopName))
            throw new AppException("Durak adı zorunludur.", "VALIDATION_ERROR");
        if (request.StopOrder < 1)
            throw new AppException("Durak sırası 1 veya daha büyük olmalıdır.", "VALIDATION_ERROR");

        var routeExists = await _uow.Repository<IntracityRoute>().Query()
            .AnyAsync(x => x.Id == request.RouteId, cancellationToken);
        if (!routeExists)
            throw new NotFoundException(nameof(IntracityRoute), request.RouteId);

        var duplicate = await _uow.Repository<IntracityStop>().Query()
            .AnyAsync(x => x.RouteId == request.RouteId && x.StopOrder == request.StopOrder, cancellationToken);
        if (duplicate)
            throw new ConflictException("Bu hatta aynı sıra numarasına sahip durak zaten var.");

        var stop = new IntracityStop
        {
            RouteId = request.RouteId,
            StopName = request.StopName.Trim(),
            StopOrder = request.StopOrder,
            TimeFromStart = request.TimeFromStart
        };
        await _uow.Repository<IntracityStop>().AddAsync(stop, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return stop.Id;
    }
}

/// <summary>Durağı siler (hard delete — lookup verisi).</summary>
public record DeleteIntracityStopCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "transport";
    public string AuditAction => "delete-stop";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "IntracityStop";
}

public class DeleteIntracityStopCommandHandler : IRequestHandler<DeleteIntracityStopCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteIntracityStopCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeleteIntracityStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await _uow.Repository<IntracityStop>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(IntracityStop), request.Id);

        _uow.Repository<IntracityStop>().Remove(stop);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
