using KadirliApp.Application.Features.Dashboard.Queries;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers.Admin;

[Route("v1/admin/dashboard")]
public class DashboardAdminController : AdminApiControllerBase
{
    /// <summary>KPI kartları: toplam kullanıcı, aktif ilan, bekleyen onaylar (modül kırılımıyla).</summary>
    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        return Success(await Sender.Send(new GetDashboardStatsQuery()));
    }

    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities([FromQuery] int limit = 8)
    {
        return Success(await Sender.Send(new GetRecentActivitiesQuery(limit)));
    }
}
