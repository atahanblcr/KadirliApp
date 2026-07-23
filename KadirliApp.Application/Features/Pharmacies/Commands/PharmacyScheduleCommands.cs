using KadirliApp.Application.Common.Auditing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Pharmacies.Commands;

/// <summary>
/// Faz 10.4: nöbet CRUD'u ilk kez Application'a çıkarıldı (önceden PharmacySchedule yalnız seeder'da kullanılıyordu).
/// İkisi de ICacheInvalidator — admin nöbet değiştirince on-duty/schedule/list cache'leri tazelenir.
/// </summary>
public sealed record CreatePharmacyScheduleCommand(CreatePharmacyScheduleDto Dto) : IRequest<Guid>, ICacheInvalidator, IAuditableCommand
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Pharmacies };

    // Nöbet ataması operasyonel olarak kritik — kim hangi güne atadı izlenir (AffectedId yanıt Guid'inden).
    public string AuditModule => "pharmacies";
    public string AuditAction => "assign-duty";
    public string? AuditAffectedType => "PharmacySchedule";
    public object? AuditDetails => new { pharmacyId = Dto.PharmacyId, dutyDate = Dto.DutyDate.ToString("yyyy-MM-dd") };
}

public sealed class CreatePharmacyScheduleCommandHandler : IRequestHandler<CreatePharmacyScheduleCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreatePharmacyScheduleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreatePharmacyScheduleCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var pharmacyExists = await _uow.Repository<Pharmacy>().Query()
            .AnyAsync(p => p.Id == dto.PharmacyId, cancellationToken);
        if (!pharmacyExists)
            throw new AppException("Geçersiz eczane.", "VALIDATION_ERROR");

        // duty_date konvansiyonu: amaçlanan yerel günün UTC gece yarısı (seeder'la aynı)
        var dutyDate = DateTime.SpecifyKind(dto.DutyDate.Date, DateTimeKind.Utc);
        var nextDay = dutyDate.AddDays(1);

        // Aynı gün birden çok eczane nöbetçi olabilir; aynı eczanenin aynı güne ikinci kaydı mükerrerdir
        var duplicate = await _uow.Repository<PharmacySchedule>().Query()
            .AnyAsync(s => s.PharmacyId == dto.PharmacyId && s.DutyDate >= dutyDate && s.DutyDate < nextDay,
                cancellationToken);
        if (duplicate)
            throw new ConflictException("Bu eczanenin o gün için zaten nöbet kaydı var.");

        var schedule = new PharmacySchedule
        {
            PharmacyId = dto.PharmacyId,
            DutyDate = dutyDate,
            Source = dto.Source
        };
        if (dto.StartTime is { } start) schedule.StartTime = start;
        if (dto.EndTime is { } end) schedule.EndTime = end;

        await _uow.Repository<PharmacySchedule>().AddAsync(schedule, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return schedule.Id;
    }
}

public sealed record DeletePharmacyScheduleCommand(Guid Id) : IRequest<bool>, ICacheInvalidator, IAuditableCommand
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Pharmacies };

    public string AuditModule => "pharmacies";
    public string AuditAction => "delete-duty";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "PharmacySchedule";
}

public sealed class DeletePharmacyScheduleCommandHandler : IRequestHandler<DeletePharmacyScheduleCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeletePharmacyScheduleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeletePharmacyScheduleCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<PharmacySchedule>();
        var schedule = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (schedule == null) return false;

        repo.Remove(schedule);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
