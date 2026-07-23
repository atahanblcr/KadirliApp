using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Deaths.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Deaths.Queries;

public class GetDeathNoticesQueryHandler : IRequestHandler<GetDeathNoticesQuery, PagedResult<DeathNoticeResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetDeathNoticesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<DeathNoticeResponseDto>> Handle(GetDeathNoticesQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<DeathNotice>().Query();

        if (dto.Date.HasValue)
        {
            var date = DateTime.SpecifyKind(dto.Date.Value.Date, DateTimeKind.Utc);
            query = query.Where(x => x.FuneralDate >= date && x.FuneralDate < date.AddDays(1));
        }

        // Faz 10.7 düzeltmesi: public liste bugüne dek status filtrelemiyordu — moderasyondan geçmemiş
        // (pending) kullanıcı ilanları yayındaydı. Public uçta istemcinin ?status= parametresi yok sayılır.
        if (request.OnlyPublished)
            query = query.Where(x => x.Status == "approved");
        else if (!string.IsNullOrWhiteSpace(dto.Status))
            query = query.Where(x => x.Status == dto.Status);

        if (!string.IsNullOrWhiteSpace(dto.Search))
            query = query.Where(x => x.DeceasedName.ToLower().Contains(dto.Search.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit,
            request.OnlyPublished ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        // DeathNotice → File navigation'ı yok (FK kısıtı da yok); URL correlated subquery ile çekiliyor.
        var files = _uow.Repository<Domain.Entities.File>().Query();

        var items = await query
            .OrderByDescending(x => x.FuneralDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
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
            .ToListAsync(cancellationToken);

        return new PagedResult<DeathNoticeResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
