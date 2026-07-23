using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Taxis.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Taxis.Queries;

/// <summary>
/// OnlyPublic=true (public uç): yalnız doğrulanmış VE aktif sürücüler döner; istemcinin
/// ?isVerified=/?isActive= parametreleri yok sayılır (telefon numarası sızıntısı). Admin varsayılanla (false) filtreleri kullanır.
/// </summary>
public class GetTaxiDriversQuery : IRequest<PagedResult<TaxiDriverResponseDto>>
{
    public QueryTaxiDriverDto QueryDto { get; set; }
    public bool OnlyPublic { get; set; }

    public GetTaxiDriversQuery(QueryTaxiDriverDto queryDto, bool onlyPublic = false)
    {
        QueryDto = queryDto;
        OnlyPublic = onlyPublic;
    }
}
