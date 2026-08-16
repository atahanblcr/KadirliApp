using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Commands.CreateAd;

public class CreateAdCommandHandler : IRequestHandler<CreateAdCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateAdCommand request, CancellationToken cancellationToken)
    {
        // Faz 10.5 validasyonu — 10.6'da UpdateMyAd ile paylaşılmak üzere AdSubmissionRules'a çıkarıldı.
        // (⚠️ 12.19b: burada "Ads/Validators altındaki FluentValidation sınıfları pipeline'a
        //  kayıtlı değil" diyen bir satır vardı; o klasör ARTIK YOK ve projede tek bir
        //  AbstractValidator kalmadı — doğrulama tümüyle elle, bu tek noktadan geçiyor.)
        AdSubmissionRules.ValidateContent(request.Title, request.Description, request.Price, request.ContactPhone, request.IsUserSubmission);

        var categoryValid = await _uow.Repository<AdCategory>().Query()
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);
        if (!categoryValid)
            throw new ValidationException("Geçersiz veya pasif ilan kategorisi.");

        var imageFileIds = request.ImageFileIds.Distinct().ToList();
        if (imageFileIds.Count > AdSubmissionRules.MaxImages)
            throw new ValidationException($"Bir ilana en fazla {AdSubmissionRules.MaxImages} görsel eklenebilir.");

        if (request.IsUserSubmission)
            await AdSubmissionRules.ValidateImageOwnershipAsync(_uow, imageFileIds, request.UserId, cancellationToken);

        var propertyValues = await AdSubmissionRules.ValidatePropertyValuesAsync(
            _uow, request.CategoryId, request.PropertyValues, request.IsUserSubmission, cancellationToken);

        var ad = new Ad
        {
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Price = request.Price,
            SellerName = request.SellerName,
            ContactPhone = request.ContactPhone.Trim(),
            UserId = request.UserId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        for (var i = 0; i < imageFileIds.Count; i++)
        {
            ad.Images.Add(new AdImage
            {
                FileId = imageFileIds[i],
                IsCover = i == 0,
                DisplayOrder = i
            });
        }

        foreach (var (propertyId, value) in propertyValues)
            ad.PropertyValues.Add(new AdPropertyValue { PropertyId = propertyId, Value = value });

        await _uow.Repository<Ad>().AddAsync(ad, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ad.Id;
    }
}
