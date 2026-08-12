using System;
using System.Linq.Expressions;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Notifications;

/// <summary>
/// Faz 12.15b — <b>"bu gönderim hangi bildirim tercihine tabidir?" sorusunun tek sahibi.</b>
/// </summary>
/// <remarks>
/// 🔴 <b>Neden var:</b> 12.15b öncesinde <c>NotificationDispatcher</c> <b>her kaynağı</b>
/// <c>NotificationPreferences.Announcements</c>'a bağlıyordu. Bu, haber bildirimi eklendiği
/// an iki yönlü sessiz bir hataya dönüştü:
/// <list type="number">
///   <item>"Duyurular"ı kapatan kullanıcı <b>haberleri de</b> kaybediyordu — ayar ekranı
///         bunu söylemiyordu.</item>
///   <item>Haber istemeyen kullanıcının <b>tek çıkışı</b> "Duyurular"ı kapatmaktı, o da
///         <b>kesinti bildirimini</b> öldürüyordu (§7 madde 41: kesinti bir duyurudur).</item>
/// </list>
///
/// 🔑 <b>Eşleme neden burada, dispatcher'ın içinde değil:</b> aynı cevabı <b>iki yer</b>
/// vermek zorunda — gerçek gönderim ve panelin "kaç kişiye gidecek" önizlemesi
/// (<c>EstimateRecipientsAsync</c>). Dispatcher'ın gövdesine gömülseydi önizleme onu
/// çağırmayı unutabilirdi ve §7 madde 38'in tam olarak uyardığı hasar doğardı: panel
/// "342 kişiye gidecek" der, gönderim 280 satır yazar ve <b>fark hiçbir yerde görünmez</b>.
///
/// ⚠️ <b>Bilinmeyen kaynak <c>Announcements</c>'a düşer, "süzme"ye DEĞİL.</b> Bu, projenin
/// başka yerlerdeki *"şüphede kalınca göster"* kuralının (§5) <b>tersi</b> ve bilinçli:
/// orada bedel bir kaydın görünmemesi, burada bedel <b>tercihini kapatmış birine bildirim
/// göndermek</b>. Ayrıca varsayılanın bugünkü davranışla aynı olması, yarın eklenecek bir
/// kaynağın var olan sözleşmeyi sessizce genişletmesini engelliyor.
/// </remarks>
public static class PushPreferenceTopics
{
    /// <summary>
    /// Kaynağın tabi olduğu tercih — <b>EF'e çevrilebilir</b> bir ifade olarak.
    /// </summary>
    /// <remarks>
    /// İfade döndürmesi zorunlu: alıcı kümesi <c>IQueryable&lt;User&gt;</c> üzerinde
    /// süzülüyor. <c>Func</c> döndürseydi süzgeç <b>belleğe</b> düşer ve 27 bin kullanıcılık
    /// bir tabloyu her gönderimde tam tarardık.
    /// </remarks>
    public static Expression<Func<User, bool>> For(string? source) => source switch
    {
        // 🔑 Haber, 12.15b'de açılan KENDİ ekseni.
        PushCampaignSources.News => u => u.NotificationPreferences.News,

        // ⚠️ Kesinti bilinçli olarak "duyuru"ya bağlı ve öyle KALMALI: kesinti bildirimi
        // ayrı bir tür değil, bir Duyuru'dur (§7 madde 41). Kendi eksenine taşımak, bugün
        // kesinti bildirimi alan kullanıcıların bir kısmını sessizce susturmak olurdu —
        // ve bu modülde susan bildirim, vatandaşın elektriğinin ne zaman kesileceğini
        // öğrenememesi demek.
        PushCampaignSources.PowerOutage => u => u.NotificationPreferences.Announcements,

        // Panelin tek seferlik gönderimi: gidilecek bir kaydı yok, genel bir şehir mesajı.
        PushCampaignSources.Manual => u => u.NotificationPreferences.Announcements,

        PushCampaignSources.Announcement => u => u.NotificationPreferences.Announcements,

        // Bilinmeyen kaynak → bugünkü davranış (yukarıdaki uyarı).
        _ => u => u.NotificationPreferences.Announcements
    };
}
