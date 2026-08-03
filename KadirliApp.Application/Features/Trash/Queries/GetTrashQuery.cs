using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Trash.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Trash.Queries;

/// <summary>
/// Faz 11.17 — çöp kutusu listesi. Soft delete her modülde vardı ama panelde karşılığı
/// yoktu: yanlışlıkla silinen bir duyuru <c>psql</c> olmadan geri gelmiyordu.
///
/// ⚠️ <b>Bellek sınırı bilinçli:</b> modül seçilmemişse altı tablodan da okunur, ama her
/// biri yalnız <c>sayfa × limit</c> kadar satır çeker; birleştirme ve sayfalama bellekte
/// yapılır. Tabloları filtresiz <c>ToListAsync()</c> etmek panelin en hızlı büyüyen
/// verisini belleğe alırdı (checklist §8, eski <c>UsersAdmin</c> hatası).
/// </summary>
public record GetTrashQuery(QueryTrashDto QueryDto) : IRequest<PagedResult<TrashItemDto>>;

public class GetTrashQueryHandler : IRequestHandler<GetTrashQuery, PagedResult<TrashItemDto>>
{
    private readonly IUnitOfWork _uow;

    public GetTrashQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<TrashItemDto>> Handle(GetTrashQuery request, CancellationToken ct)
    {
        var dto = request.QueryDto;
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        var wanted = TrashModules.Keys
            .Where(k => string.IsNullOrWhiteSpace(dto.Module) || k == dto.Module)
            .ToList();

        // Bilinmeyen modül anahtarı sessizce "her şey"e düşmemeli — kullanıcı süzdüğünü sanır.
        if (!string.IsNullOrWhiteSpace(dto.Module) && wanted.Count == 0)
            return Empty(page, limit);

        var take = page * limit; // birleştirmeden sonra doğru sayfayı kesebilmek için yeterli
        var items = new List<TrashItemDto>();
        var totalCount = 0;

        foreach (var module in wanted)
        {
            // ⚠️ Başlık ifadesi SQL'e çevrilecek — derlenmiş delege (`title.Compile()`) burada
            // çalışmaz, projeksiyon her modül için ayrı ayrı yazılır.
            var (rows, count) = module switch
            {
                "ads" => await ReadAsync<Ad>(take, ct,
                    e => new TrashItemDto { Module = "ads", Id = e.Id, Title = e.Title, DeletedAt = e.DeletedAt!.Value, CreatedAt = e.CreatedAt }),
                "announcements" => await ReadAsync<Announcement>(take, ct,
                    e => new TrashItemDto { Module = "announcements", Id = e.Id, Title = e.Title, DeletedAt = e.DeletedAt!.Value, CreatedAt = e.CreatedAt }),
                "deaths" => await ReadAsync<DeathNotice>(take, ct,
                    e => new TrashItemDto { Module = "deaths", Id = e.Id, Title = e.DeceasedName, DeletedAt = e.DeletedAt!.Value, CreatedAt = e.CreatedAt }),
                "events" => await ReadAsync<Event>(take, ct,
                    e => new TrashItemDto { Module = "events", Id = e.Id, Title = e.Title, DeletedAt = e.DeletedAt!.Value, CreatedAt = e.CreatedAt }),
                "campaigns" => await ReadAsync<Campaign>(take, ct,
                    e => new TrashItemDto { Module = "campaigns", Id = e.Id, Title = e.Title, DeletedAt = e.DeletedAt!.Value, CreatedAt = e.CreatedAt }),
                "taxis" => await ReadAsync<TaxiDriver>(take, ct,
                    e => new TrashItemDto { Module = "taxis", Id = e.Id, Title = e.Name, DeletedAt = e.DeletedAt!.Value, CreatedAt = e.CreatedAt }),
                _ => (new List<TrashItemDto>(), 0)
            };

            items.AddRange(rows);
            totalCount += count;
        }

        var pageItems = items
            .OrderByDescending(i => i.DeletedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        return new PagedResult<TrashItemDto>
        {
            Items = pageItems,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }

    /// <summary>
    /// ⚠️ <c>IgnoreQueryFilters()</c> şart: global soft-delete süzgeci tam olarak bu satırları
    /// gizliyor. Onsuz çöp kutusu **her zaman boş** görünürdü ve kimse hata almazdı.
    /// </summary>
    private async Task<(List<TrashItemDto> Rows, int Count)> ReadAsync<TEntity>(
        int take,
        CancellationToken ct,
        System.Linq.Expressions.Expression<Func<TEntity, TrashItemDto>> selector)
        where TEntity : Domain.Common.BaseEntity, Domain.Common.ISoftDeletable
    {
        var query = _uow.Repository<TEntity>().Query()
            .IgnoreQueryFilters()
            .Where(e => e.DeletedAt != null);

        var count = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(e => e.DeletedAt)
            .Take(take)
            .Select(selector)
            .ToListAsync(ct);

        return (rows, count);
    }

    private static PagedResult<TrashItemDto> Empty(int page, int limit) => new()
    {
        Items = new List<TrashItemDto>(),
        TotalCount = 0,
        CurrentPage = page,
        PageSize = limit
    };
}
