using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Ads.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

public record GetAdImagesQuery(Guid AdId) : IRequest<List<AdImageDto>>;

public class GetAdImagesQueryHandler : IRequestHandler<GetAdImagesQuery, List<AdImageDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAdImagesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<AdImageDto>> Handle(GetAdImagesQuery request, CancellationToken cancellationToken)
    {
        // AdImage → File navigation'ı yok; URL correlated subquery ile çekiliyor.
        var files = _uow.Repository<Domain.Entities.File>().Query();

        return await _uow.Repository<AdImage>().Query()
            .Where(i => i.AdId == request.AdId)
            .OrderByDescending(i => i.IsCover).ThenBy(i => i.DisplayOrder)
            .Select(i => new AdImageDto(
                i.Id,
                i.FileId,
                files.Where(f => f.Id == i.FileId).Select(f => f.CdnUrl).FirstOrDefault(),
                i.IsCover,
                i.DisplayOrder
            ))
            .ToListAsync(cancellationToken);
    }
}
