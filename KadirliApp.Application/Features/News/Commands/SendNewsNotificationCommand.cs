using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Application.Features.News.Services;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Commands;

/// <summary>
/// Faz 12.15 — bir haberi <b>tek tıkla</b> push olarak gönderir.
/// </summary>
/// <remarks>
/// 🔴 <b>ELLE, otomatik değil.</b> Günde ~5 haber otomatik push'a çevrilseydi kullanıcı
/// bildirimleri <b>tümden kapatır</b> ve o andan sonra <i>kesinti</i> bildirimini de almazdı —
/// yani otomatik haber push'u <b>başka modüllerin bildirimlerini zehirler</b>. 5/gün için
/// tek tık yeter.
///
/// 🔴 <b>Hedeflemenin tek sahibi <see cref="INotificationDispatcher"/></b> (§7 madde 38).
/// Buraya ikinci bir alıcı sorgusu yazılmaz; kullanıcının bildirim tercihi de orada
/// uygulanır ("yönetici yolladıysa herkese gitsin" istisnası 10.3'ün tercih ekranını
/// yalancı yapardı).
///
/// 🔑 <b>Hedef her zaman <c>all</c>:</b> haberin mahallesi yok. Mahalle seçtiren bir form
/// koymak, doldurulacak bilgisi olmayan bir alan olurdu.
///
/// ⚠️ <b>Kaynak <c>news</c>, <c>manual</c> DEĞİL:</b> ayrım <c>SourceId</c> ve o kimlik
/// "aynı haber ikinci kez gönderilemez" kuralının veritabanındaki çıpası.
/// </remarks>
public class SendNewsNotificationCommand : IRequest<ApiResponse<NewsNotificationResultDto>>, IAuditableCommand
{
    public Guid Id { get; set; }

    /// <summary>Butona basan yönetici.</summary>
    public Guid? AdminId { get; set; }

    public string AuditModule => NewsAudit.Module;
    public string AuditAction => "send-notification";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(NewsArticle);
}

public class SendNewsNotificationCommandHandler
    : IRequestHandler<SendNewsNotificationCommand, ApiResponse<NewsNotificationResultDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationDispatcher _dispatcher;

    public SendNewsNotificationCommandHandler(IUnitOfWork uow, INotificationDispatcher dispatcher)
    {
        _uow = uow;
        _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<NewsNotificationResultDto>> Handle(
        SendNewsNotificationCommand request, CancellationToken ct)
    {
        // ⚠️ tracking: true — işaret yazılacak. `Query()` varsayılanı AsNoTracking'tir ve
        // bağlantısız nesneye yazmak `SaveChanges`'e hiç ulaşmaz (12.3 canlı bulgusu):
        // bildirim gider, "gönderildi" işareti düşmez ve panel ikinci bir push teklif eder.
        var article = await _uow.Repository<NewsArticle>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (article is null)
            return ApiResponse<NewsNotificationResultDto>.FailureResponse("NOT_FOUND", "Haber bulunamadı.");

        // 🔑 Görünürlük BURADA YENİDEN YAZILMAZ: aynı üç koşulun bellek yüzünü yazmak
        // §7 madde 23'ün sınıfı olurdu (panel bir şey der, vatandaş başkasını görür).
        // Kaydın kategorileri zaten yüklü — yine de tek sahibin (`NewsVisibility`) kendi
        // sorgusu koşuluyor; fazladan bir indeksli `EXISTS` turu, ayrışabilen ikinci bir
        // tanımdan ucuz.
        var isVisible = await NewsVisibility.Published(_uow.Repository<NewsArticle>().Query())
            .AnyAsync(x => x.Id == article.Id, ct);

        var eligibility = NewsNotificationRules.Evaluate(article, isVisible);

        if (eligibility != NewsNotifyEligibility.Sendable)
            return ApiResponse<NewsNotificationResultDto>.FailureResponse(
                eligibility == NewsNotifyEligibility.AlreadySent ? "CONFLICT" : "VALIDATION",
                NewsNotificationRules.Reason(eligibility)!);

        var title = NewsNotificationText.Title(article.TitleOverride, article.SourceTitle);
        var body = NewsNotificationText.Body(
            article.ExcerptOverride, article.SourceExcerpt, article.SourcePlainText, title);

        var result = await _dispatcher.DispatchAsync(new PushDispatchRequest(
            Title: title,
            Body: body,
            TargetType: PushTargetTypes.All,
            NeighborhoodIds: null,
            Source: PushCampaignSources.News,
            SourceId: article.Id,
            CreatedBy: request.AdminId,
            NotificationType: NewsNotifications.RelatedType,
            RelatedType: NewsNotifications.RelatedType,
            RelatedId: article.Id), ct);

        // 🔴 İşaret gönderimden SONRA konur ve sıra bilinçli. Ters sıra (önce işaretle,
        // sonra gönder) kampanya kimliğini bilmediğimiz için zaten mümkün değil; ama
        // mümkün olsaydı da yanlış olurdu: gönderim düşerse haber "bildirildi" damgası
        // yer ve **bir daha hiç** bildirilemezdi. Bu yöndeki risk (gönderildi ama işaret
        // düşmedi) yöneticiye ikinci bir buton gösterir — ve o butonu veritabanındaki
        // kısmi unique indeks durdurur (`ix_push_campaigns_news_source_id_unique`).
        article.MarkNotificationSent(result.CampaignId, result.RecipientCount, request.AdminId, DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<NewsNotificationResultDto>.SuccessResponse(new NewsNotificationResultDto
        {
            CampaignId = result.CampaignId,
            RecipientCount = result.RecipientCount,
            Title = title,
            Body = body
        });
    }
}
