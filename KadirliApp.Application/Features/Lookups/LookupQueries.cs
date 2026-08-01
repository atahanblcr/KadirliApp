using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Lookups;

/// <summary>
/// Faz 10.4: mobilin form/filtre ekranları için public lookup query'leri.
/// Hepsi `lookups` cache grubunda 15 dk TTL.
/// 10.9(d): artık admin CRUD'u var — LookupCommands grup invalidation'ı yapar, cache taze kalır.
/// </summary>
public sealed record NeighborhoodLookupDto(
    Guid Id, string Name, string Slug, string? Type,
    decimal? Latitude, decimal? Longitude, int DisplayOrder);

public sealed record NamedLookupDto(Guid Id, string Name, string? Address, decimal? Latitude, decimal? Longitude);

public sealed record SluggedLookupDto(Guid Id, string Name, string Slug);

public sealed record GetNeighborhoodsQuery : IRequest<IReadOnlyList<NeighborhoodLookupDto>>, ICacheableQuery
{
    public string CacheKey => "lookups:neighborhoods";
    public string CacheGroup => CacheGroups.Lookups;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetNeighborhoodsQueryHandler : IRequestHandler<GetNeighborhoodsQuery, IReadOnlyList<NeighborhoodLookupDto>>
{
    private readonly IUnitOfWork _uow;
    public GetNeighborhoodsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<NeighborhoodLookupDto>> Handle(GetNeighborhoodsQuery request, CancellationToken ct)
        => await _uow.Repository<Neighborhood>().Query()
            .Where(n => n.IsActive)
            .OrderBy(n => n.DisplayOrder).ThenBy(n => n.Name)
            .Select(n => new NeighborhoodLookupDto(n.Id, n.Name, n.Slug, n.Type, n.Latitude, n.Longitude, n.DisplayOrder))
            .ToListAsync(ct);
}

public sealed record GetCemeteriesQuery : IRequest<IReadOnlyList<NamedLookupDto>>, ICacheableQuery
{
    public string CacheKey => "lookups:cemeteries";
    public string CacheGroup => CacheGroups.Lookups;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetCemeteriesQueryHandler : IRequestHandler<GetCemeteriesQuery, IReadOnlyList<NamedLookupDto>>
{
    private readonly IUnitOfWork _uow;
    public GetCemeteriesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<NamedLookupDto>> Handle(GetCemeteriesQuery request, CancellationToken ct)
        => await _uow.Repository<Cemetery>().Query()
            .OrderBy(c => c.Name)
            .Select(c => new NamedLookupDto(c.Id, c.Name, c.Address, c.Latitude, c.Longitude))
            .ToListAsync(ct);
}

public sealed record GetMosquesQuery : IRequest<IReadOnlyList<NamedLookupDto>>, ICacheableQuery
{
    public string CacheKey => "lookups:mosques";
    public string CacheGroup => CacheGroups.Lookups;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetMosquesQueryHandler : IRequestHandler<GetMosquesQuery, IReadOnlyList<NamedLookupDto>>
{
    private readonly IUnitOfWork _uow;
    public GetMosquesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<NamedLookupDto>> Handle(GetMosquesQuery request, CancellationToken ct)
        => await _uow.Repository<Mosque>().Query()
            .OrderBy(m => m.Name)
            .Select(m => new NamedLookupDto(m.Id, m.Name, m.Address, m.Latitude, m.Longitude))
            .ToListAsync(ct);
}

public sealed record GetEventCategoriesQuery : IRequest<IReadOnlyList<SluggedLookupDto>>, ICacheableQuery
{
    public string CacheKey => "lookups:event-categories";
    public string CacheGroup => CacheGroups.Lookups;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetEventCategoriesQueryHandler : IRequestHandler<GetEventCategoriesQuery, IReadOnlyList<SluggedLookupDto>>
{
    private readonly IUnitOfWork _uow;
    public GetEventCategoriesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<SluggedLookupDto>> Handle(GetEventCategoriesQuery request, CancellationToken ct)
        => await _uow.Repository<EventCategory>().Query()
            .OrderBy(c => c.Name)
            .Select(c => new SluggedLookupDto(c.Id, c.Name, c.Slug))
            .ToListAsync(ct);
}

/// <summary>
/// Faz 11.11: mobil mekan filtresi için kategori lookup'ı.
/// `PlaceResponseDto` yalnız `CategoryId` taşıyor; public bir kategori ucu
/// olmadan mobil ne kategori adını yazabiliyor ne de filtre chip'i çizebiliyordu
/// ("işlevsiz buton yok" kuralı olmayan listeden chip üretmeyi de yasaklıyor).
/// Etkinlik kategorileriyle bire bir aynı desen — additive, kontrat kırılmaz.
/// </summary>
public sealed record GetPlaceCategoriesQuery : IRequest<IReadOnlyList<SluggedLookupDto>>, ICacheableQuery
{
    public string CacheKey => "lookups:place-categories";
    public string CacheGroup => CacheGroups.Lookups;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}

public sealed class GetPlaceCategoriesQueryHandler : IRequestHandler<GetPlaceCategoriesQuery, IReadOnlyList<SluggedLookupDto>>
{
    private readonly IUnitOfWork _uow;
    public GetPlaceCategoriesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<SluggedLookupDto>> Handle(GetPlaceCategoriesQuery request, CancellationToken ct)
        => await _uow.Repository<PlaceCategory>().Query()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new SluggedLookupDto(c.Id, c.Name, c.Slug))
            .ToListAsync(ct);
}
