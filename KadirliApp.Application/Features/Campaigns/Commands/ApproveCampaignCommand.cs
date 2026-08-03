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

        // Faz 11.15b: reddedilmiş bir kampanya sonradan onaylanırsa bayat red gerekçesi
        // kalmasın. Aynı düzeltme ilanlarda 10.14(1)'de yapılmıştı ama kampanyaya
        // taşınmamıştı: panelde "Onaylandı" rozetiyle "Reddedilme sebebi: …" satırı
        // yan yana görünüyor, işletme sahibi kampanyasının durumundan emin olamıyordu.
        campaign.Status = "approved";
        campaign.ApprovedBy = request.AdminId;
        campaign.ApprovedAt = DateTime.UtcNow;
        campaign.RejectedReason = null;

        repo.Update(campaign);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
