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

    /// <summary>
    /// Faz 12.1 — son 24 saatte görülmüş, hâlâ açık hata kaydı sayısı.
    /// <b>null = kullanıcı bunu görmemeli</b> (moderatör): hata kayıtları ekranı yalnız
    /// admin'e açık, dolayısıyla sayacı da yalnız admin görür. Sıfır ile null farklı
    /// şeylerdir — sıfırda "0 hata" rozeti çizilir (iyi haber), null'da hiç çizilmez.
    /// </summary>
    public int? OpenErrorCount { get; set; }

    /// <summary>
    /// Faz 12.2 — son 24 saatte şüpheli işaretlenmiş giriş denemesi sayısı.
    /// <b>null = kullanıcı bunu görmemeli</b> (moderatör), <see cref="OpenErrorCount"/>
    /// ile aynı kural ve aynı gerekçe.
    /// </summary>
    public int? SuspiciousLoginCount { get; set; }

    /// <summary>
    /// Faz 12.2b — en son bildirim gönderimi ("son gönderim: N/M teslim").
    /// <b>null iki farklı şey demek olabilir</b> ve ikisi de kartı gizler: kullanıcı bunu
    /// görmemeli (moderatör) ya da hiç gönderim yapılmamış. Ayrım gerekmiyor — ikisinde de
    /// gösterilecek bir teslim yok. (Hata/şüpheli rozetinde ayrım gerekiyordu: orada "sıfır"
    /// bir iyi haberdi, burada "hiç gönderim yok" bir haber değil.)
    /// </summary>
    public Application.Features.PushCampaigns.Dtos.PushCampaignResponseDto? LastPushCampaign { get; set; }

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
