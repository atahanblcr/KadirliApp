using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Queries;

/// <summary>Faz 12.12 — <c>GET /v1/news</c>. Sayfalı liste; gövde <b>taşınmaz</b>.</summary>
public record GetNewsQuery(QueryNewsDto Dto) : IRequest<PagedResult<NewsArticleDto>>, ICacheableQuery
{
    public string CacheKey =>
        $"news:list:p{Dto.Page}:l{Dto.Limit}:s{Dto.Search}:c{Dto.CategoryId}:f{Dto.Featured}";

    public string CacheGroup => CacheGroups.News;

    /// <summary>
    /// 15 dk — artımlı senkron da 15 dk'da bir koşuyor. ⚠️ Asıl tazelik TTL'den değil
    /// <b>invalidation'dan</b> gelir: senkron ve panel yazmaları grubu temizler (§7 madde 22).
    /// </summary>
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetNewsQueryHandler : IRequestHandler<GetNewsQuery, PagedResult<NewsArticleDto>>
{
    private readonly IUnitOfWork _uow;

    public GetNewsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<NewsArticleDto>> Handle(GetNewsQuery request, CancellationToken ct)
    {
        var dto = request.Dto;
        var now = DateTime.UtcNow;

        // Görünürlük tanımı tek yerde (NewsVisibility) — controller ayrıca DTO'dan gelen
        // hiçbir bayrağa güvenmez (Değişmez Kural #3).
        var query = NewsVisibility.Published(_uow.Repository<NewsArticle>().Query());

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.Categories.Any(c => c.Id == dto.CategoryId.Value));

        // ⚠️ `false` sessizce yok sayılmıyor (12.12 sonrası denetim, bulgu 9): "öne çıkanlar"
        // bir eksense, "öne çıkmayanlar" da o eksenin diğer ucudur. Yok sayılsaydı panelden
        // ya da mobilden gönderilen `?featured=false` **tüm listeyi** döndürür ve yönetici
        // süzdüğünü sanırdı — hiçbir hata oluşmadan yanlış liste (§7 madde 37'nin sınıfı).
        if (dto.Featured == true)
            query = NewsVisibility.Featured(query, now);
        else if (dto.Featured == false)
            query = NewsVisibility.NotFeatured(query, now);

        // Desen `NewsSearch`'te kurulur (tek sahip). 🔬 Ölçüldü: `Contains` da Npgsql'de
        // `lower(...) LIKE @p` üretiyor — yani asıl kazanç sorgu şeklinde değil, GIN/trigram
        // ifade indeksinde (btree `LIKE '%x%'`'i karşılayamaz). Bkz. NewsSearch'ün dürüst notu.
        var pattern = NewsSearch.Pattern(dto.Search);
        if (pattern is not null)
        {
            // Başlıkta override varsa onda da aranır: yönetici başlığı düzelttiyse vatandaş
            // ekranda gördüğü metinle arayabilmeli.
            // ⚠️ `ToLower()` üç kolonda da şart — ifade indeksleri `lower(kolon)` üzerinde.
            query = query.Where(x =>
                (x.TitleOverride != null && EF.Functions.Like(x.TitleOverride.ToLower(), pattern)) ||
                EF.Functions.Like(x.SourceTitle.ToLower(), pattern) ||
                EF.Functions.Like(x.SourcePlainText.ToLower(), pattern));
        }

        var totalCount = await query.CountAsync(ct);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit);

        var items = await query
            // ⚠️ ThenBy(Id) şart: 27k kayıtta eşit yayın anı kesin var ve ayraçsız sıralamada
            // aynı kayıt iki sayfada görünüp bir başkası hiç görünmez (§7 madde 30).
            .OrderByDescending(x => x.SourcePublishedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(NewsProjection.Select(includeContent: false))
            .ToListAsync(ct);

        return new PagedResult<NewsArticleDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
