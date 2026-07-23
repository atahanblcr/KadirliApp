using KadirliApp.Application.Common.Caching;
using MediatR;

namespace KadirliApp.Application.Features.Dashboard.Queries;

// 8 COUNT sorgusunu her panel açılışında çalıştırmamak için kısa TTL ile cache'lenir;
// istatistikler birçok modülün yazmasıyla değiştiğinden invalidation yerine TTL yeterli.
public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>, ICacheableQuery
{
    public string CacheKey => "dashboard:stats";
    public string CacheGroup => CacheGroups.Dashboard;
    public TimeSpan CacheDuration => TimeSpan.FromSeconds(60);
}

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveAds { get; set; }
    public int TotalAnnouncements { get; set; }
    public int PendingApprovals { get; set; }
    public PendingBreakdownDto PendingBreakdown { get; set; } = new();

    // Faz 10.10-A (vizyon turu): "Etkileşim" satırı — üç 7-günlük nabız + bir toplam.
    // (Duyuru görüntülemede 7-günlük trend İMKANSIZ: announcement_views'ta timestamp yok — toplam kalır.)
    public int NewUsersLast7Days { get; set; }
    public int TaxiCallsLast7Days { get; set; }
    public int NewAdsLast7Days { get; set; }
    public int TotalAnnouncementViews { get; set; }
}

public class PendingBreakdownDto
{
    public int Ads { get; set; }
    public int Deaths { get; set; }
    public int Events { get; set; }
    public int Campaigns { get; set; }
    public int Complaints { get; set; }
}
