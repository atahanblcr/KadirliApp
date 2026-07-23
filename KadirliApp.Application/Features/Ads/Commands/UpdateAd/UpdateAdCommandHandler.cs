using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Commands.UpdateAd;

public class UpdateAdCommandHandler : IRequestHandler<UpdateAdCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (ad == null) return false;

        ad.CategoryId = request.CategoryId;
        ad.Title = request.Title;
        ad.Description = request.Description;
        ad.Price = request.Price;
        ad.SellerName = request.SellerName;
        ad.ContactPhone = request.ContactPhone;
        ad.Status = request.Status;

        var imageRepo = _uow.Repository<AdImage>();

        if (request.RemoveImageIds.Count > 0)
        {
            var toRemove = await imageRepo.Query()
                .Where(i => i.AdId == ad.Id && request.RemoveImageIds.Contains(i.Id))
                .ToListAsync(cancellationToken);
            foreach (var image in toRemove)
                imageRepo.Remove(image);
        }

        if (request.NewImageFileIds.Count > 0)
        {
            var maxOrder = await imageRepo.Query()
                .Where(i => i.AdId == ad.Id)
                .Select(i => (int?)i.DisplayOrder)
                .MaxAsync(cancellationToken) ?? -1;

            foreach (var fileId in request.NewImageFileIds)
            {
                await imageRepo.AddAsync(new AdImage
                {
                    AdId = ad.Id,
                    FileId = fileId,
                    IsCover = false,
                    DisplayOrder = ++maxOrder
                }, cancellationToken);
            }
        }

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        // Kapak silinmiş ya da hiç kapak kalmamış olabilir — en düşük sıradaki görseli kapak yap.
        var remaining = await imageRepo.Query(tracking: true)
            .Where(i => i.AdId == ad.Id)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(cancellationToken);
        if (remaining.Count > 0 && !remaining.Any(i => i.IsCover))
        {
            remaining[0].IsCover = true;
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
