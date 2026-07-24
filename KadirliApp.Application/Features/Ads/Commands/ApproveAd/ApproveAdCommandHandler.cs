using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.ApproveAd;

public class ApproveAdCommandHandler : IRequestHandler<ApproveAdCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public ApproveAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(ApproveAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.AdId, cancellationToken);

        if (ad == null) return false;

        // Faz 10.14(1) yan düzeltmesi: reddedilmiş bir ilan sonradan onaylanırsa bayat red gerekçesi kalmasın.
        ad.Status = "approved";
        ad.ApprovedBy = request.AdminId;
        ad.ApprovedAt = DateTime.UtcNow;
        ad.RejectedReason = null;
        ad.RejectedAt = null;

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
