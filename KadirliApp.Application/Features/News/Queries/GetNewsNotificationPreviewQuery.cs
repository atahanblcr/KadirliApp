using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Queries;

/// <summary>
/// Faz 12.15 — <b>"gönder" butonunun etrafındaki her şeyi</b> sunucuda hesaplar.
/// </summary>
/// <remarks>
/// 🔴 <b>Neden ayrı bir sorgu, neden <c>NewsAdminDto</c>'ya eklenmiş birkaç alan değil:</b>
/// cevabın iki parçası <b>kayıtta yok</b> — gönderilecek metin (üretilir) ve alıcı sayısı
/// (kullanıcı tablosundan sayılır). Bunları liste projeksiyonuna koymak, 20 satırlık her
/// sayfada 20 kez alıcı saymak olurdu; ayrı bir sorgu <b>yalnız ayrıntı ekranında</b> koşar.
///
/// 🔑 <b>Alıcı sayısı gönderimin KENDİ sorgusundan gelir</b> (§7 madde 38): ikinci bir sayım
/// yazılsaydı önizleme "342 kişiye gidecek" der, gönderim 280 satır yazar ve fark hiçbir
/// yerde görünmezdi (12.2b'de birebir bu tuzak vardı).
///
/// 📌 <b>Önbelleklenmez</b> — panelin diğer haber sorguları gibi. Alıcı sayısı ve
/// görünürlük "şu an"ın cevabı; 15 dakika eski bir sayı, yöneticinin onayladığı şeyi
/// değiştirir.
/// </remarks>
public record GetNewsNotificationPreviewQuery(Guid Id) : IRequest<NewsNotificationPreviewDto?>;

public class GetNewsNotificationPreviewQueryHandler
    : IRequestHandler<GetNewsNotificationPreviewQuery, NewsNotificationPreviewDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationDispatcher _dispatcher;

    public GetNewsNotificationPreviewQueryHandler(IUnitOfWork uow, INotificationDispatcher dispatcher)
    {
        _uow = uow;
        _dispatcher = dispatcher;
    }

    public async Task<NewsNotificationPreviewDto?> Handle(
        GetNewsNotificationPreviewQuery request, CancellationToken ct)
    {
        var article = await _uow.Repository<NewsArticle>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (article is null) return null;

        // Görünürlüğün tek sahibi (`NewsVisibility`) — komutun kullandığı sorgunun aynısı.
        var isVisible = await NewsVisibility.Published(_uow.Repository<NewsArticle>().Query())
            .AnyAsync(x => x.Id == article.Id, ct);

        var eligibility = NewsNotificationRules.Evaluate(article, isVisible);

        var title = NewsNotificationText.Title(article.TitleOverride, article.SourceTitle);
        var body = NewsNotificationText.Body(
            article.ExcerptOverride, article.SourceExcerpt, article.SourcePlainText, title);

        // Hesabını silmiş yöneticinin gönderimi de kayıtta durur (denetim izindeki karar).
        var sentByName = article.NotificationSentBy is { } sender
            ? await _uow.Repository<User>().Query().IgnoreQueryFilters()
                .Where(u => u.Id == sender).Select(u => u.Username).FirstOrDefaultAsync(ct)
            : null;

        return new NewsNotificationPreviewDto
        {
            ArticleId = article.Id,
            CanSend = eligibility == NewsNotifyEligibility.Sendable,
            Reason = NewsNotificationRules.Reason(eligibility),
            Title = title,
            Body = body,

            // Hedef her zaman "all": haberin mahallesi yok.
            EstimatedRecipients = await _dispatcher.EstimateRecipientsAsync(PushTargetTypes.All, null, ct),

            SentAt = article.NotificationSentAt,
            CampaignId = article.NotificationCampaignId,
            RecipientCount = article.NotificationRecipientCount,
            SentByName = sentByName
        };
    }
}
