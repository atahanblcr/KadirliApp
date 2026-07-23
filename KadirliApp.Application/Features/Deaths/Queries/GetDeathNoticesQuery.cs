using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Deaths.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Queries;

/// <summary>
/// OnlyPublished=true (public uç): yalnız "approved" ilanlar döner; istemcinin ?status= parametresi
/// yok sayılır (archived dahil edilmez — arşiv admin-işi). Admin/panel varsayılanla (false) tüm statüleri görür.
/// </summary>
public record GetDeathNoticesQuery(QueryDeathNoticeDto Dto, bool OnlyPublished = false) : IRequest<PagedResult<DeathNoticeResponseDto>>;
