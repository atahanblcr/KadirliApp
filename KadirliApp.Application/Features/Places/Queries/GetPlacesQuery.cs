using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Places.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Places.Queries;

/// <summary>OnlyActive=true (public uç): yalnız aktif mekanlar döner. Admin/panel varsayılanla (false) pasifleri de görür.</summary>
public record GetPlacesQuery(QueryPlaceDto Dto, bool OnlyActive = false) : IRequest<PagedResult<PlaceResponseDto>>;
