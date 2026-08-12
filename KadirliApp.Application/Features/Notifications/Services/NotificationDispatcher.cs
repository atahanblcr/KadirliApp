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
/// Faz 12.2b — hedefleme + bildirim yazımı + kampanya açılışı, **tek yerde**.
///
/// 10.10'un kararları aynen korunuyor, yalnız sahibi değişti:
/// <list type="bullet">
///   <item>Hedefleme: <c>neighborhood</c> → <c>PrimaryNeighborhoodId</c> <b>VEYA</b>
///         <c>UserNeighborhood</c> eşleşmesi; tanınmayan her <c>targetType</c> "all" gibi davranır.</item>
///   <item>Bildirim tercihi (<c>NotificationPreferences.Announcements</c>) <b>üretim anında</b>
///         uygulanır — aksi hâlde 10.3'ün tercih ekranı ölü kalırdı.</item>
///   <item>Pasif ve yasaklı hesaba satır yazılmaz.</item>
///   <item>Gövde 500 karaktere kırpılır (tam metin kaynağın detayında).</item>
/// </list>
///
/// ⚠️ <b>İdempotency burada DEĞİL, çağıranda.</b> Bilinçli: "aynı duyuru iki kez üretilmesin"
/// duyuruya özgü bir kuraldır (<c>related_type</c>+<c>related_id</c>), manuel gönderimde ise
/// aynı metni ikinci kez yollamak <b>meşru bir istektir</b> (ilki gitmemiş olabilir). Bu sınıf
/// idempotency dayatsaydı yönetici ikinci gönderimin neden sessizce yutulduğunu anlayamazdı.
/// </summary>
public class NotificationDispatcher : INotificationDispatcher
{
    /// <summary>Bildirim listesi için özet yeterli; tam metin kaynağın detay ekranında.</summary>
    public const int MaxBodyLength = 500;

    private readonly IUnitOfWork _uow;

    public NotificationDispatcher(IUnitOfWork uow) => _uow = uow;

    public Task<int> EstimateRecipientsAsync(
        string targetType, IReadOnlyList<Guid>? neighborhoodIds, string? source = null,
        CancellationToken ct = default)
        => BuildRecipientQuery(source, targetType, neighborhoodIds).CountAsync(ct);

    public async Task<PushDispatchResult> DispatchAsync(
        PushDispatchRequest request, CancellationToken ct = default)
    {
        var targetUserIds = await BuildRecipientQuery(request.Source, request.TargetType, request.NeighborhoodIds)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var body = request.Body.Length > MaxBodyLength
            ? request.Body[..MaxBodyLength] + "…"
            : request.Body;

        var campaign = new PushCampaign
        {
            Title = request.Title,
            Body = body,
            TargetType = request.TargetType,
            TargetNeighborhoods = request.NeighborhoodIds is { Count: > 0 }
                ? JsonSerializer.Serialize(request.NeighborhoodIds)
                : null,
            Source = request.Source,
            SourceId = request.SourceId,
            CreatedBy = request.CreatedBy,
            RecipientCount = targetUserIds.Count,
            // 🔑 Alıcı yoksa gönderilecek bir şey de yok → kampanya doğduğu anda tamamlanır.
            // İşaretlenmeseydi pano bu satırı sonsuza kadar "Kuyrukta" gösterir ve yönetici
            // gitmeyen bir bildirimi bekler dururdu.
            CompletedAt = targetUserIds.Count == 0 ? DateTime.UtcNow : null
        };

        await _uow.Repository<PushCampaign>().AddAsync(campaign, ct);

        var notifications = _uow.Repository<Notification>();
        foreach (var userId in targetUserIds)
        {
            await notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = request.Title,
                Body = body,
                Type = request.NotificationType,
                RelatedId = request.RelatedId,
                RelatedType = request.RelatedType,
                // 🐛 `CampaignId = campaign.Id` YAZILAMAZ ve bu tuzağa ilk yazımda düşüldü:
                // Id kolonu `gen_random_uuid()` varsayılanıyla tanımlı, yani EF onu
                // **store-generated** sayar ve değer ancak INSERT'ten SONRA geri döner —
                // bu satırda `campaign.Id` hâlâ `Guid.Empty`. Sonuç, bütün bildirimlerin
                // var olmayan bir kampanyaya bağlanması ve FK ihlaliyle patlaması oldu.
                // Gezinme özelliği verildiğinde EF hem INSERT sırasını kurar hem de FK'yı
                // gerçek kimlikle doldurur.
                Campaign = campaign
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return new PushDispatchResult(campaign.Id, targetUserIds.Count);
    }

    /// <summary>
    /// 🔴 <b>Gönderimin ve önizlemenin ORTAK süzgeci.</b> İki ayrı sorgu yazılsaydı panel
    /// "342 kişiye gidecek" der, gönderim 280 satır yazardı ve aradaki fark hiçbir yerde
    /// görünmezdi.
    /// </summary>
    private IQueryable<User> BuildRecipientQuery(
        string? source, string targetType, IReadOnlyList<Guid>? neighborhoodIds)
    {
        // 🔴 Faz 12.15b — tercih artık KAYNAĞA GÖRE seçiliyor (`PushPreferenceTopics`).
        // Öncesinde burada sabit `NotificationPreferences.Announcements` yazıyordu ve
        // 12.15 haber gönderimini eklediği an bu iki yönlü sessiz bir hataya dönüştü:
        // "Duyurular"ı kapatan haberleri de kaybediyor, haber istemeyenin tek çıkışı ise
        // "Duyurular"ı kapatmak — o da §7 madde 41 gereği KESİNTİ bildirimini öldürüyordu.
        // ⚠️ Eşleme burada yazılmaz, tek sahibinden gelir: önizleme de (`EstimateRecipients`)
        // aynı metottan geçmek zorunda, yoksa panel "342" der gönderim 280 yazar (§7 madde 38).
        var users = _uow.Repository<User>().Query()
            .Where(u => u.IsActive && !u.IsBanned)
            .Where(PushPreferenceTopics.For(source));

        // 🔑 `null` ile BOŞ LİSTE burada farklı şeyler demek ve ayrım bilinçli:
        //
        //   null      → "hedefleme listesi YOK" → süzgeç uygulanmaz, herkese gider.
        //               10.10'dan beri böyle ve testle yazılı (`NeighborhoodTargeting_
        //               WithoutAList_FallsBackToEveryone`): duyurunun mahalle kolonu boşsa
        //               kayıt "tüm şehir" demektir.
        //   boş liste → "hedefleme var ama hiçbir mahalle seçilmemiş" → KİMSEYE gitmez.
        //
        // ⚠️ İkisi tek şeye indirgenseydi biri diğerinin yerine geçerdi ve ikisi de sessiz
        // olurdu: null'ı "kimse" saymak duyuruları öldürür, bozuk bir JSON'u "herkes"
        // saymak ise bir veri hatasını **tüm şehre giden bildirime** çevirir.
        if (targetType == PushTargetTypes.Neighborhood && neighborhoodIds is not null)
        {
            var ids = neighborhoodIds;
            users = users.Where(u =>
                (u.PrimaryNeighborhoodId != null && ids.Contains(u.PrimaryNeighborhoodId.Value)) ||
                u.Neighborhoods.Any(un => ids.Contains(un.NeighborhoodId)));
        }

        return users;
    }
}
