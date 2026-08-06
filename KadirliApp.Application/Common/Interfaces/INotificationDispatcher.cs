using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>Bir gönderim isteğinin tarif ettiği her şey.</summary>
/// <param name="Title">Bildirim başlığı.</param>
/// <param name="Body">Gövde — gerçekleme 500 karaktere kırpar (liste için özet yeter).</param>
/// <param name="TargetType"><c>PushTargetTypes</c>: <c>all</c> · <c>neighborhood</c>.</param>
/// <param name="NeighborhoodIds"><c>neighborhood</c> hedeflemesinde seçilen mahalleler.</param>
/// <param name="Source"><c>PushCampaignSources</c>: gönderimi doğuran olay.</param>
/// <param name="SourceId">Kaynak kaydın kimliği (duyuru id'si). Manuel gönderimde boş.</param>
/// <param name="CreatedBy">Manuel gönderimde butona basan yönetici.</param>
/// <param name="NotificationType"><c>notifications.type</c> — bildirimin görsel kimliği.</param>
/// <param name="RelatedType">
/// Deep-link hedefinin modülü. ⚠️ Görünmez sözleşme #18: mobil bu değerden <b>rota üretiyor</b>
/// ve tanımadığı türde gezinmeyi iptal ediyor. Manuel gönderimde bilinçli olarak <c>null</c> —
/// gidilecek bir kayıt yok, bildirim yalnız okunur (mobil bunu zaten "genel zil" ile karşılıyor).
/// </param>
/// <param name="RelatedId">Deep-link hedefinin kimliği.</param>
public sealed record PushDispatchRequest(
    string Title,
    string Body,
    string TargetType,
    IReadOnlyList<Guid>? NeighborhoodIds,
    string Source,
    Guid? SourceId = null,
    Guid? CreatedBy = null,
    string? NotificationType = null,
    string? RelatedType = null,
    Guid? RelatedId = null);

/// <param name="CampaignId">Açılan <c>PushCampaign</c> satırı.</param>
/// <param name="RecipientCount">Yazılan bildirim satırı sayısı (0 olabilir — ve bu bir bilgidir).</param>
public sealed record PushDispatchResult(Guid CampaignId, int RecipientCount);

/// <summary>
/// Faz 12.2b — **hedefleme mantığının TEK sahibi.**
/// </summary>
/// <remarks>
/// 🔴 Bu arayüzün var olma sebebi bir kopyalamayı engellemek: 12.2b'den önce mahalle
/// süzgeci + bildirim tercihi + idempotency yalnız <c>AnnouncementNotificationGenerator</c>
/// içinde yaşıyordu. Manuel gönderim için ikinci bir gerçekleme yazılsaydı duyuru ile
/// manuel gönderim <b>aynı mahalleye farklı kişi kümesi</b> yollardı — ve iki taraf da
/// hiçbir hata vermezdi (görünmez sözleşme #23'ün aynı sınıfı: yönetici "342 kişi" der,
/// vatandaş bildirimi hiç almaz).
///
/// ⚠️ <b>Bildirim tercihi burada uygulanır ve manuel gönderim de ona tabidir.</b>
/// "Yönetici elle yolladıysa herkese gitsin" istisnası açılırsa 10.3'ün tercih ekranı
/// yalan söylemeye başlar: "bildirimleri kapattım ama geliyor".
/// </remarks>
public interface INotificationDispatcher
{
    /// <summary>
    /// Panel formunun "kaç kişiye gidecek?" önizlemesi — gönderimin <b>kendisiyle aynı
    /// süzgeçten</b> geçer, yoksa önizleme ile gerçek ayrışır ve kimse fark etmez.
    /// </summary>
    Task<int> EstimateRecipientsAsync(
        string targetType, IReadOnlyList<Guid>? neighborhoodIds, CancellationToken ct = default);

    /// <summary>
    /// Kampanya satırını açar, hedef kullanıcılara bildirim satırlarını yazar.
    /// Alıcı çıkmasa bile kampanya <b>açılır</b> (0 alıcı da bir cevaptır) ve tamamlanmış işaretlenir.
    /// </summary>
    Task<PushDispatchResult> DispatchAsync(PushDispatchRequest request, CancellationToken ct = default);
}
