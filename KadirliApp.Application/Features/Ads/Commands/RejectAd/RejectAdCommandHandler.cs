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

        // Faz 12.10: kuralın tek sahibi AdModeration (bkz. ApproveAdCommandHandler).
        AdModeration.Reject(ad, request.Reason, DateTime.UtcNow);

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
