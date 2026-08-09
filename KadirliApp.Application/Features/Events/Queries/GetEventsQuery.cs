using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Events.Queries;

public record GetEventsQuery(QueryEventDto Dto) : IRequest<PagedResult<EventResponseDto>>;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetEventsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<EventResponseDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<Event>().Query();

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == dto.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(dto.Status))
            query = query.Where(x => x.Status == dto.Status);

        if (dto.StartDate.HasValue)
        {
            var start = System.DateTime.SpecifyKind(dto.StartDate.Value.Date, System.DateTimeKind.Utc);
            query = query.Where(x => x.EventDate >= start);
        }

        if (dto.EndDate.HasValue)
        {
            var end = System.DateTime.SpecifyKind(dto.EndDate.Value.Date, System.DateTimeKind.Utc).AddDays(1);
            query = query.Where(x => x.EventDate < end);
        }

        if (dto.IsFree.HasValue)
            query = query.Where(x => x.IsFree == dto.IsFree.Value);

        // Faz 12.4 — konum. İlçe süzgeci tek bir kaydı, kapsam ise bir bölgeyi seçer;
        // ikisi birlikte verilebilir (panelde "Osmaniye" + "Düziçi" gibi bir daralma).
        if (dto.DistrictId.HasValue)
            query = query.Where(x => x.DistrictId == dto.DistrictId.Value);

        query = EventLocationScopes.Apply(query, EventLocationScopes.Parse(dto.LocationScope, dto.OnlyLocal));

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var search = dto.Search.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                (x.VenueName != null && x.VenueName.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        // Faz 11.18: elle yazılmış iki dallı sıra ortak SortMap'e taşındı (başlık sıralaması
        // için title_asc/title_desc de eklendi). ⚠️ Davranış korundu: varsayılan hâlâ
        // "en ileri tarih önce" ve bilinmeyen değer hâlâ varsayılana düşüyor (11.10 sözleşmesi,
        // QueryEventDto'da yazılı) — SortMap'in rejectUnknown'ı bu modülde bilerek KAPALI.
        query = Common.Sorting.PanelSorts.Events.Apply(query, dto.Sort);

        var rows = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(EventProjection.Select)
            .ToListAsync(cancellationToken);

        var items = rows.Select(EventProjection.Finish).ToList();

        return new PagedResult<EventResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
