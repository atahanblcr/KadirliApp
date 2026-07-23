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
