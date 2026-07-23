using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Campaigns.Commands;

public class CreateCampaignCommand : IRequest<Guid>
{
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountCode { get; set; }
    public string? Terms { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? CoverImageId { get; set; }

    /// <summary>Admin panelden oluşturulan kampanyalar doğrudan onaylı başlar.</summary>
    public bool AutoApprove { get; set; }

    /// <summary>Onaylayan admin; controller claim'lerden set eder.</summary>
    public Guid? ApprovedBy { get; set; }
}

public class CreateCampaignCommandHandler : IRequestHandler<CreateCampaignCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateCampaignCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = new Campaign
        {
            BusinessId = request.BusinessId,
            Title = request.Title,
            Description = request.Description,
            DiscountPercentage = request.DiscountPercentage,
            DiscountCode = request.DiscountCode,
            Terms = request.Terms,
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            CoverImageId = request.CoverImageId,
            Status = request.AutoApprove ? "approved" : "pending",
            ApprovedBy = request.AutoApprove ? request.ApprovedBy : null,
            ApprovedAt = request.AutoApprove ? DateTime.UtcNow : null
        };

        await _uow.Repository<Campaign>().AddAsync(campaign, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return campaign.Id;
    }
}
