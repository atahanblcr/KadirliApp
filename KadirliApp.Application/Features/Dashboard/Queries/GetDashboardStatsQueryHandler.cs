using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Dashboard.Queries;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IUnitOfWork _uow;

    public GetDashboardStatsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var pendingAds = await _uow.Repository<Ad>().Query().CountAsync(a => a.Status == "pending", ct);
        var pendingDeaths = await _uow.Repository<DeathNotice>().Query().CountAsync(d => d.Status == "pending", ct);
        var pendingEvents = await _uow.Repository<Event>().Query().CountAsync(e => e.Status == "pending", ct);
        var pendingCampaigns = await _uow.Repository<Campaign>().Query().CountAsync(c => c.Status == "pending", ct);
        var pendingComplaints = await _uow.Repository<Complaint>().Query().CountAsync(c => c.Status == "pending", ct);

        var now = DateTime.UtcNow;
        var last7Days = now.AddDays(-7);

        // ⚠️ Faz 11.15c: "Aktif" sayaçları VATANDAŞIN GÖRDÜĞÜ tanıma bağlıdır.
        // Önceki hâlde ActiveAds yalnız Status == "approved" sayıyordu; süresi dolmuş ama
        // ExpireAdsJob'ın (saatlik) henüz dokunmadığı ilanlar da "aktif" görünüyordu.
        // Canlı denetimde panel 1 derken GET /v1/ads 0 döndürdü — yönetici ile vatandaş
        // farklı gerçeklik görüyordu. Süzgeçler public sorgularla birebir aynı:
        //   GetAdsQueryHandler:32           → Status == "approved" && ExpiresAt > now
        //   GetAnnouncementsQuery:46        → Status == "active" && (VisibleUntil == null || > now)
        // (Public uçların süzgeci değişirse buranın da değişmesi gerekir; DashboardStatsTests kilitliyor.)
        return new DashboardStatsDto
        {
            TotalUsers = await _uow.Repository<User>().Query().CountAsync(ct),
            ActiveAds = await _uow.Repository<Ad>().Query()
                .CountAsync(a => a.Status == "approved" && a.ExpiresAt > now, ct),
            TotalAnnouncements = await _uow.Repository<Announcement>().Query()
                .CountAsync(a => a.Status == "active" && (a.VisibleUntil == null || a.VisibleUntil > now), ct),
            PendingApprovals = pendingAds + pendingDeaths + pendingEvents + pendingCampaigns + pendingComplaints,
            PendingBreakdown = new PendingBreakdownDto
            {
                Ads = pendingAds,
                Deaths = pendingDeaths,
                Events = pendingEvents,
                Campaigns = pendingCampaigns,
                Complaints = pendingComplaints
            },
            NewUsersLast7Days = await _uow.Repository<User>().Query().CountAsync(u => u.CreatedAt >= last7Days, ct),
            TaxiCallsLast7Days = await _uow.Repository<TaxiCall>().Query().CountAsync(c => c.CalledAt >= last7Days, ct),
            NewAdsLast7Days = await _uow.Repository<Ad>().Query().CountAsync(a => a.CreatedAt >= last7Days, ct),
            TotalAnnouncementViews = await _uow.Repository<Announcement>().Query().SumAsync(a => a.ViewCount, ct)
        };
    }
}
