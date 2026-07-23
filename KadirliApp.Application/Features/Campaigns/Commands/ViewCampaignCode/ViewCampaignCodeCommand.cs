using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Campaigns.Commands.ViewCampaignCode;

public record CampaignCodeDto(string Code, DateTime ViewedAt);

/// <summary>
/// Faz 10.12: POST /v1/campaigns/{id}/view-code ([Authorize]) — kampanya kodunu döner ve
/// campaign_code_views'a iz düşer. Aynı kullanıcı ikinci kez isterse MEVCUT kayıt döner
/// (yeni satır yok, code_view_count artmaz) — sayaç "kodu kaç FARKLI kullanıcı gördü" demektir.
/// Görünürlük public detayla aynı: yalnız approved + tarih aralığı geçerli kampanya, diğerine 404.
/// KARAR: kodu olmayan kampanyada 400 VALIDATION_ERROR (iz de düşülmez — istatistik şişmesin).
/// </summary>
public record ViewCampaignCodeCommand(Guid CampaignId, Guid UserId) : IRequest<CampaignCodeDto>;

public class ViewCampaignCodeCommandHandler : IRequestHandler<ViewCampaignCodeCommand, CampaignCodeDto>
{
    private readonly IUnitOfWork _uow;

    public ViewCampaignCodeCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<CampaignCodeDto> Handle(ViewCampaignCodeCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var campaign = await _uow.Repository<Campaign>().Query()
            .Where(x => x.Id == request.CampaignId
                        && x.Status == "approved" && x.StartDate <= now && x.EndDate >= now)
            .Select(x => new { x.Id, x.DiscountCode })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Campaign), request.CampaignId);

        if (string.IsNullOrWhiteSpace(campaign.DiscountCode))
            throw new AppException("Bu kampanyanın indirim kodu yok.", "VALIDATION_ERROR");

        var codeViews = _uow.Repository<CampaignCodeView>();
        var existing = await codeViews.Query()
            .Where(v => v.CampaignId == request.CampaignId && v.UserId == request.UserId)
            .OrderBy(v => v.ViewedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != null)
            return new CampaignCodeDto(campaign.DiscountCode, existing.ViewedAt);

        await codeViews.AddAsync(new CampaignCodeView
        {
            CampaignId = request.CampaignId,
            UserId = request.UserId,
            ViewedAt = now
        }, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Sayaç atomik ExecuteUpdate — cache invalidation tetiklemez (campaigns sorguları cache'siz).
        // (campaign_code_views'ta unique index yok — eşzamanlı ilk-istek yarışı nadiren çift satır
        // bırakabilir; okuma ViewedAt sırasıyla ilkini döndüğünden davranış bozulmaz.)
        await _uow.Repository<Campaign>().Query()
            .Where(x => x.Id == request.CampaignId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CodeViewCount, x => x.CodeViewCount + 1), cancellationToken);

        return new CampaignCodeDto(campaign.DiscountCode, now);
    }
}
