using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Common;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Trash.Commands;

/// <summary>
/// Faz 11.17 — silinmiş kaydı geri getirir (<c>deleted_at = null</c>).
///
/// 🔑 <b>Geri getirme, yayına almak DEĞİLDİR.</b> Kayıt silinmeden önceki <c>status</c>'üyle
/// döner: onay bekleyen bir ilan yine <c>pending</c> olur, reddedilmiş olan <c>rejected</c>.
/// Aksi hâlde çöp kutusu, moderasyonu atlayan bir arka kapıya dönüşürdü — yasaklı bir ilan
/// silinip geri getirilerek yayına sokulabilirdi.
///
/// ⚠️ Kapsam <see cref="TrashModules"/>'da; ikinci bir <c>switch</c> yazılmamalı.
/// </summary>
public sealed record RestoreRecordCommand(string Module, Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => Module;
    public string AuditAction => "restore";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => TrashModules.Supported
        .FirstOrDefault(s => s.Module == Module).EntityType?.Name;
}

public sealed class RestoreRecordCommandHandler : IRequestHandler<RestoreRecordCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RestoreRecordCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(RestoreRecordCommand request, CancellationToken ct)
    {
        if (!TrashModules.IsSupported(request.Module))
            throw new AppException("Bu modülde geri getirme desteklenmiyor.", "VALIDATION_ERROR");

        return request.Module switch
        {
            "ads" => await RestoreAsync<Ad>(request.Id, ct),
            "announcements" => await RestoreAsync<Announcement>(request.Id, ct),
            "deaths" => await RestoreAsync<DeathNotice>(request.Id, ct),
            "events" => await RestoreAsync<Event>(request.Id, ct),
            "campaigns" => await RestoreAsync<Campaign>(request.Id, ct),
            "taxis" => await RestoreAsync<TaxiDriver>(request.Id, ct),
            _ => false
        };
    }

    private async Task<bool> RestoreAsync<TEntity>(Guid id, CancellationToken ct)
        where TEntity : BaseEntity, ISoftDeletable
    {
        // ⚠️ IgnoreQueryFilters şart: aradığımız kayıt tam olarak süzgecin gizlediği kayıt.
        // Tracking açık olmalı, yoksa değişiklik kaydedilmez.
        var entity = await _uow.Repository<TEntity>().Query(tracking: true)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt != null, ct);

        if (entity is null) return false; // zaten geri getirilmiş ya da hiç yok — iz de yazılmaz

        entity.DeletedAt = null;
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
