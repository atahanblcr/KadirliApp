using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>Kesinti kaydının bildirim tarafına ne olduğu.</summary>
public enum PowerOutageNotifyOutcome
{
    /// <summary>Yönetici bildirim istemedi.</summary>
    NotRequested,

    /// <summary>
    /// Kesintinin sözlükte bir mahallesi yok → hedeflenemez. 🔴 Sessizce "gönderildi"
    /// denmez; panel bunu buton kapalı + açıklama olarak söyler.
    /// </summary>
    NotTargetable,

    /// <summary>Duyuru üretildi ve bildirimler yazıldı.</summary>
    Created,

    /// <summary>Duyuru zaten vardı, metni/süresi tazelendi — <b>ikinci bildirim üretilmedi</b>.</summary>
    Updated
}

/// <param name="Outcome">Ne olduğu.</param>
/// <param name="AnnouncementId">Kesintiye bağlanan duyuru (yoksa <c>null</c>).</param>
/// <param name="RecipientCount">Bu çağrıda yazılan bildirim satırı sayısı (güncellemede 0).</param>
public sealed record PowerOutageAnnouncementResult(
    PowerOutageNotifyOutcome Outcome, Guid? AnnouncementId, int RecipientCount);

/// <summary>
/// Faz 12.3 — <b>kesinti ↔ duyuru bağının TEK sahibi.</b>
/// </summary>
/// <remarks>
/// 🔑 <b>Kesinti bildirimi ayrı bir bildirim türü değil, bir DUYURUDUR.</b> Faz başında
/// bilinçli olarak seçildi: <c>Announcement</c> + <c>AnnouncementNotificationGenerator</c> +
/// <c>SendPushNotificationsJob</c> + mobil deep-link zinciri <b>aynen</b> çalışıyor, yani
/// mobilde tek satır değişmeden mağazadaki eski sürümler de kesinti bildirimini alıyor.
/// Yeni bir <c>relatedType</c> uydurulsaydı görünmez sözleşme #18 gereği eski sürümler
/// bildirime dokunduğunda <b>sessizce hiçbir yere gitmezdi</b>.
///
/// 🔴 Üç komut da (oluştur/güncelle/sil) buradan geçmek zorunda. İkinci bir gerçekleme
/// yazılırsa "güncelleme ikinci duyuru üretti" ya da "silinen kesintinin duyurusu ayakta
/// kaldı" sınıfından sessiz hatalar doğar — ikincisi 11.15c'de gerçekten yaşandı
/// (9 ölü bildirim, "dokun → boş sayfa").
/// </remarks>
public interface IPowerOutageAnnouncementWriter
{
    /// <summary>
    /// Kesintinin duyurusunu istenen hâle getirir. <b>Kaydetmez</b> — çağıran
    /// <c>SaveChangesAsync</c> çağırmış olmalıdır ya da sonrasında çağırır (ayrıntı gerçeklemede).
    /// </summary>
    /// <param name="outage">Kesinti (kaydedilmiş olmalı — <c>Id</c> gerekiyor).</param>
    /// <param name="sendNotification">Yönetici bildirim istedi mi.</param>
    /// <param name="targetNeighborhoodIds">
    /// Bildirimin gideceği ek mahalleler. Kesintinin kendi mahallesi <b>her zaman</b> dâhildir;
    /// bu liste onu genişletir (bir trafo arızası komşu mahalleyi de karartabilir).
    /// </param>
    /// <param name="createdBy">İşlemi yapan yönetici.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<PowerOutageAnnouncementResult> SyncAsync(
        PowerOutage outage,
        bool sendNotification,
        IReadOnlyList<Guid>? targetNeighborhoodIds,
        Guid? createdBy,
        CancellationToken ct = default);

    /// <summary>
    /// Kesinti silinirken duyurusunu <b>ve onun bildirimlerini</b> temizler.
    /// </summary>
    /// <remarks>
    /// 🔴 Görünmez sözleşme #24'ün uzantısı: bildirim türetilmiş veridir, kaynağı yok olunca
    /// saklanmasının bir anlamı yok. Kalsalardı kullanıcı bildirime dokunup <b>boş sayfaya</b>
    /// düşerdi — 11.15c'de duyurularda tam olarak bu yaşandı.
    /// </remarks>
    Task RemoveAsync(PowerOutage outage, CancellationToken ct = default);
}
