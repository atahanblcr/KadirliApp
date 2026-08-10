using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Campaigns.Commands;

public record ApproveCampaignCommand(Guid Id, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "campaigns";
    public string AuditAction => "approve";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Campaign";
}

public class ApproveCampaignCommandHandler : IRequestHandler<ApproveCampaignCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public ApproveCampaignCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(ApproveCampaignCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Campaign>();
        var campaign = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (campaign == null) return false;

        // Faz 12.10: kuralın tek sahibi CampaignModeration.
        CampaignModeration.Approve(campaign, request.AdminId, DateTime.UtcNow);

        repo.Update(campaign);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
