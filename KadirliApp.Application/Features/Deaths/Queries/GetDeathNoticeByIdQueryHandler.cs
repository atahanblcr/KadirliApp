using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Deaths.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Deaths.Queries;

public class GetDeathNoticeByIdQueryHandler : IRequestHandler<GetDeathNoticeByIdQuery, DeathNoticeResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetDeathNoticeByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DeathNoticeResponseDto?> Handle(GetDeathNoticeByIdQuery request, CancellationToken cancellationToken)
    {
        // DeathNotice → File navigation'ı yok (FK kısıtı da yok); URL correlated subquery ile çekiliyor.
        var files = _uow.Repository<Domain.Entities.File>().Query();

        var notice = await _uow.Repository<DeathNotice>().Query()
            .Where(x => x.Id == request.Id)
            .Select(x => new DeathNoticeResponseDto(
                x.Id,
                x.DeceasedName,
                x.PhotoFileId,
                x.FuneralDate,
                x.FuneralTime,
                x.CemeteryId,
                x.Cemetery != null ? x.Cemetery.Name : null,
                x.MosqueId,
                x.Mosque != null ? x.Mosque.Name : null,
                x.NeighborhoodId,
                x.CondolenceAddress,
                x.CondolenceLatitude,
                x.CondolenceLongitude,
                x.AddedBy,
                x.Status,
                x.CreatedAt
            )
            {
                PhotoUrl = x.PhotoFileId != null
                    ? files.Where(f => f.Id == x.PhotoFileId).Select(f => f.CdnUrl).FirstOrDefault()
                    : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Faz 10.7 düzeltmesi: public uçta approved olmayan ilanı yalnız ekleyen görür (Ads detay emsali),
        // diğerlerine 404 — id bilinse bile pending/rejected/archived içerik sızmaz.
        if (notice is not null && request.OnlyPublished &&
            notice.Status != "approved" &&
            (request.RequesterId is null || notice.AddedBy != request.RequesterId))
            return null;

        return notice;
    }
}
