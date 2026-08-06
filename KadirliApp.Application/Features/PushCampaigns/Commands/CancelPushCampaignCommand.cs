using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.PushCampaigns.Commands;

/// <summary>
/// Plan dışı ek (Faz 12.2b) — **gönderilmemiş satırları geri çek.**
/// </summary>
/// <remarks>
/// 🔑 Gerekçe: bir bildirim, gönderildikten sonra düzeltilemez. Yanlış metinle ya da yanlış
/// mahalleye yollanan bir gönderimin 12.2b'den önceki tek çaresi veritabanına elle girmekti —
/// ve pratikte kimse girmiyordu. Bu komut, teslim panosunun kurduğu <b>tek çıpayı</b>
/// (kampanya kimliği) kullanarak henüz FCM'e iletilmemiş satırları hem push kuyruğundan
/// hem de kullanıcının bildirim listesinden çıkarır.
///
/// 🔴 <b>İptal, gönderimin tersi DEĞİL sınırıdır.</b> <c>FcmSent=true</c> terminaldir
/// (görünmez sözleşme): iletilmiş mesaj geri alınamaz, bu komut ona <b>dokunmaz</b> ve
/// dokunmayı teklif de etmez. "Hepsini geri al" gibi bir buton konsaydı hiçbir şey yapmaz
/// ve kimse hata almazdı — panelin en sinsi yalan biçimi.
///
/// ⚠️ Silme <b>fiziksel</b>: <c>Notification</c> soft-delete taşımıyor ve taşımamalı.
/// Görünmez sözleşme #24 aynı kararı veriyor (silinen duyurunun bildirimleri fiziksel
/// siliniyor) — "silinmiş ama listede duran bildirim" kullanıcıyı boş sayfaya götürür.
/// </remarks>
public sealed record CancelPushCampaignCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "push-campaigns";
    public string AuditAction => "cancel-push";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(PushCampaign);
}

public sealed class CancelPushCampaignCommandHandler : IRequestHandler<CancelPushCampaignCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public CancelPushCampaignCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(CancelPushCampaignCommand request, CancellationToken ct)
    {
        var campaign = await _uow.Repository<PushCampaign>().Query(tracking: true)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (campaign is null) return false;

        // 🐛 İlk yazımda buradaki koşul `CompletedAt is not null || CancelledAt is not null`
        // idi ve test onu hemen kırdı: **"tamamlandı" ile "geri çekilecek bir şey kalmadı"
        // AYNI ŞEY DEĞİL.** Kampanya, gönderilebilir bekleyen satır kalmadığında tamamlanır —
        // ama token'ı olmayan alıcıların satırları hâlâ durur, kullanıcının bildirim
        // listesinde görünür ve o kişi yarın token kaydederse gönderilir. Yani tamamlanmış
        // bir kampanyada bile geri çekilecek gerçek bir şey olabilir.
        //
        // Tek gerçek engel: zaten iptal edilmiş olmak.
        if (campaign.CancelledAt is not null) return false;

        var notifications = _uow.Repository<Notification>();
        var pending = await notifications.Query(tracking: true)
            .Where(n => n.CampaignId == campaign.Id && !n.FcmSent)
            .ToListAsync(ct);

        if (pending.Count == 0) return false;   // iletilmemiş satır yok → iptal edilecek şey yok

        foreach (var n in pending)
            notifications.Remove(n);

        var now = DateTime.UtcNow;
        campaign.CancelledAt = now;
        // Kampanya kapanır: gönderim işi bir daha bu satırlara bakmayacak (zaten silindiler)
        // ve pano onu sonsuza kadar "Kuyrukta" göstermeyecek.
        // ⚠️ Zaten tamamlanmışsa ilk tamamlanma anı korunur — "ne zaman bitti" sorusunun
        // cevabı iptal yüzünden tazelenmemeli.
        campaign.CompletedAt ??= now;

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
