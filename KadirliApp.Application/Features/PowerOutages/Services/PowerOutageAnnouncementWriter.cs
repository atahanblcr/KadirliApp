using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Notifications.Services;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.PowerOutages.Services;

/// <inheritdoc cref="IPowerOutageAnnouncementWriter"/>
public class PowerOutageAnnouncementWriter : IPowerOutageAnnouncementWriter
{
    /// <summary>Duyuru türünün slug'ı — ad panelden değişse bile satır bulunur.</summary>
    public const string AnnouncementTypeSlug = "elektrik-kesintisi";

    private readonly IUnitOfWork _uow;
    private readonly IAnnouncementNotificationGenerator _generator;

    public PowerOutageAnnouncementWriter(IUnitOfWork uow, IAnnouncementNotificationGenerator generator)
    {
        _uow = uow;
        _generator = generator;
    }

    public async Task<PowerOutageAnnouncementResult> SyncAsync(
        PowerOutage outage,
        bool sendNotification,
        IReadOnlyList<Guid>? targetNeighborhoodIds,
        Guid? createdBy,
        CancellationToken ct = default)
    {
        // ⚠️ `tracking: true` ŞART. `Repository.Query()` varsayılan olarak **AsNoTracking**
        // döner; bağlantısız bir varlık üzerinde yapılan değişiklik `SaveChanges`'e hiç
        // ulaşmaz. Bu tuzak 12.3'te canlı testte yakalandı (aşağıdaki `RemoveAsync`'te
        // duyuru "silindi" görünüyor, `deleted_at` boş kalıyordu) — kod doğru görünür,
        // istisna oluşmaz, yalnız hiçbir şey olmaz.
        var existing = outage.AnnouncementId is { } id
            ? await _uow.Repository<Announcement>().Query(tracking: true)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == id, ct)
            : null;

        // ── Zaten gönderilmiş bir bildirim varsa: metni/süresi TAZELENİR, ikincisi ÜRETİLMEZ.
        //
        // ⚠️ `sendNotification` burada bilinçli olarak sorulmuyor. Yönetici güncellemede
        // kutuyu boşaltsa bile duyuru silinmez: bildirim çoktan gitti (`FcmSent` terminaldir,
        // görünmez sözleşme #37) ve duyuruyu kaldırmak yalnız vatandaşın elindeki bildirimi
        // ölü bağlantıya çevirirdi. Kutunun anlamı "gönder", "gönderilmiş olanı geri al" değil.
        if (existing is not null)
        {
            Apply(existing, outage, EffectiveTargets(outage, targetNeighborhoodIds));
            _uow.Repository<Announcement>().Update(existing);
            return new PowerOutageAnnouncementResult(PowerOutageNotifyOutcome.Updated, existing.Id, 0);
        }

        if (!sendNotification)
            return new PowerOutageAnnouncementResult(PowerOutageNotifyOutcome.NotRequested, null, 0);

        // 🔴 FK yoksa hedeflenecek kimse yok. Serbest metinli (12.3 öncesi ya da geri
        // doldurmada eşleşmemiş) kayıt bildirim gönderemez — panel bunu buton kapalı +
        // açıklamayla söyler, komut ise sessizce "gönderdim" demez.
        if (outage.NeighborhoodId is null)
            return new PowerOutageAnnouncementResult(PowerOutageNotifyOutcome.NotTargetable, null, 0);

        var type = await _uow.Repository<AnnouncementType>().Query()
            .FirstOrDefaultAsync(t => t.Slug == AnnouncementTypeSlug, ct);

        // Tür seed'de garanti altında (DbSeeder.EnsurePowerOutageAnnouncementTypeAsync);
        // yine de silinmiş olabilir — o hâlde bildirim gitmez ama kesinti kaydı kurtulur.
        if (type is null)
            return new PowerOutageAnnouncementResult(PowerOutageNotifyOutcome.NotTargetable, null, 0);

        var announcement = new Announcement
        {
            TypeId = type.Id,
            CreatedBy = createdBy,
            SendPushNotification = true,
            Status = "active",
            SentAt = DateTime.UtcNow
        };

        Apply(announcement, outage, EffectiveTargets(outage, targetNeighborhoodIds));

        await _uow.Repository<Announcement>().AddAsync(announcement, ct);

        // 🐛 `outage.AnnouncementId = announcement.Id` YAZILAMAZ — 12.2b'de bu tuzağa
        // düşülmüştü: `id` kolonu `gen_random_uuid()` varsayılanıyla tanımlı, yani EF onu
        // **store-generated** sayar ve değer ancak INSERT'ten sonra geri döner; bu satırda
        // hâlâ `Guid.Empty`. Bağ gezinme özelliğinden kurulur, EF sırayı ve FK'yı kendisi yazar.
        outage.Announcement = announcement;
        _uow.Repository<PowerOutage>().Update(outage);

        await _uow.SaveChangesAsync(ct);

        // Bildirim üretimi duyuru kaydedildikten SONRA: üretici `announcement.Id`'yi hem
        // idempotency işareti hem deep-link hedefi olarak kullanıyor.
        var recipients = await _generator.GenerateForAnnouncementAsync(
            announcement, PushCampaignSources.PowerOutage, ct);

        return new PowerOutageAnnouncementResult(
            PowerOutageNotifyOutcome.Created, announcement.Id, recipients);
    }

    public async Task RemoveAsync(PowerOutage outage, CancellationToken ct = default)
    {
        if (outage.AnnouncementId is not { } announcementId) return;

        // 🐛 `tracking: true` olmadan `SoftRemove` bağlantısız bir nesnenin alanını yazar
        // ve `SaveChanges` onu hiç görmez: duyuru silinmiş görünür, `deleted_at` boş kalır,
        // vatandaş ölü duyuruyu görmeye devam eder ve **hiçbir hata oluşmaz.**
        var announcement = await _uow.Repository<Announcement>().Query(tracking: true)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == announcementId, ct);

        // Bağ önce kopar: duyuru soft-delete edilirken kesinti hâlâ ona işaret ederse
        // panel silinmiş bir duyuruya bağlantı çizmeye devam eder.
        outage.AnnouncementId = null;
        outage.Announcement = null;

        if (announcement is null) return;

        _uow.Repository<Announcement>().SoftRemove(announcement);

        // 🔴 Bildirimler GERÇEKTEN silinir (soft değil) — `DeleteAnnouncementCommand`
        // ile birebir aynı gerekçe: türetilmiş veri, kaynağı yoksa yaşamamalı.
        var orphaned = await _uow.Repository<Notification>().Query()
            .Where(n => n.RelatedType == AnnouncementNotificationGenerator.RelatedTypeAnnouncement
                        && n.RelatedId == announcementId)
            .ToListAsync(ct);

        foreach (var notification in orphaned)
            _uow.Repository<Notification>().Remove(notification);
    }

    /// <summary>
    /// Bildirimin gideceği mahalleler: kesintinin kendi mahallesi <b>her zaman</b> + istenen ekler.
    /// </summary>
    /// <remarks>
    /// ⚠️ Boş liste dönmesi mümkün değil (FK zaten denetlendi) ve bu önemli:
    /// <c>NotificationDispatcher</c>'da <b>boş liste "kimseye gitmez"</b> demek, <c>null</c> ise
    /// "herkese" — yani yanlış tarafa düşen bir hesap, bir mahalle kesintisini <b>tüm şehre
    /// giden bildirime</b> çevirebilirdi.
    /// </remarks>
    private static List<Guid> EffectiveTargets(PowerOutage outage, IReadOnlyList<Guid>? extra)
    {
        var targets = new List<Guid>();
        if (outage.NeighborhoodId is { } own) targets.Add(own);
        if (extra is not null) targets.AddRange(extra);
        return targets.Distinct().ToList();
    }

    private static void Apply(Announcement announcement, PowerOutage outage, List<Guid> targets)
    {
        announcement.Title = PowerOutageAnnouncementText.Title(outage);
        announcement.Body = PowerOutageAnnouncementText.Body(outage);
        announcement.TargetType = PushTargetTypes.Neighborhood;
        announcement.TargetNeighborhoods = JsonSerializer.Serialize(targets);

        // 🔑 Duyuru kesinti bitince kendiliğinden görünmez olur. Aksi hâlde vatandaşın
        // duyurular listesi geçmiş kesintilerle dolar ve "bu ne zamanki kesinti?" sorusu
        // her seferinde tarihi okumayı gerektirirdi.
        announcement.VisibleUntil = DateTime.SpecifyKind(outage.EndTime, DateTimeKind.Utc);
    }
}
