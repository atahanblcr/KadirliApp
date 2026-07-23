using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Commands.FavoriteAd;

// Faz 10.6 KARARI: favori ekleme/çıkarma İDEMPOTENT — mobil çift tıklama / offline retry senaryolarında
// 409 üretmek istemci tarafında gereksiz hata yönetimi doğurur. Dönen bool "bu istek bir değişiklik yaptı mı"
// bilgisidir (true=eklendi/silindi, false=zaten öyleydi); iki durumda da HTTP 200.

/// <summary>Favoriye ekleme; yalnız yayında görünür (approved, silinmemiş) ilanlar favorilenebilir — aksi 404.</summary>
public record AddAdFavoriteCommand(Guid AdId, Guid UserId) : IRequest<bool>;

/// <summary>Favoriden çıkarma; ilan sonradan silinmiş/yayından düşmüş olsa da favori kaydı kaldırılabilir.</summary>
public record RemoveAdFavoriteCommand(Guid AdId, Guid UserId) : IRequest<bool>;

public class AddAdFavoriteCommandHandler : IRequestHandler<AddAdFavoriteCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public AddAdFavoriteCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(AddAdFavoriteCommand request, CancellationToken cancellationToken)
    {
        // Detay ucuyla aynı görünürlük kuralı: yayında (approved + süresi geçmemiş) olmayan ilanın
        // varlığı sızdırılmaz (soft-delete filter zaten aktif).
        var adVisible = await _uow.Repository<Ad>().Query()
            .AnyAsync(a => a.Id == request.AdId && a.Status == "approved" && a.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (!adVisible)
            throw new NotFoundException(nameof(Ad), request.AdId);

        var favoriteRepo = _uow.Repository<AdFavorite>();
        var exists = await favoriteRepo.Query()
            .AnyAsync(f => f.AdId == request.AdId && f.UserId == request.UserId, cancellationToken);
        if (exists)
            return false;

        await favoriteRepo.AddAsync(new AdFavorite
        {
            AdId = request.AdId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Yarış: iki eşzamanlı istek unique (UserId, AdId) index'ine takıldı — idempotent davran.
            return false;
        }

        return true;
    }
}

public class RemoveAdFavoriteCommandHandler : IRequestHandler<RemoveAdFavoriteCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RemoveAdFavoriteCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(RemoveAdFavoriteCommand request, CancellationToken cancellationToken)
    {
        var favoriteRepo = _uow.Repository<AdFavorite>();
        var favorite = await favoriteRepo.Query(tracking: true)
            .FirstOrDefaultAsync(f => f.AdId == request.AdId && f.UserId == request.UserId, cancellationToken);
        if (favorite == null)
            return false;

        favoriteRepo.Remove(favorite);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
