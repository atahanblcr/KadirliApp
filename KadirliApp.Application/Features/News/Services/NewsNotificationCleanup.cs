using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Services;

/// <summary>
/// Faz 12.15 — <b>haber görünmez olduğunda bildirimlerini de düşüren tek yer.</b>
/// </summary>
/// <remarks>
/// 🔴 <b>Neden zorunlu:</b> bildirim <b>türetilmiş</b> veridir; hedefi yaşamıyorsa kendisi de
/// yaşamamalı (§7 madde 24). Bu proje bunu canlıda ödedi: 11.15c'de silinen bir duyurunun
/// <b>9 ölü bildirimi</b> ayakta kaldı ve vatandaş bildirime dokunup <b>boş sayfaya</b>
/// düştü. Kesinti modülü aynı temizliği 12.3'te yaptı (§7 madde 41), haber modülünün
/// karşılığı burası.
///
/// 🔑 <b>Silme FİZİKSEL</b> (soft değil): satırın kendisi bir kaynağın gölgesi; kaynağı
/// görünmezken "silinmiş bildirim" diye bir kavramın kimseye faydası yok.
///
/// ⚠️ <b>Kampanya satırına DOKUNULMAZ</b> ve bu ayrım bilinçli: kampanya "ne yollandı"
/// tarihçesidir (§7 madde 37/39). Silinseydi <i>"bu haberi şehre biz mi duyurmuştuk?"</i>
/// sorusunun cevabı kaybolurdu — üstelik haberin <c>NotificationSentAt</c> damgası duruyor,
/// yani kampanyasız bir "gönderildi" izi panelde <b>ucu açık bir bağlantıya</b> dönerdi.
///
/// ⚠️ <b>Haberin "gönderildi" işareti de DÜŞMEZ.</b> Geri alındığında ikinci bir push
/// atılmamalı: FCM'e iletilmiş mesaj geri çağrılamaz (terminal), yani "hiç gönderilmemiş"
/// durumuna dönmek yalan olurdu ve panel o yalana bakıp şehre ikinci kez yazardı.
///
/// 📌 <b>Bilinçli sınır:</b> haber bir <i>kategori dışlaması</i> yüzünden görünmez olduğunda
/// bildirimleri <b>silinmez</b>. O eksen geri alınabilir ve toplu (tek anahtar yüzlerce haberi
/// birden etkiler); yüzlerce kullanıcının bildirim listesini bir anahtarın çevrilmesiyle
/// budamak, çözdüğünden çok şey bozardı. Bedeli: o aralıkta bildirime dokunan kullanıcı
/// "haber bulunamadı" görür — <c>gone</c> durumunun zaten kabul edilmiş davranışı.
/// </remarks>
public static class NewsNotificationCleanup
{
    /// <summary>
    /// Bu habere bağlı bildirim satırlarını siler. <b>Kaydetmez</b> — çağıranın
    /// <c>SaveChanges</c>'ine katılır.
    /// </summary>
    /// <remarks>
    /// 🔑 Kaydetmemesi bilinçli: hem arşivleme komutu hem de mutabakat işi bunu kendi
    /// işleminin <b>içinde</b> çağırıyor. Burada kaydetseydi mutabakat işi tek koşuda
    /// yüzlerce kez <c>SaveChanges</c> çağırır ve yarıda kesilen bir koşu kaydı
    /// <i>bildirimleri silinmiş ama hâlâ yayında</i> bırakırdı.
    /// </remarks>
    /// <returns>Silinen satır sayısı.</returns>
    public static async Task<int> RemoveDeliveredAsync(IUnitOfWork uow, Guid articleId, CancellationToken ct = default)
    {
        var repo = uow.Repository<Notification>();

        var orphaned = await repo.Query()
            .Where(n => n.RelatedType == NewsNotifications.RelatedType && n.RelatedId == articleId)
            .ToListAsync(ct);

        foreach (var notification in orphaned)
            repo.Remove(notification);

        return orphaned.Count;
    }
}

/// <summary>
/// Haber bildiriminin <b>kontrat sabitleri</b> — tek yerde.
/// </summary>
/// <remarks>
/// 🔴 <c>RelatedType</c> bir <b>kontrattır</b> (§7 madde 18): mobil bu değerden
/// <c>/haberler/:id</c> rotasını üretiyor (<c>app_notification.dart</c>, 12.14'te yazıldı).
/// Yeniden adlandırılırsa mağazadaki bütün sürümler bildirime dokunduğunda <b>hiçbir yere
/// gitmez</b> ve hata da almaz. İkinci bir yerde string olarak yazılması, o gün tek satırı
/// güncelleyip diğerini unutmayı mümkün kılardı — silme temizliği de bu sabite bakıyor.
/// </remarks>
public static class NewsNotifications
{
    /// <summary>Mobilin deep-link eşlemesindeki anahtar. ⚠️ Değiştirilemez.</summary>
    public const string RelatedType = "news";
}
