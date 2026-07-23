using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Transport.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Transport.Queries;

/// <summary>OnlyActive=true (public uç): yalnız aktif hatlar döner. Admin/panel varsayılanla (false) pasifleri de görür.</summary>
public class GetIntercityRoutesQuery : IRequest<PagedResult<IntercityRouteResponseDto>>
{
    public QueryTransportDto QueryDto { get; set; }
    public bool OnlyActive { get; set; }

    public GetIntercityRoutesQuery(QueryTransportDto queryDto, bool onlyActive = false)
    {
        QueryDto = queryDto;
        OnlyActive = onlyActive;
    }
}
