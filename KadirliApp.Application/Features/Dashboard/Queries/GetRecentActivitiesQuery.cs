using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Dashboard.Queries;

public record GetRecentActivitiesQuery(int Limit = 8)
    : IRequest<List<RecentActivityDto>>, ICacheableQuery
{
    public string CacheKey => $"dashboard:recent:{Limit}";
    public string CacheGroup => CacheGroups.Dashboard;
    public TimeSpan CacheDuration => TimeSpan.FromSeconds(60);
}

public class RecentActivityDto
{
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class GetRecentActivitiesQueryHandler : IRequestHandler<GetRecentActivitiesQuery, List<RecentActivityDto>>
{
    private readonly IUnitOfWork _uow;

    public GetRecentActivitiesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<RecentActivityDto>> Handle(GetRecentActivitiesQuery request, CancellationToken ct)
    {
        var ads = await _uow.Repository<Ad>().Query()
            .OrderByDescending(a => a.CreatedAt).Take(request.Limit)
            .Select(a => new RecentActivityDto { Type = "ad", Title = a.Title, CreatedAt = a.CreatedAt })
            .ToListAsync(ct);

        var announcements = await _uow.Repository<Announcement>().Query()
            .OrderByDescending(a => a.CreatedAt).Take(request.Limit)
            .Select(a => new RecentActivityDto { Type = "announcement", Title = a.Title, CreatedAt = a.CreatedAt })
            .ToListAsync(ct);

        return ads.Concat(announcements)
            .OrderByDescending(x => x.CreatedAt)
            .Take(request.Limit)
            .ToList();
    }
}
