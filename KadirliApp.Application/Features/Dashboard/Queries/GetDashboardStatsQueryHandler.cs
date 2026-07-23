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

        var last7Days = DateTime.UtcNow.AddDays(-7);

        return new DashboardStatsDto
        {
            TotalUsers = await _uow.Repository<User>().Query().CountAsync(ct),
            ActiveAds = await _uow.Repository<Ad>().Query().CountAsync(a => a.Status == "approved", ct),
            TotalAnnouncements = await _uow.Repository<Announcement>().Query().CountAsync(ct),
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
