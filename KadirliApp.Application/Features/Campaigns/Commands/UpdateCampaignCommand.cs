using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
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
    public string Status { get; set; } = "pending";
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

        campaign.BusinessId = request.BusinessId;
        campaign.Title = request.Title;
        campaign.Description = request.Description;
        campaign.DiscountPercentage = request.DiscountPercentage;
        campaign.DiscountCode = request.DiscountCode;
        campaign.Terms = request.Terms;
        campaign.StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        campaign.EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);
        campaign.Status = request.Status;

        if (request.RemoveCoverImage)
            campaign.CoverImageId = null;
        else if (request.CoverImageId.HasValue)
            campaign.CoverImageId = request.CoverImageId;

        repo.Update(campaign);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
