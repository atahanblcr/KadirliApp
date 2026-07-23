using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.DeleteAd;

public class DeleteAdCommandHandler : IRequestHandler<DeleteAdCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.AdId, cancellationToken);

        if (ad == null) return false;

        repo.SoftRemove(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
