using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Moderation;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public class UpdateDeathNoticeCommandHandler : IRequestHandler<UpdateDeathNoticeCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateDeathNoticeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateDeathNoticeCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<DeathNotice>();
        var notice = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (notice == null) return false;

        var dto = request.Dto;

        // Faz 12.10 — moderasyon durumu bu yoldan yazılamaz (#52); guard ilk yazmadan ÖNCE.
        ModerationStatusGuard.EnsureUnchanged(notice.Status, dto.Status);

        notice.DeceasedName = dto.DeceasedName;
        notice.PhotoFileId = dto.PhotoFileId;
        notice.FuneralDate = DateTime.SpecifyKind(dto.FuneralDate, DateTimeKind.Utc);
        notice.FuneralTime = dto.FuneralTime;
        notice.CemeteryId = dto.CemeteryId;
        notice.MosqueId = dto.MosqueId;
        notice.NeighborhoodId = dto.NeighborhoodId;
        notice.CondolenceAddress = dto.CondolenceAddress;
        notice.CondolenceLatitude = dto.CondolenceLatitude;
        notice.CondolenceLongitude = dto.CondolenceLongitude;
        notice.AutoArchiveAt = DateTime.SpecifyKind(dto.FuneralDate, DateTimeKind.Utc).AddDays(7);

        repo.Update(notice);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
