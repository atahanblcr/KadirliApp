using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Campaigns.Commands;

public record DeleteCampaignCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "campaigns";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Campaign";
}

public class DeleteCampaignCommandHandler : IRequestHandler<DeleteCampaignCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteCampaignCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteCampaignCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Campaign>();
        var campaign = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (campaign == null) return false;

        repo.SoftRemove(campaign);
        repo.Update(campaign);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
