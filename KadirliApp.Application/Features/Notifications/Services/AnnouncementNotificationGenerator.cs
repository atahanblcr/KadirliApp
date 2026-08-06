using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Notifications.Services;

/// <summary>
/// Faz 10.10 kararları:
/// - SendPushNotification=false ise bildirim SATIRI DA yazılmaz (plandaki iki seçenekten ilki —
///   böylece 10.11 FCM job'ı "fcm_sent=false olan her satır push'lanabilir" varsayımıyla sade kalır).
/// - İdempotency işareti: duyuru başına kontrol — satır varsa üretim atlanır
///   (job iki kez koşarsa veya update tekrar tetiklerse mükerrer üretilmez).
///
/// 🔑 <b>Faz 12.2b: hedefleme mantığı buradan <see cref="NotificationDispatcher"/>'a taşındı.</b>
/// Mahalle süzgeci, bildirim tercihi ve gövde kırpması artık orada — çünkü manuel gönderim
/// (<c>PushCampaignsAdmin</c>) de aynı kuralları uygulamak zorunda. İkinci bir gerçekleme
/// yazılsaydı duyuru ile manuel gönderim <b>aynı mahalleye farklı kişi kümesi</b> yollardı ve
/// iki taraf da hiç hata vermezdi. Bu sınıfta kalan tek şey <b>duyuruya özgü</b> olan:
/// "push isteniyor mu" ve "bu duyuru daha önce üretildi mi".
/// </summary>
public class AnnouncementNotificationGenerator : IAnnouncementNotificationGenerator
{
    public const string RelatedTypeAnnouncement = "announcement";

    private readonly IUnitOfWork _uow;
    private readonly INotificationDispatcher _dispatcher;

    public AnnouncementNotificationGenerator(IUnitOfWork uow, INotificationDispatcher dispatcher)
    {
        _uow = uow;
        _dispatcher = dispatcher;
    }

    public async Task<int> GenerateForAnnouncementAsync(Announcement announcement, CancellationToken ct = default)
    {
        if (!announcement.SendPushNotification)
            return 0;

        if (await AlreadyGeneratedAsync(announcement.Id, ct))
            return 0;

        // ⚠️ Boş/null hedef "all" demektir — 10.10'dan beri böyle. Dispatcher'a null
        // geçilseydi hedefleme "tanınmayan tür" dalına düşerdi; sonuç aynı olurdu ama
        // kampanya satırına null bir hedef türü yazılırdı ve pano onu okuyamazdı.
        var targetType = string.IsNullOrWhiteSpace(announcement.TargetType)
            ? PushTargetTypes.All
            : announcement.TargetType;

        var neighborhoodIds = targetType == PushTargetTypes.Neighborhood
            ? ParseNeighborhoods(announcement.TargetNeighborhoods)
            : null;

        var result = await _dispatcher.DispatchAsync(new PushDispatchRequest(
            Title: announcement.Title,
            Body: announcement.Body,
            TargetType: targetType,
            NeighborhoodIds: neighborhoodIds,
            Source: PushCampaignSources.Announcement,
            SourceId: announcement.Id,
            CreatedBy: announcement.CreatedBy,
            NotificationType: RelatedTypeAnnouncement,
            RelatedType: RelatedTypeAnnouncement,
            RelatedId: announcement.Id), ct);

        return result.RecipientCount;
    }

    /// <summary>
    /// İki ayrı işaret sorulur ve bu <b>bilinçli</b>.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///   <item><b>Kampanya satırı</b> (12.2b'den itibaren): asıl kapı. Alıcı çıkmayan bir
    ///         duyuruda bildirim satırı hiç yazılmaz — yalnız bildirime baksaydık o duyuru
    ///         her çağrıda <b>yeni bir kampanya</b> açardı ve pano aynı duyuru için üst üste
    ///         "0 alıcı" satırlarıyla dolardı.</item>
    ///   <item><b>Bildirim satırı</b> (12.2b ÖNCESİ kayıtlar için): eski duyuruların
    ///         kampanyası yoktur ve geriye dönük uydurulmadı. Bu kontrol kalkarsa
    ///         <c>UpdateAnnouncement</c> ya da job eski bir duyuruya dokunduğunda
    ///         <b>bildirimler ikinci kez üretilir</b> — kullanıcı aynı duyuruyu iki kez alır.</item>
    /// </list>
    /// </remarks>
    private async Task<bool> AlreadyGeneratedAsync(Guid announcementId, CancellationToken ct)
    {
        var campaignExists = await _uow.Repository<PushCampaign>().Query()
            .AnyAsync(c => c.Source == PushCampaignSources.Announcement && c.SourceId == announcementId, ct);
        if (campaignExists) return true;

        return await _uow.Repository<Notification>().Query()
            .AnyAsync(n => n.RelatedType == RelatedTypeAnnouncement && n.RelatedId == announcementId, ct);
    }

    /// <summary>
    /// Mahalle listesini çözer. <c>null</c> ile <b>boş liste</b> farklı anlamlar taşır
    /// (bkz. <see cref="NotificationDispatcher"/>) ve bu metot ikisini ayırmakla yükümlü.
    /// </summary>
    /// <returns>
    /// <c>null</c> — kolon boş, yani hedefleme listesi <b>yok</b>: duyuru herkese gider
    /// (10.10'dan beri geçerli, testle yazılı kural).
    /// <br/>
    /// <b>boş liste</b> — kolon dolu ama çözülemedi: hiç kimseye gitmez.
    /// ⚠️ Bozuk JSON'da <c>null</c> dönmek, bir veri hatasını <b>tüm şehre giden bildirime</b>
    /// çevirirdi. Eski kod burada istisna fırlatıyordu; sessiz yayından iyisi ama
    /// duyuru kaydını da tamamen erişilemez kılıyordu.
    /// </returns>
    private static IReadOnlyList<Guid>? ParseNeighborhoods(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }
}
