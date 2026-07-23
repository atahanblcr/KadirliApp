using System;
using KadirliApp.Application.Features.Taxis.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Queries;

/// <summary>OnlyPublic=true (public uç): doğrulanmamış/pasif sürücü id bilinse bile dönmez (null → 404).</summary>
public class GetTaxiDriverByIdQuery : IRequest<TaxiDriverResponseDto?>
{
    public Guid Id { get; set; }
    public bool OnlyPublic { get; set; }

    public GetTaxiDriverByIdQuery(Guid id, bool onlyPublic = false)
    {
        Id = id;
        OnlyPublic = onlyPublic;
    }
}
