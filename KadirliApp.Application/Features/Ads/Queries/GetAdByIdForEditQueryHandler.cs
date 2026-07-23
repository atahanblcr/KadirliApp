using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Application.Features.Ads.Commands.UpdateAd;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Queries;

public class GetAdByIdForEditQueryHandler : IRequestHandler<GetAdByIdForEditQuery, UpdateAdCommand?>
{
    private readonly IUnitOfWork _uow;

    public GetAdByIdForEditQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UpdateAdCommand?> Handle(GetAdByIdForEditQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (ad == null) return null;

        return new UpdateAdCommand
        {
            Id = ad.Id,
            CategoryId = ad.CategoryId,
            Title = ad.Title,
            Description = ad.Description,
            Price = ad.Price,
            SellerName = ad.SellerName,
            ContactPhone = ad.ContactPhone,
            Status = ad.Status
        };
    }
}
