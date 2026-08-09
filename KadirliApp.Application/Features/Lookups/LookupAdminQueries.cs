using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Lookups;

/// <summary>
/// Faz 10.9(d): panel lookup yönetimi sorguları — public LookupQueries'ten farkı: pasif mahalleler de
/// döner, mekan kategorileri de kapsanır ve CACHE'SİZDİR (admin verisi taze olmalı).
/// Mezarlık/cami/etkinlik kategorisi admin listesi public query'lerden okunur (filtresizler zaten;
/// command'ler lookups grubunu invalidate ettiğinden panel bayat cache görmez).
/// </summary>
public sealed record NeighborhoodAdminDto(
    Guid Id, string Name, string Slug, string? Type, int DisplayOrder, bool IsActive,
    decimal? Latitude, decimal? Longitude, int ResidentCount);

public sealed record PlaceCategoryAdminDto(Guid Id, string Name, string Slug, string? Icon, int DisplayOrder, int PlaceCount);

/// <param name="LocationLabel">Kullanıcıya gösterilen hazır etiket — <see cref="DistrictLabel"/>'dan gelir.</param>
/// <param name="EventCount">Bu ilçeye bağlı etkinlik sayısı (pasifleştirmenin görünür etkisi).</param>
/// <param name="IsHome">Ev ilçesi mi — panel bunu rozetle söyler ve pasifleştirmeyi engeller.</param>
public sealed record DistrictAdminDto(
    Guid Id, string Name, string Slug, string ProvinceName, bool IsCenter,
    int DisplayOrder, bool IsActive, string? LocationLabel, int EventCount, bool IsHome);

/// <summary>Faz 12.4 — panel ilçe yönetimi (pasifler de döner, cache'siz).</summary>
public sealed record GetDistrictsAdminQuery : IRequest<IReadOnlyList<DistrictAdminDto>>;

public sealed class GetDistrictsAdminQueryHandler : IRequestHandler<GetDistrictsAdminQuery, IReadOnlyList<DistrictAdminDto>>
{
    private readonly IUnitOfWork _uow;
    public GetDistrictsAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<DistrictAdminDto>> Handle(GetDistrictsAdminQuery request, CancellationToken ct)
    {
        var eventCounts = await _uow.Repository<Event>().Query()
            .Where(e => e.DistrictId != null)
            .GroupBy(e => e.DistrictId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var items = await _uow.Repository<District>().Query()
            // İl adına göre gruplanabilsin diye önce il, sonra sıra: panel <optgroup> bu sırayı bekliyor.
            .OrderBy(d => d.ProvinceName == DistrictDefaults.HomeProvince ? 0 : 1)
            .ThenBy(d => d.ProvinceName)
            .ThenBy(d => d.DisplayOrder)
            .ThenBy(d => d.Name)
            .ToListAsync(ct);

        return items
            .Select(d => new DistrictAdminDto(
                d.Id, d.Name, d.Slug, d.ProvinceName, d.IsCenter, d.DisplayOrder, d.IsActive,
                DistrictLabel.For(d.Name, d.ProvinceName, d.IsCenter),
                eventCounts.TryGetValue(d.Id, out var c) ? c : 0,
                string.Equals(d.Slug, DistrictDefaults.HomeSlug, StringComparison.Ordinal)))
            .ToList();
    }
}

public sealed record GetNeighborhoodsAdminQuery : IRequest<IReadOnlyList<NeighborhoodAdminDto>>;

public sealed class GetNeighborhoodsAdminQueryHandler : IRequestHandler<GetNeighborhoodsAdminQuery, IReadOnlyList<NeighborhoodAdminDto>>
{
    private readonly IUnitOfWork _uow;
    public GetNeighborhoodsAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<NeighborhoodAdminDto>> Handle(GetNeighborhoodsAdminQuery request, CancellationToken ct)
    {
        // Sakin sayısı: mahalleyi birincil seçen kullanıcılar (pasifleştirme öncesi görünür etki ölçüsü).
        var residents = await _uow.Repository<User>().Query()
            .Where(u => u.PrimaryNeighborhoodId != null)
            .GroupBy(u => u.PrimaryNeighborhoodId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var items = await _uow.Repository<Neighborhood>().Query()
            .OrderBy(n => n.DisplayOrder).ThenBy(n => n.Name)
            .ToListAsync(ct);

        return items
            .Select(n => new NeighborhoodAdminDto(
                n.Id, n.Name, n.Slug, n.Type, n.DisplayOrder, n.IsActive, n.Latitude, n.Longitude,
                residents.TryGetValue(n.Id, out var c) ? c : 0))
            .ToList();
    }
}

public sealed record GetPlaceCategoriesAdminQuery : IRequest<IReadOnlyList<PlaceCategoryAdminDto>>;

public sealed class GetPlaceCategoriesAdminQueryHandler : IRequestHandler<GetPlaceCategoriesAdminQuery, IReadOnlyList<PlaceCategoryAdminDto>>
{
    private readonly IUnitOfWork _uow;
    public GetPlaceCategoriesAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<PlaceCategoryAdminDto>> Handle(GetPlaceCategoriesAdminQuery request, CancellationToken ct)
    {
        var placeCounts = await _uow.Repository<Place>().Query()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var items = await _uow.Repository<PlaceCategory>().Query()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

        return items
            .Select(c => new PlaceCategoryAdminDto(
                c.Id, c.Name, c.Slug, c.Icon, c.DisplayOrder,
                placeCounts.TryGetValue(c.Id, out var n) ? n : 0))
            .ToList();
    }
}
