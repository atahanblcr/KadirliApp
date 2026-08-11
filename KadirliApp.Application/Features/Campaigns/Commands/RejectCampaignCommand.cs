using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Campaigns.Commands;

public record RejectCampaignCommand(Guid Id, Guid AdminId, string? Reason = null) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "campaigns";
    public string AuditAction => "reject";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Campaign";
    public object? AuditDetails => Reason is not null ? new { reason = Reason } : null;
}

public class RejectCampaignCommandHandler : IRequestHandler<RejectCampaignCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RejectCampaignCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(RejectCampaignCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Campaign>();
        var campaign = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (campaign == null) return false;

        // Faz 12.10: kuralın tek sahibi CampaignModeration — onay izlerinin temizlenmesi
        // (12.10'da düzeltilen simetri hatası) de orada.
        campaign.Reject(request.Reason);

        repo.Update(campaign);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
