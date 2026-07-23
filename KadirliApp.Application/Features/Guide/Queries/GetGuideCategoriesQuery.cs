using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Guide.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Guide.Queries;

public record GetGuideCategoriesQuery(QueryGuideCategoryDto Dto)
    : IRequest<PagedResult<GuideCategoryResponseDto>>, ICacheableQuery
{
    public string CacheKey => $"guide:categories:p{Dto.Page}:l{Dto.Limit}:s{Dto.Search}:par{Dto.ParentId}";
    public string CacheGroup => CacheGroups.Guide;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}
