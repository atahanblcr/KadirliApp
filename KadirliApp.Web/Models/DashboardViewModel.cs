namespace KadirliApp.Web.Models;

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveAds { get; set; }
    public int PendingApprovals { get; set; }
    public int TotalAnnouncements { get; set; }

    // Faz 10.10-A: Etkileşim satırı
    public int NewUsersLast7Days { get; set; }
    public int TaxiCallsLast7Days { get; set; }
    public int NewAdsLast7Days { get; set; }
    public int TotalAnnouncementViews { get; set; }

    public List<ActivityItem> RecentActivities { get; set; } = new();
}

public class ActivityItem
{
    public string Type { get; set; } = default!;
    public string Icon { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
