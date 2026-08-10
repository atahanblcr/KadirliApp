using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Moderation;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Campaigns.Commands;

public class UpdateCampaignCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountCode { get; set; }
    public string? Terms { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? CoverImageId { get; set; }
    public bool RemoveCoverImage { get; set; }

    /// <summary>
    /// ☠️ Faz 12.10'dan beri <b>yazılamaz</b> — moderasyon durumunun tek sahibi
    /// <c>ApproveCampaignCommand</c>/<c>RejectCampaignCommand</c> (görünmez sözleşme #52).
    /// Alan DTO'da duruyor (§5), ama farklı bir değer gelirse komut reddeder
    /// (<c>ModerationStatusGuard</c>).
    /// ⚠️ Nullable: non-nullable bir referans tipi MVC'de <b>örtük olarak zorunludur</b>;
    /// alan formdan kaldırılınca <c>ModelState</c> kırılırdı (§5 — gevşetmek güvenlidir).
    /// </summary>
    public string? Status { get; set; }
}

public class UpdateCampaignCommandHandler : IRequestHandler<UpdateCampaignCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateCampaignCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Campaign>();
        var campaign = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (campaign == null) return false;

        // Faz 12.10 — moderasyon durumu bu yoldan yazılamaz (#52); guard ilk yazmadan ÖNCE.
        ModerationStatusGuard.EnsureUnchanged(campaign.Status, request.Status);

        campaign.BusinessId = request.BusinessId;
        campaign.Title = request.Title;
        campaign.Description = request.Description;
        campaign.DiscountPercentage = request.DiscountPercentage;
        campaign.DiscountCode = request.DiscountCode;
        campaign.Terms = request.Terms;
        campaign.StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        campaign.EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

        if (request.RemoveCoverImage)
            campaign.CoverImageId = null;
        else if (request.CoverImageId.HasValue)
            campaign.CoverImageId = request.CoverImageId;

        repo.Update(campaign);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
