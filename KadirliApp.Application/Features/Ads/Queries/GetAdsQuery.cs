using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Ads.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Queries;

/// <summary>OnlyPublished=true (public uç): yalnız onaylı ve süresi geçmemiş ilanlar. Admin/panel varsayılanla (false) tüm statüleri görür.</summary>
public record GetAdsQuery(QueryAdDto Dto, bool OnlyPublished = false) : IRequest<PagedResult<AdResponseDto>>;
