using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.PushCampaigns.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.PushCampaigns.Commands;

/// <summary>
/// Faz 12.2b — **panelden tek seferlik bildirim gönderimi.**
///
/// 12.2b öncesinde bildirim satırı üreten tek şey <c>AnnouncementNotificationGenerator</c>'dı;
/// yönetici tek bir push atmak için <b>duyuru oluşturmak zorundaydı</b> — yani vatandaşın
/// duyurular listesine, kalıcı olarak, yalnız bildirim göndermek uğruna bir kayıt düşüyordu.
///
/// ⚠️ <b>Hedefleme burada YAZILMAZ</b> — <see cref="INotificationDispatcher"/>'a devredilir.
/// Buraya ikinci bir mahalle süzgeci yazılsaydı duyuru ile manuel gönderim aynı mahalleye
/// farklı kişi kümesi yollardı ve iki taraf da hiç hata vermezdi.
///
/// 🔴 <b>Aksiyon adı ≠ izin eylemi (görünmez sözleşme #19).</b> Panel controller'ında bu
/// komutu çağıran aksiyonun adı <c>Send</c>'dir; hiçbir moderasyon önekiyle eşleşmez ve POST
/// olduğu için sessizce <c>update</c>'e düşer. Ekran <b>yalnız-admin</b> olduğu için bugün
/// zararsız — ama tam bu yüzden <c>AdminOnlyControllers</c>'ta yazılı olmak zorunda:
/// ekran bir gün moderatöre açılırsa "düzenleme yetkisi olan herkes tüm şehre push atabilir"
/// hâline gelirdi ve bunu kimse fark etmezdi.
/// </summary>
public sealed record SendPushCampaignCommand(SendPushCampaignDto Dto, Guid? CreatedBy)
    : IRequest<ApiResponse<Guid>>, IAuditableCommand
{
    public string AuditModule => "push-campaigns";
    public string AuditAction => "send-push";
    public Guid? AuditAffectedId => null;      // kampanya kimliği komut BİTİNCE doğuyor
    public string? AuditAffectedType => nameof(PushCampaign);
}

public sealed class SendPushCampaignCommandHandler
    : IRequestHandler<SendPushCampaignCommand, ApiResponse<Guid>>
{
    private readonly INotificationDispatcher _dispatcher;

    public SendPushCampaignCommandHandler(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task<ApiResponse<Guid>> Handle(SendPushCampaignCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        var targetType = string.IsNullOrWhiteSpace(dto.TargetType) ? PushTargetTypes.All : dto.TargetType.Trim();
        if (!PushTargetTypes.Supported.Contains(targetType))
            return ApiResponse<Guid>.FailureResponse("VALIDATION", "Geçersiz hedef kitle seçildi.");

        // Duyuru formundaki aynı kural: "Belirli Mahalleler" seçilip hiç mahalle
        // işaretlenmemesi bir kullanıcı hatasıdır. ⚠️ Sessizce "herkes"e düşürülemez —
        // bir doğrulama hatası, tüm şehre giden bir bildirime dönüşürdü.
        if (targetType == PushTargetTypes.Neighborhood && dto.TargetNeighborhoodIds.Count == 0)
            return ApiResponse<Guid>.FailureResponse(
                "VALIDATION", "Hedef kitle 'Belirli Mahalleler' seçildiğinde en az bir mahalle seçmelisiniz.");

        var result = await _dispatcher.DispatchAsync(new PushDispatchRequest(
            Title: dto.Title.Trim(),
            Body: dto.Body.Trim(),
            TargetType: targetType,
            NeighborhoodIds: targetType == PushTargetTypes.Neighborhood ? dto.TargetNeighborhoodIds : null,
            Source: PushCampaignSources.Manual,
            SourceId: null,
            CreatedBy: request.CreatedBy,
            NotificationType: PushCampaignSources.Manual,
            // 🔑 Deep-link YOK ve bu bilinçli: gidilecek bir kayıt yok. Uydurma bir
            // relatedType yazsaydık görünmez sözleşme #18 gereği mobil onu tanımaz,
            // gezinmeyi iptal eder — yani aynı sonuç, üstelik yalan bir alanla.
            RelatedType: null,
            RelatedId: null), ct);

        return ApiResponse<Guid>.SuccessResponse(result.CampaignId);
    }
}
