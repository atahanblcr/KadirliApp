using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Queries;

/// <summary>
/// Faz 12.12 — <c>GET /v1/news/categories</c>.
/// </summary>
/// <remarks>
/// ⚠️ <b>Dışlanmış kategori public uçta HİÇ görünmez</b> — göründüğü an vatandaş boş bir
/// süzgece dokunur ve "haber yok" sanır. <see cref="NewsCategoryDto.ArticleCount"/> de
/// kaynağınki değil <b>bizdeki görünür sayı</b>: kaynak "E-Gazete 366" derken bizde 0 kayıt
/// olabilir ve o rakam yalan olurdu.
/// </remarks>
public record GetNewsCategoriesQuery : IRequest<List<NewsCategoryDto>>, ICacheableQuery
{
    public string CacheKey => "news:categories";
    public string CacheGroup => CacheGroups.News;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetNewsCategoriesQueryHandler : IRequestHandler<GetNewsCategoriesQuery, List<NewsCategoryDto>>
{
    private readonly IUnitOfWork _uow;

    public GetNewsCategoriesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<NewsCategoryDto>> Handle(GetNewsCategoriesQuery request, CancellationToken ct)
    {
        // 🐛 12.12 sonrası denetim, bulgu 5: sayaç eskiden projeksiyonun İÇİNDE
        // (`visible.Count(a => a.Categories.Any(...))`) hesaplanıyordu ve bu, kategori başına
        // **ayrı bir korelasyonlu alt sorgu** üretiyordu — 15 kategori = 27k satır üzerinde
        // 15 COUNT. Önbellek bunu gizlemiyor: grubu **her senkron temizliyor** (15 dk'da bir).
        // Tek `GROUP BY` ile sayım, aynı sayıyı tek taramada verir.
        var counts = await NewsVisibility.Published(_uow.Repository<NewsArticle>().Query())
            .SelectMany(a => a.Categories)
            .GroupBy(c => c.Id)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

        var categories = await _uow.Repository<NewsCategory>().Query()
            .Where(c => !c.IsExcluded)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new NewsCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ShowInFilterStrip = c.ShowInFilterStrip,
                DisplayOrder = c.DisplayOrder
            })
            .ToListAsync(ct);

        foreach (var category in categories)
            category.ArticleCount = counts.TryGetValue(category.Id, out var count) ? count : 0;

        return categories;
    }
}
