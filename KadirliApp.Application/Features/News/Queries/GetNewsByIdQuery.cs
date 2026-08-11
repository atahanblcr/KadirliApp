using System;
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

/// <summary>Faz 12.12 — <c>GET /v1/news/{id}</c>. Gövde <b>yalnız burada</b> döner.</summary>
public record GetNewsByIdQuery(Guid Id) : IRequest<NewsArticleDto?>, ICacheableQuery
{
    public string CacheKey => $"news:detail:{Id}";
    public string CacheGroup => CacheGroups.News;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public class GetNewsByIdQueryHandler : IRequestHandler<GetNewsByIdQuery, NewsArticleDto?>
{
    private readonly IUnitOfWork _uow;

    public GetNewsByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NewsArticleDto?> Handle(GetNewsByIdQuery request, CancellationToken ct)
    {
        // Görünürlük detayda da zorlanır: arşivlenmiş ya da kaynaktan kalkmış bir haberin
        // bağlantısı elde kalmış olabilir (paylaşılmış link, eski push bildirimi).
        // Proje deseni (EventsController): sorgu null döner, controller 404'e çevirir.
        return await NewsVisibility
            .Published(_uow.Repository<NewsArticle>().Query())
            .Where(x => x.Id == request.Id)
            .Select(NewsProjection.Select(includeContent: true))
            .FirstOrDefaultAsync(ct);
    }
}
