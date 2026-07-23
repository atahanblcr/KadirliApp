using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public class CreateDeathNoticeCommandHandler : IRequestHandler<CreateDeathNoticeCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateDeathNoticeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateDeathNoticeCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var notice = new DeathNotice
        {
            DeceasedName = dto.DeceasedName,
            PhotoFileId = dto.PhotoFileId,
            FuneralDate = DateTime.SpecifyKind(dto.FuneralDate, DateTimeKind.Utc),
            FuneralTime = dto.FuneralTime,
            CemeteryId = dto.CemeteryId,
            MosqueId = dto.MosqueId,
            NeighborhoodId = dto.NeighborhoodId,
            CondolenceAddress = dto.CondolenceAddress,
            CondolenceLatitude = dto.CondolenceLatitude,
            CondolenceLongitude = dto.CondolenceLongitude,
            AddedBy = request.AddedBy ?? Guid.Empty,
            Status = request.AutoApprove ? "approved" : "pending",
            ApprovedBy = request.AutoApprove ? request.AddedBy : null,
            ApprovedAt = request.AutoApprove ? DateTime.UtcNow : null,
            AutoArchiveAt = DateTime.SpecifyKind(dto.FuneralDate, DateTimeKind.Utc).AddDays(7)
        };

        await _uow.Repository<DeathNotice>().AddAsync(notice, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return notice.Id;
    }
}
