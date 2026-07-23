using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Commands.UpdateMyAd;

/// <summary>
/// Faz 10.6: kullanıcının KENDİ ilanını güncellemesi (PUT /v1/ads/{id}). Admin'in UpdateAdCommand'inden farkları:
/// sahiplik kontrolü (403), kategori DEĞİŞTİRİLEMEZ (property tanımları kategoriye bağlı), user-submission
/// validasyonları (cep telefonu formatı, görsel sahipliği, zorunlu property denetimi) ve her düzenlemenin ilanı
/// yeniden admin onayına (pending) düşürmesi — onaylı içerik onaysız değiştirilemesin.
/// </summary>
public class UpdateMyAdCommand : IRequest<bool>
{
    /// <summary>Route'tan set edilir, body'den bind edilmez.</summary>
    public Guid Id { get; set; }

    /// <summary>Claim'den set edilir, body'den bind edilmez.</summary>
    public Guid UserId { get; set; }

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;

    /// <summary>Eklenecek yeni görsel dosya id'leri (kullanıcının kendi yükledikleri).</summary>
    public List<Guid> NewImageFileIds { get; set; } = new();

    /// <summary>Silinecek ad_images kayıt id'leri (AdImage.Id — file id değil; detay yanıtındaki images[].id).</summary>
    public List<Guid> RemoveImageIds { get; set; } = new();

    /// <summary>null → property değerlerine dokunulmaz; gönderilirse mevcut değerlerin TAMAMI bununla değiştirilir (zorunlu alan denetimi dahil).</summary>
    public Dictionary<Guid, string>? PropertyValues { get; set; }
}

public class UpdateMyAdCommandHandler : IRequestHandler<UpdateMyAdCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateMyAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateMyAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (ad == null)
            throw new NotFoundException(nameof(Ad), request.Id);
        if (ad.UserId != request.UserId)
            throw new ForbiddenException("Bu ilan üzerinde işlem yetkiniz yok.");

        AdSubmissionRules.ValidateContent(request.Title, request.Description, request.Price, request.ContactPhone, isUserSubmission: true);

        var imageRepo = _uow.Repository<AdImage>();
        var newImageFileIds = request.NewImageFileIds.Distinct().ToList();
        var removeImageIds = request.RemoveImageIds.Distinct().ToList();

        var currentCount = await imageRepo.Query().CountAsync(i => i.AdId == ad.Id, cancellationToken);
        var removeCount = removeImageIds.Count == 0
            ? 0
            : await imageRepo.Query().CountAsync(i => i.AdId == ad.Id && removeImageIds.Contains(i.Id), cancellationToken);
        if (currentCount - removeCount + newImageFileIds.Count > AdSubmissionRules.MaxImages)
            throw new ValidationException($"Bir ilana en fazla {AdSubmissionRules.MaxImages} görsel eklenebilir.");

        await AdSubmissionRules.ValidateImageOwnershipAsync(_uow, newImageFileIds, request.UserId, cancellationToken);

        List<(Guid PropertyId, string Value)>? newPropertyValues = null;
        if (request.PropertyValues != null)
            newPropertyValues = await AdSubmissionRules.ValidatePropertyValuesAsync(
                _uow, ad.CategoryId, request.PropertyValues, isUserSubmission: true, cancellationToken);

        ad.Title = request.Title.Trim();
        ad.Description = request.Description;
        ad.Price = request.Price;
        ad.SellerName = request.SellerName;
        ad.ContactPhone = request.ContactPhone.Trim();

        // Her kullanıcı düzenlemesi yeniden moderasyona düşer; önceki onay/red izleri temizlenir
        // (rejected ilanın düzeltilip yeniden gönderilme yolu da budur). ExpiresAt'e DOKUNULMAZ — süre işi extend'in.
        ad.Status = "pending";
        ad.ApprovedBy = null;
        ad.ApprovedAt = null;
        ad.RejectedReason = null;
        ad.RejectedAt = null;

        if (removeImageIds.Count > 0)
        {
            var toRemove = await imageRepo.Query()
                .Where(i => i.AdId == ad.Id && removeImageIds.Contains(i.Id))
                .ToListAsync(cancellationToken);
            foreach (var image in toRemove)
                imageRepo.Remove(image);
        }

        if (newImageFileIds.Count > 0)
        {
            var maxOrder = await imageRepo.Query()
                .Where(i => i.AdId == ad.Id)
                .Select(i => (int?)i.DisplayOrder)
                .MaxAsync(cancellationToken) ?? -1;

            foreach (var fileId in newImageFileIds)
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

        if (newPropertyValues != null)
        {
            var valueRepo = _uow.Repository<AdPropertyValue>();
            var existing = await valueRepo.Query(tracking: true)
                .Where(v => v.AdId == ad.Id)
                .ToListAsync(cancellationToken);
            foreach (var value in existing)
                valueRepo.Remove(value);
            foreach (var (propertyId, value) in newPropertyValues)
                await valueRepo.AddAsync(new AdPropertyValue { AdId = ad.Id, PropertyId = propertyId, Value = value }, cancellationToken);
        }

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        // Kapak silinmiş ya da hiç kapak kalmamış olabilir — en düşük sıradaki görseli kapak yap (UpdateAd deseni).
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
