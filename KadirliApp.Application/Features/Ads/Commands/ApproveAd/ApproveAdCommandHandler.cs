using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.ApproveAd;

// `Ads` namespace'i (AdModeration) bu dosyanın namespace'inin atasında olduğu için
// ayrıca using gerekmiyor.

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

        // Faz 12.10: kuralın tek sahibi AdModeration (taze pencere #25 + bayat gerekçe
        // temizliği orada). Handler artık yalnız veriyi getirip kaydediyor — kural burada
        // yazılırsa Düzenle formunun açtığı ikinci yol onu yine atlar.
        ad.Approve(request.AdminId, DateTime.UtcNow);

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
