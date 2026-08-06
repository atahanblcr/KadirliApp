using System;

namespace KadirliApp.Application.Features.PushCampaigns;

/// <summary>Kampanya durumları — ham değerler. Türkçeleştirme panelde (<c>PanelDisplay</c>).</summary>
public static class PushCampaignStatuses
{
    /// <summary>Hedeflemeye uyan kimse çıkmadı; yazılan bildirim satırı yok.</summary>
    public const string Empty = "empty";

    /// <summary>Satırlar yazıldı, gönderim işi bu kampanyadan henüz hiçbir şey işlemedi.</summary>
    public const string Queued = "queued";

    /// <summary>Bir kısmı işlendi, bekleyen satır var.</summary>
    public const string Sending = "sending";

    /// <summary>Gönderilebilir bekleyen satır kalmadı.</summary>
    public const string Completed = "completed";

    /// <summary>Yönetici gönderilmemiş satırları geri çekti.</summary>
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Empty, Queued, Sending, Completed, Cancelled };
}

/// <summary>
/// Faz 12.2b — kampanya durumunun **tek türetme noktası** (saf, container'sız test edilir).
/// </summary>
/// <remarks>
/// 🔑 Durum ayrı bir kolonda tutulmuyor çünkü tutulsaydı <b>sayaçlarla ayrışabilirdi</b>:
/// job sayaçları artırıp durumu yazmayı unutsa pano "Kuyrukta" derken tablo 500 gönderim
/// gösterirdi — görünmez sözleşme #23'ün aynı sınıfı (iki taraf farklı gerçeklik görür).
/// Türetilmiş bir durum bu ayrışmayı <b>imkânsız</b> kılar.
///
/// ⚠️ Sıra önemlidir: <c>cancelled</c> her şeyin üstündedir (iptal edilmiş bir kampanya
/// "tamamlandı" diye okunmamalı), <c>empty</c> ondan sonra gelir (alıcı çıkmadıysa
/// gönderimden söz edilemez).
/// </remarks>
public static class PushCampaignStatus
{
    public static string Of(int recipientCount, int sentCount, int failedCount, DateTime? completedAt, DateTime? cancelledAt)
    {
        if (cancelledAt is not null) return PushCampaignStatuses.Cancelled;
        if (recipientCount == 0) return PushCampaignStatuses.Empty;
        if (completedAt is not null) return PushCampaignStatuses.Completed;
        return sentCount + failedCount > 0 ? PushCampaignStatuses.Sending : PushCampaignStatuses.Queued;
    }

    /// <summary>
    /// Henüz işlenmemiş satır sayısı.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu sayı sıfıra inmeden de kampanya <b>tamamlanabilir</b> ve bu bir hata değil:
    /// <c>SendPushNotificationsJob</c> yalnız <c>FcmToken</c>'ı olan kullanıcıların satırlarını
    /// alır. Token'ı olmayan alıcı bildirimi <b>uygulama içinde görür</b>, push'u ise ancak
    /// token kaydedince alır. Panel bu farkı yazıyla söyler — yoksa yönetici "12 kişiye
    /// ulaşmadı" diye okur, oysa o 12 kişi bildirimi görüyor.
    /// </remarks>
    public static int Pending(int recipientCount, int sentCount, int failedCount)
        => Math.Max(0, recipientCount - sentCount - failedCount);
}
