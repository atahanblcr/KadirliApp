using KadirliApp.Application.Features.Dashboard.Queries;
using KadirliApp.Infrastructure.Persistence;
using KadirliApp.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 11.15b: Dashboard, giriş sonrası **iniş sayfası**dır — moderatöre de açıktır.
/// Kapalı olsaydı moderatör panele girer girmez "Yetkiniz yok" ekranına düşer ve
/// izin verilen modüllerine hiç ulaşamazdı (çıkmaz sokak). Gösterdiği şey toplu sayaç;
/// modül içeriği değil.
/// </summary>
[Authorize(Roles = "admin,super_admin,moderator")]
public class DashboardController : Controller
{
    private readonly ISender _sender;
    private readonly AppDbContext _db; // yalnızca Seed için

    public DashboardController(ISender sender, AppDbContext db)
    {
        _sender = sender;
        _db = db;
    }

    // Faz 9.4: inline COUNT sorguları yerine Application query'leri — Admin API ile aynı
    // handler'lar kullanılır ve sonuç Redis'te 60 sn cache'lenir (CachingBehavior).
    public async Task<IActionResult> Index()
    {
        var stats = await _sender.Send(new GetDashboardStatsQuery());
        var recent = await _sender.Send(new GetRecentActivitiesQuery(8));

        var model = new DashboardViewModel
        {
            TotalUsers = stats.TotalUsers,
            ActiveAds = stats.ActiveAds,
            PendingApprovals = stats.PendingApprovals,
            TotalAnnouncements = stats.TotalAnnouncements,
            NewUsersLast7Days = stats.NewUsersLast7Days,
            TaxiCallsLast7Days = stats.TaxiCallsLast7Days,
            NewAdsLast7Days = stats.NewAdsLast7Days,
            TotalAnnouncementViews = stats.TotalAnnouncementViews,
            // Faz 11.15c: kırılım artık ekrana çıkıyor; sıfır olan modül satırı çizilmez
            // ("0 bekliyor" satırı gürültü, tıklanınca boş liste açar).
            PendingQueue = new List<PendingQueueItem>
            {
                new("İlanlar",       "fa-bullhorn",     stats.PendingBreakdown.Ads,        "AdsAdmin",        "pending"),
                new("Vefat İlanları","fa-book-dead",    stats.PendingBreakdown.Deaths,     "DeathsAdmin",     "pending"),
                new("Etkinlikler",   "fa-calendar-alt", stats.PendingBreakdown.Events,     "EventsAdmin",     "pending"),
                new("Kampanyalar",   "fa-percent",      stats.PendingBreakdown.Campaigns,  "CampaignsAdmin",  "pending"),
                new("Şikayetler",    "fa-flag",         stats.PendingBreakdown.Complaints, "ComplaintsAdmin", "pending"),
            }.Where(x => x.Count > 0).ToList(),
            RecentActivities = recent.Select(a => new ActivityItem
            {
                Type = a.Type == "ad" ? "İlan" : "Duyuru",
                Icon = a.Type == "ad" ? "fa-bullhorn" : "fa-bell",
                Title = a.Title,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

        return View(model);
    }

    /// <summary>
    /// ⚠️ Örnek veri basar — moderatöre KAPALI. İzin matrisinde karşılığı olmayan,
    /// veritabanına toplu yazan tek panel aksiyonu.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Seed()
    {
        try
        {
            await MockDataSeeder.SeedAsync(_db);
            TempData["Success"] = "Örnek veriler başarıyla eklendi. (Zaten dolu olan tablolara dokunulmadı.)";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Örnek veri eklenirken hata oluştu: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
