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

/// <summary>
/// Şehirlerarası hatta kalkış saati ekler. Saat "HH:mm" formatında; aynı hat+saat 409.
/// Faz 12.5: <paramref name="OperatingDays"/> — 7 bitlik gün maskesi, varsayılan "her gün".
/// </summary>
/// <remarks>
/// ⚠️ Mükerrer denetimi <b>bilerek günlere bakmıyor</b>: aynı hatta iki ayrı "08:00" satırı,
/// günleri farklı olsa bile mobil listede <b>iki kez 08:00</b> olarak görünürdü. Günü değişen
/// sefer yeni satır açmaz, var olanı <see cref="UpdateIntercityScheduleCommand"/> ile düzenler.
/// </remarks>
public record CreateIntercityScheduleCommand(
    Guid RouteId, string DepartureTime, int OperatingDays = Domain.Enums.OperatingDays.Daily)
    : IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "transport";
    public string AuditAction => "create-schedule";
    public Guid? AuditAffectedId => RouteId;
    public string? AuditAffectedType => "IntercitySchedule";
    public object? AuditDetails => new { departureTime = DepartureTime, operatingDays = OperatingDays };
}

public class CreateIntercityScheduleCommandHandler : IRequestHandler<CreateIntercityScheduleCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateIntercityScheduleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateIntercityScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!TimeSpan.TryParse(request.DepartureTime, out var time) || time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
            throw new AppException("Kalkış saati 'HH:mm' formatında olmalıdır.", "VALIDATION_ERROR");

        var days = TransportScheduleRules.ValidateDays(request.OperatingDays);

        var routeExists = await _uow.Repository<IntercityRoute>().Query()
            .AnyAsync(x => x.Id == request.RouteId, cancellationToken);
        if (!routeExists)
            throw new NotFoundException(nameof(IntercityRoute), request.RouteId);

        var duplicate = await _uow.Repository<IntercitySchedule>().Query()
            .AnyAsync(x => x.RouteId == request.RouteId && x.DepartureTime == time, cancellationToken);
        if (duplicate)
            throw new ConflictException("Bu hatta aynı kalkış saati zaten tanımlı.");

        var schedule = new IntercitySchedule
        {
            RouteId = request.RouteId,
            DepartureTime = time,
            OperatingDays = days.Mask,
            IsActive = true
        };
        await _uow.Repository<IntercitySchedule>().AddAsync(schedule, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return schedule.Id;
    }
}

/// <summary>
/// Faz 12.5 — var olan bir seferin <b>günlerini / saatini / yayın durumunu</b> düzenler.
/// </summary>
/// <remarks>
/// 🔑 Neden var: 12.5 öncesinde seferin tek düzenleme yolu <b>sil + yeniden ekle</b>ydi.
/// Gün maskesi eklendiği anda bu kabul edilemez hâle geldi — "Pazar seferini kaldır"
/// demek için yöneticinin saati silip yeniden yazması gerekirdi ve denetim izinde bu bir
/// <i>silme</i> olarak görünürdü.
///
/// ⚠️ Aksiyon adı <c>UpdateSchedule</c>: izin eylemi aksiyon adının <b>önekinden</b> türetilir
/// (görünmez sözleşme #19) — <c>Update…</c> → <c>update</c>, doğru olan bu.
/// </remarks>
public record UpdateIntercityScheduleCommand(Guid Id, string DepartureTime, int OperatingDays, bool IsActive)
    : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "transport";
    public string AuditAction => "update-schedule";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "IntercitySchedule";
    public object? AuditDetails => new { departureTime = DepartureTime, operatingDays = OperatingDays, isActive = IsActive };
}

public class UpdateIntercityScheduleCommandHandler : IRequestHandler<UpdateIntercityScheduleCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateIntercityScheduleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateIntercityScheduleCommand request, CancellationToken cancellationToken)
    {
        if (!TimeSpan.TryParse(request.DepartureTime, out var time) || time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
            throw new AppException("Kalkış saati 'HH:mm' formatında olmalıdır.", "VALIDATION_ERROR");

        var days = TransportScheduleRules.ValidateDays(request.OperatingDays);

        var repo = _uow.Repository<IntercitySchedule>();
        // ⚠️ Query() varsayılan olarak AsNoTracking — değiştireceğimiz için tracking şart
        // (12.3'te SoftRemove bu yüzden sessizce kaybolmuştu).
        var schedule = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (schedule == null) return false;

        var duplicate = await repo.Query()
            .AnyAsync(x => x.RouteId == schedule.RouteId && x.DepartureTime == time && x.Id != schedule.Id,
                cancellationToken);
        if (duplicate)
            throw new ConflictException("Bu hatta aynı kalkış saati zaten tanımlı.");

        schedule.DepartureTime = time;
        schedule.OperatingDays = days.Mask;
        schedule.IsActive = request.IsActive;

        repo.Update(schedule);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Faz 12.5 — sefer doğrulamalarının ortak yeri (Create ve Update aynı kuralı kullanır).</summary>
internal static class TransportScheduleRules
{
    public const string NoDayMessage =
        "Sefer için en az bir gün seçilmelidir — hiçbir gün çalışmayan sefer mobilde hiç görünmez.";

    /// <summary>
    /// 🔴 <c>OperatingDays = 0</c> yasak. Hiçbir gün çalışmayan bir sefer panelde <i>duran</i>
    /// ama mobilde <i>hiç görünmeyen</i> bir kayıttır: yönetici saati girdiğini sanır,
    /// vatandaş hiçbir zaman göremez ve <b>kimse hata almaz</b>.
    /// </summary>
    public static Domain.Enums.OperatingDays ValidateDays(int mask)
    {
        var days = new Domain.Enums.OperatingDays(mask);
        if (!days.IsValid)
            throw new AppException(NoDayMessage, "VALIDATION_ERROR");
        return days;
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
