using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.DeleteMyAd;

/// <summary>
/// Faz 10.6: kullanıcının KENDİ ilanını silmesi (soft delete). Admin'in DeleteAdCommand'inden farkı:
/// sahiplik kontrolü (başkasının ilanı → 403) ve olmayan ilan için 404 (bool yerine exception).
/// </summary>
public record DeleteMyAdCommand(Guid AdId, Guid UserId) : IRequest<bool>;

public class DeleteMyAdCommandHandler : IRequestHandler<DeleteMyAdCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteMyAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteMyAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.AdId, cancellationToken);
        if (ad == null)
            throw new NotFoundException(nameof(Ad), request.AdId);
        if (ad.UserId != request.UserId)
            throw new ForbiddenException("Bu ilan üzerinde işlem yetkiniz yok.");

        repo.SoftRemove(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
