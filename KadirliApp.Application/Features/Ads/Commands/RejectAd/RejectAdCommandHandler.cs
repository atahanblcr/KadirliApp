using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.RejectAd;

public class RejectAdCommandHandler : IRequestHandler<RejectAdCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RejectAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(RejectAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.AdId, cancellationToken);

        if (ad == null) return false;

        // Faz 10.14(1): red gerekçesi RejectedReason/RejectedAt'e yazılır (MyAdDto sahibe bunu döner).
        // "Kim reddetti" izi ApprovedBy'ı ezerek DEĞİL, IAuditableCommand üzerinden tutulur (RejectCampaign deseni).
        // Bir ilan aynı anda hem onaylı hem reddedilmiş olamaz → onay izleri temizlenir.
        ad.Status = "rejected";
        ad.RejectedReason = request.Reason;
        ad.RejectedAt = DateTime.UtcNow;
        ad.ApprovedBy = null;
        ad.ApprovedAt = null;

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
