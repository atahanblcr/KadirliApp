using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Queries;

/// <summary>
/// Faz 10.9(c): panel/admin tarafı kategori-özellik yönetimi sorguları — public AdCategoryQueries'ten
/// farkı: pasifler de döner, sayaçlar (alt kategori/özellik/ilan) yönetim için eklidir ve CACHE'SİZDİR
/// (admin verisi taze olmalı — GetBusinessesQuery emsali).
/// </summary>
public sealed record AdCategoryAdminDto(
    Guid Id, string Name, string Slug, Guid? ParentId, string? ParentName, string? Icon,
    int DisplayOrder, bool IsActive, int SubCategoryCount, int PropertyCount, int AdCount);

public sealed record PropertyOptionAdminDto(Guid Id, string OptionValue, int DisplayOrder);

public sealed record CategoryPropertyAdminDto(
    Guid Id, Guid CategoryId, string PropertyName, string PropertyType, bool IsRequired,
    string? DefaultValue, int DisplayOrder, int UsageCount, List<PropertyOptionAdminDto> Options);

/// <summary>Tüm kategoriler düz liste (pasifler dahil) — panel ağacı istemci tarafında kurar.</summary>
public sealed record GetAdCategoriesAdminQuery : IRequest<IReadOnlyList<AdCategoryAdminDto>>;

public sealed class GetAdCategoriesAdminQueryHandler : IRequestHandler<GetAdCategoriesAdminQuery, IReadOnlyList<AdCategoryAdminDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAdCategoriesAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AdCategoryAdminDto>> Handle(GetAdCategoriesAdminQuery request, CancellationToken ct)
    {
        // İlan sayısı soft-silinmişler DAHİL (IgnoreQueryFilters) — silme kuralıyla aynı ölçüt,
        // panel "neden silemiyorum" sorusunun cevabını görebilsin.
        var adCounts = await _uow.Repository<Ad>().Query().IgnoreQueryFilters()
            .GroupBy(a => a.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var categories = await _uow.Repository<AdCategory>().Query()
            .OrderBy(c => c.ParentId == null ? 0 : 1).ThenBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id, c.Name, c.Slug, c.ParentId,
                ParentName = c.Parent != null ? c.Parent.Name : null,
                c.Icon, c.DisplayOrder, c.IsActive,
                SubCategoryCount = c.SubCategories.Count,
                PropertyCount = c.Properties.Count
            })
            .ToListAsync(ct);

        return categories
            .Select(c => new AdCategoryAdminDto(
                c.Id, c.Name, c.Slug, c.ParentId, c.ParentName, c.Icon, c.DisplayOrder, c.IsActive,
                c.SubCategoryCount, c.PropertyCount, adCounts.TryGetValue(c.Id, out var n) ? n : 0))
            .ToList();
    }
}

/// <summary>Bir kategorinin özellikleri (seçenekler + kaç ilanda kullanıldığı ile) — kategori yoksa 404.</summary>
public sealed record GetCategoryPropertiesAdminQuery(Guid CategoryId) : IRequest<IReadOnlyList<CategoryPropertyAdminDto>>;

public sealed class GetCategoryPropertiesAdminQueryHandler : IRequestHandler<GetCategoryPropertiesAdminQuery, IReadOnlyList<CategoryPropertyAdminDto>>
{
    private readonly IUnitOfWork _uow;
    public GetCategoryPropertiesAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<CategoryPropertyAdminDto>> Handle(GetCategoryPropertiesAdminQuery request, CancellationToken ct)
    {
        if (!await _uow.Repository<AdCategory>().Query().AnyAsync(c => c.Id == request.CategoryId, ct))
            throw new NotFoundException(nameof(AdCategory), request.CategoryId);

        var usage = await _uow.Repository<AdPropertyValue>().Query()
            .Where(v => v.Property.CategoryId == request.CategoryId)
            .GroupBy(v => v.PropertyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var properties = await _uow.Repository<CategoryProperty>().Query()
            .Where(p => p.CategoryId == request.CategoryId)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.PropertyName)
            .Select(p => new
            {
                p.Id, p.CategoryId, p.PropertyName, PropertyType = p.PropertyType.ToString(),
                p.IsRequired, p.DefaultValue, p.DisplayOrder,
                Options = p.Options
                    .OrderBy(o => o.DisplayOrder).ThenBy(o => o.OptionValue)
                    .Select(o => new PropertyOptionAdminDto(o.Id, o.OptionValue, o.DisplayOrder))
                    .ToList()
            })
            .ToListAsync(ct);

        return properties
            .Select(p => new CategoryPropertyAdminDto(
                p.Id, p.CategoryId, p.PropertyName, p.PropertyType, p.IsRequired, p.DefaultValue,
                p.DisplayOrder, usage.TryGetValue(p.Id, out var n) ? n : 0, p.Options))
            .ToList();
    }
}

/// <summary>
/// Faz 10.9(g): moderasyon ekranı için bir ilanın kategoriye özel alan değerleri (salt-okunur).
/// </summary>
public sealed record AdPropertyValueDisplayDto(string PropertyName, string PropertyType, string Value);

public sealed record GetAdPropertyValuesQuery(Guid AdId) : IRequest<IReadOnlyList<AdPropertyValueDisplayDto>>;

public sealed class GetAdPropertyValuesQueryHandler : IRequestHandler<GetAdPropertyValuesQuery, IReadOnlyList<AdPropertyValueDisplayDto>>
{
    private readonly IUnitOfWork _uow;
    public GetAdPropertyValuesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AdPropertyValueDisplayDto>> Handle(GetAdPropertyValuesQuery request, CancellationToken ct)
        => await _uow.Repository<AdPropertyValue>().Query()
            .Where(v => v.AdId == request.AdId)
            .OrderBy(v => v.Property.DisplayOrder).ThenBy(v => v.Property.PropertyName)
            .Select(v => new AdPropertyValueDisplayDto(v.Property.PropertyName, v.Property.PropertyType.ToString(), v.Value))
            .ToListAsync(ct);
}
