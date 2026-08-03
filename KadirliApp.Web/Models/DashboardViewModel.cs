namespace KadirliApp.Web.Models;

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveAds { get; set; }
    public int PendingApprovals { get; set; }
    public int TotalAnnouncements { get; set; }

    /// <summary>
    /// Faz 11.15c: onay kuyruğunun modül kırılımı. <c>PendingBreakdownDto</c> 10.10'dan beri
    /// hesaplanıyordu ama Web/Api'de hiç OKUNMUYORDU — "Bekleyen Onaylar" tıklanamayan tek
    /// bir sayıydı. Her satır ilgili modülün <c>?status=pending</c> filtresine gider.
    /// </summary>
    public List<PendingQueueItem> PendingQueue { get; set; } = new();

    // Faz 10.10-A: Etkileşim satırı
    public int NewUsersLast7Days { get; set; }
    public int TaxiCallsLast7Days { get; set; }
    public int NewAdsLast7Days { get; set; }
    public int TotalAnnouncementViews { get; set; }

    public List<ActivityItem> RecentActivities { get; set; } = new();
}

/// <summary>Dashboard onay kuyruğunun tek satırı: "İlanlar · 4 bekliyor" → AdsAdmin?status=pending.</summary>
public record PendingQueueItem(string Label, string Icon, int Count, string Controller, string StatusValue);

public class ActivityItem
{
    public string Type { get; set; } = default!;
    public string Icon { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
