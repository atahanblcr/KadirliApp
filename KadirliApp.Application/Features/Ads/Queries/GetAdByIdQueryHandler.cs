using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Ads.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

public class GetAdByIdQueryHandler : IRequestHandler<GetAdByIdQuery, AdDetailDto>
{
    private readonly IUnitOfWork _uow;

    public GetAdByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AdDetailDto> Handle(GetAdByIdQuery request, CancellationToken cancellationToken)
    {
        // AdImage → File navigation'ı yok; URL'ler correlated subquery ile files tablosundan çekiliyor (GetAds deseni).
        var files = _uow.Repository<Domain.Entities.File>().Query();

        var ad = await _uow.Repository<Ad>().Query()
            .Where(x => x.Id == request.Id && x.DeletedAt == null)
            .Select(x => new AdDetailDto(
                x.Id,
                x.Title,
                x.Description,
                x.Price,
                x.Status,
                x.CategoryId,
                x.Category.Name,
                x.UserId,
                x.SellerName,
                x.ContactPhone,
                x.ViewCount,
                x.CreatedAt,
                x.ExpiresAt,
                x.Images
                    .OrderByDescending(i => i.IsCover).ThenBy(i => i.DisplayOrder)
                    .Select(i => new AdImageDto(
                        i.Id,
                        i.FileId,
                        files.Where(f => f.Id == i.FileId).Select(f => f.CdnUrl).FirstOrDefault(),
                        i.IsCover,
                        i.DisplayOrder))
                    .ToList(),
                x.PropertyValues
                    .OrderBy(v => v.Property.DisplayOrder)
                    .Select(v => new AdPropertyValueDto(
                        v.PropertyId,
                        v.Property.PropertyName,
                        v.Property.PropertyType.ToString(),
                        v.Value))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        // Liste/favori kuralıyla aynı görünürlük: approved VE süresi geçmemiş; ExpireAdsJob saatlik koştuğundan
        // yalnız status kontrolü, süresi dolan ilanı bir sonraki job koşusuna dek detayda açık bırakıyordu.
        if (ad is null || ((ad.Status != "approved" || ad.ExpiresAt <= DateTime.UtcNow) && ad.UserId != request.RequesterId))
            throw new NotFoundException(nameof(Ad), request.Id);

        // view_count artışı: tracked entity üzerinden değil, ayrı atomik UPDATE — yarış durumunda kayıp artış olmaz,
        // cache'li bir sorguyu da invalide etmez. Yanıttaki ViewCount artıştan önceki değerdir.
        await _uow.Repository<Ad>().Query()
            .Where(x => x.Id == request.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), cancellationToken);

        return ad;
    }
}
