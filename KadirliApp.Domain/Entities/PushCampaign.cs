using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.2b — **bir gönderim olayı**: "ne yollandı, kime yollandı, kaçına ulaştı?"
///
/// 12.2b öncesinde bu sorunun cevabı hiçbir yerde yoktu ve iki ayrı boşluktan doğuyordu:
/// <list type="number">
///   <item><b>Bildirimlerin panelde hiç ekranı yoktu</b> (<c>ARCHITECTURE.md</c> modül
///         tablosunda "Bildirimler → Panel <i>(yok)</i>"). FCM'in mesaj başına verdiği
///         cevap <see cref="Notification.FcmSent"/>/<c>FcmSentAt</c>/<c>FcmError</c>
///         alanlarında 10.11'den beri <b>saklanıyordu</b> — ama yalnız satır düzeyinde,
///         ve o satırlara bakan bir ekran yoktu. Yönetici "duyuruyu yayınladım, gitti mi?"
///         sorusuna ancak veritabanına girerek cevap verebiliyordu.</item>
///   <item><b>Serbest bir gönderimi gruplayacak anahtar yoktu.</b> Duyuru bildirimleri
///         <c>RelatedId</c> ile gruplanabiliyordu; ada hoc bir gönderimin tutunacağı
///         hiçbir şey yoktu — dolayısıyla bildirim satırı üreten tek yol
///         <c>AnnouncementNotificationGenerator</c>'dı ve yönetici tek seferlik bir push
///         atmak için <b>duyuru oluşturmak zorundaydı</b>.</item>
/// </list>
///
/// 🔑 <b>Bu satır artık her gönderimin çıpası:</b> duyuru da, manuel gönderim de (ve
/// 12.3'te kesinti de) önce bir kampanya açar, sonra <see cref="Notification"/> satırlarını
/// ona bağlar. <c>SendPushNotificationsJob</c> batch'i işlerken sayaçları buraya yazar.
///
/// 🔴 <b>Sayaçlar neden kolonda tutuluyor da sorgu anında <c>COUNT</c> ile hesaplanmıyor:</b>
/// her liste açılışında binlerce bildirim satırını <c>GROUP BY</c> ile saymak, panelin en
/// hızlı büyüyecek tablosunu tam tarar. Job zaten <c>sent</c>/<c>failed</c>/<c>invalidTokens</c>
/// değerlerini <b>elinde tutuyor</b> (bugün de hesaplıyor, yalnız log'a yazıp atıyordu) —
/// yazması bedava, sonradan saymak pahalı. ⚠️ Bunun bedeli, sayaçların artımlı yazılması
/// zorunluluğudur: "bir kez daha say, düzelir" yolu yoktur.
/// </summary>
public class PushCampaign : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;

    /// <summary>Hedef kitle: <c>all</c> · <c>neighborhood</c> (<c>PushTargetTypes</c>).</summary>
    public string TargetType { get; set; } = default!;

    /// <summary>
    /// <c>neighborhood</c> hedeflemesinde seçilen mahalle kimlikleri, JSON dizisi.
    /// Biçim <see cref="Announcement.TargetNeighborhoods"/> ile <b>birebir aynı</b> —
    /// hedefleme mantığının tek sahibi ikisini de aynı koddan okuyor.
    /// </summary>
    public string? TargetNeighborhoods { get; set; }

    /// <summary>Gönderimi doğuran şey: <c>announcement</c> · <c>power_outage</c> · <c>manual</c> · <c>news</c>.</summary>
    public string Source { get; set; } = default!;

    /// <summary>Kaynak kaydın kimliği (duyuru ya da haber id'si). Manuel gönderimde <c>null</c>.</summary>
    public Guid? SourceId { get; set; }

    /// <summary>Manuel gönderimde butona basan yönetici. Otomatik gönderimlerde <c>null</c>.</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Yazılan <see cref="Notification"/> satırı sayısı — yani mesajı <b>uygulama içinde</b>
    /// görecek kişi sayısı.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu sayı "push alacak kişi" sayısı DEĞİLDİR ve olmamalı: token'ı olmayan kullanıcı
    /// bildirimi listesinde görür, push'u ise ancak token kaydedince (belki yarın) alır.
    /// İkisini tek sayıya indirseydik pano ya "gönderilmedi" diye yalan söylerdi ya da
    /// uygulama içinde görülen bildirimleri saymazdı.
    /// </remarks>
    public int RecipientCount { get; set; }

    /// <summary>FCM'in kabul ettiği mesaj sayısı.</summary>
    public int SentCount { get; set; }

    /// <summary>FCM'in reddettiği mesaj sayısı. Sebep kırılımı <c>notifications.fcm_error</c>'da.</summary>
    public int FailedCount { get; set; }

    /// <summary>Kalıcı geçersiz token yüzünden <c>User.FcmToken</c>'ı temizlenen kullanıcı sayısı.</summary>
    public int InvalidTokenCount { get; set; }

    /// <summary>
    /// Gönderilebilir (token'ı olan) tüm satırlar işlendiğinde dolar.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Ölçüt "işlenen sayısı alıcı sayısına ulaştı" DEĞİL.</b> Öyle olsaydı pano
    /// asla tamamlanmazdı: <c>SendPushNotificationsJob</c> yalnız <c>User.FcmToken != null</c>
    /// olan satırları alır, token'ı olmayan alıcılar sonsuza kadar "bekliyor" görünürdü ve
    /// her kampanya kalıcı olarak yarım kalırdı. Ölçüt <b>"bu kampanyada gönderilebilir
    /// bekleyen satır kalmadı"</b>; token'ı olmayanlar bekler, kampanya yine tamamlanır ve
    /// biri yarın token kaydederse satırı gönderilir (kampanya tekrar açılmaz, sayaç artar).
    /// </remarks>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Plan dışı ek (12.2b): yönetici <b>henüz gönderilmemiş</b> satırları geri çektiğinde dolar.
    /// </summary>
    /// <remarks>
    /// 🔑 İptal, gönderimin <b>tersi değil sınırıdır</b>: FCM'e iletilmiş mesaj geri alınamaz
    /// (<c>FcmSent=true</c> terminaldir), ama henüz iletilmemiş satırlar hem push olmaktan
    /// hem de kullanıcının bildirim listesinde durmaktan çıkarılabilir. Yanlış metinle
    /// gönderilen bir bildirimin tek çaresi 12.2b'den önce veritabanına elle girmekti.
    ///
    /// ⚠️ <see cref="RecipientCount"/> iptalde <b>düşürülmez</b> — o sayı "kaç kişiye yazıldı"nın
    /// tarihçesidir. Düşürseydik pano iptal edilmiş bir kampanyayı hiç var olmamış gibi
    /// gösterir ve "neden 380 kişi bunu görmedi" sorusunun izi kaybolurdu.
    /// </remarks>
    public DateTime? CancelledAt { get; set; }
}

/// <summary>Kampanyanın hedef kitlesi. Değerler <see cref="Announcement.TargetType"/> ile aynı sözlükten.</summary>
public static class PushTargetTypes
{
    public const string All = "all";
    public const string Neighborhood = "neighborhood";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>(StringComparer.Ordinal) { All, Neighborhood };
}

/// <summary>Kampanyayı doğuran olay.</summary>
public static class PushCampaignSources
{
    /// <summary>Duyuru yayınlandı (10.10 akışı).</summary>
    public const string Announcement = "announcement";

    /// <summary>Elektrik kesintisi bildirimi (12.3'te bağlanacak — bugün üretilmiyor).</summary>
    public const string PowerOutage = "power_outage";

    /// <summary>Panelden elle gönderilen tek seferlik bildirim.</summary>
    public const string Manual = "manual";

    /// <summary>
    /// Faz 12.15 — panelden <b>bir habere</b> bağlı olarak gönderilen bildirim.
    /// </summary>
    /// <remarks>
    /// 🔑 <c>Manual</c>'dan ayrı bir kaynak olmak zorunda ve fark <c>SourceId</c>: elle
    /// gönderimin gidilecek bir kaydı yoktur (<c>RelatedType = null</c>), haber bildiriminin
    /// vardır. Aynı kova sayılsalardı pano "elle gönderim" derken bir kısmı deep-link taşırdı
    /// ve <b>"aynı haber ikinci kez gönderilmesin"</b> kuralının tutunacağı bir anahtar
    /// kalmazdı — kısmi unique indeks tam olarak <c>(source, source_id)</c> üzerinde duruyor.
    /// </remarks>
    public const string News = "news";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Announcement, PowerOutage, Manual, News };
}
