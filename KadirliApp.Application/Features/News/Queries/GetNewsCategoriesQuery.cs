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
        var visible = NewsVisibility.Published(_uow.Repository<NewsArticle>().Query());

        return await _uow.Repository<NewsCategory>().Query()
            .Where(c => !c.IsExcluded)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new NewsCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ArticleCount = visible.Count(a => a.Categories.Any(x => x.Id == c.Id)),
                ShowInFilterStrip = c.ShowInFilterStrip,
                DisplayOrder = c.DisplayOrder
            })
            .ToListAsync(ct);
    }
}
