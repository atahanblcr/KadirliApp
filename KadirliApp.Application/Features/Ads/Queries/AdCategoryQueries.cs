using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

/// <summary>
/// Faz 10.5: mobilin ilan verme/filtreleme ekranları için kategori ağacı ve
/// kategoriye özel alan (property) lookup'ları. `ads-lookup` grubunda 15 dk TTL.
/// 10.9(c): artık admin CRUD'u var — AdCategoryCommands grup invalidation'ı yapar, cache taze kalır.
/// </summary>
public sealed record AdCategoryDto(
    Guid Id, string Name, string Slug, Guid? ParentId, string? Icon,
    int DisplayOrder, int SubCategoryCount);

public sealed record PropertyOptionDto(Guid Id, string OptionValue, int DisplayOrder);

public sealed record CategoryPropertyDto(
    Guid Id, string PropertyName, string PropertyType, bool IsRequired,
    string? DefaultValue, int DisplayOrder, List<PropertyOptionDto> Options);

/// <summary>ParentId null → kök kategoriler; dolu → o kategorinin alt kategorileri.</summary>
public sealed record GetAdCategoriesQuery(Guid? ParentId) : IRequest<IReadOnlyList<AdCategoryDto>>, ICacheableQuery
{
    public string CacheKey => $"ads:categories:{(ParentId.HasValue ? ParentId.Value.ToString() : "root")}";
    public string CacheGroup => CacheGroups.AdsLookup;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetAdCategoriesQueryHandler : IRequestHandler<GetAdCategoriesQuery, IReadOnlyList<AdCategoryDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAdCategoriesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AdCategoryDto>> Handle(GetAdCategoriesQuery request, CancellationToken ct)
        => await _uow.Repository<AdCategory>().Query()
            .Where(c => c.IsActive && c.ParentId == request.ParentId)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new AdCategoryDto(
                c.Id, c.Name, c.Slug, c.ParentId, c.Icon, c.DisplayOrder,
                c.SubCategories.Count(s => s.IsActive)))
            .ToListAsync(ct);
}

public sealed record GetCategoryPropertiesQuery(Guid CategoryId) : IRequest<IReadOnlyList<CategoryPropertyDto>>, ICacheableQuery
{
    public string CacheKey => $"ads:category-properties:{CategoryId}";
    public string CacheGroup => CacheGroups.AdsLookup;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetCategoryPropertiesQueryHandler : IRequestHandler<GetCategoryPropertiesQuery, IReadOnlyList<CategoryPropertyDto>>
{
    private readonly IUnitOfWork _uow;
    public GetCategoryPropertiesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<CategoryPropertyDto>> Handle(GetCategoryPropertiesQuery request, CancellationToken ct)
    {
        var categoryExists = await _uow.Repository<AdCategory>().Query()
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, ct);
        if (!categoryExists)
            throw new NotFoundException(nameof(AdCategory), request.CategoryId);

        return await _uow.Repository<CategoryProperty>().Query()
            .Where(p => p.CategoryId == request.CategoryId)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.PropertyName)
            .Select(p => new CategoryPropertyDto(
                p.Id, p.PropertyName, p.PropertyType.ToString(), p.IsRequired,
                p.DefaultValue, p.DisplayOrder,
                p.Options
                    .OrderBy(o => o.DisplayOrder).ThenBy(o => o.OptionValue)
                    .Select(o => new PropertyOptionDto(o.Id, o.OptionValue, o.DisplayOrder))
                    .ToList()))
            .ToListAsync(ct);
    }
}
