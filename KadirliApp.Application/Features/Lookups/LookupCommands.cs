using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Utils;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Lookups;

/// <summary>
/// Faz 10.9(d): lookup tablolarının (mahalle/mezarlık/cami/etkinlik+mekan kategorisi) CRUD'u ilk kez
/// yazıldı — öncesinde YALNIZ DbSeeder dolduruyordu. KARAR: silme YOK (hepsine FK referansları var —
/// vefat→mezarlık/cami, kullanıcı→mahalle, etkinlik/mekan→kategori); pasifleştirme/pasif kalma yeterli
/// (IsActive olmayan entity'lerde kayıt kalıcıdır, yalnız ad/adres düzeltilir).
/// Tüm command'ler `lookups` cache grubunu invalidate eder (10.4 notundaki şart).
/// </summary>
public abstract record LookupsInvalidatorBase : ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Lookups };
}

// ---------- Mahalle ----------

public sealed record CreateNeighborhoodCommand(string Name, string? Type, int DisplayOrder,
    decimal? Latitude = null, decimal? Longitude = null)
    : LookupsInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "create-neighborhood";
    public string? AuditAffectedType => "Neighborhood";
    public object? AuditDetails => new { name = Name };
}

public sealed class CreateNeighborhoodCommandHandler : IRequestHandler<CreateNeighborhoodCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateNeighborhoodCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateNeighborhoodCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Neighborhood>();
        var (name, slug) = await LookupRules.ValidateSluggedNameAsync(
            repo.Query(), request.Name, excludeId: null, "mahalle", ct);

        var neighborhood = new Neighborhood
        {
            Name = name,
            Slug = slug,
            Type = LookupRules.Clean(request.Type),
            DisplayOrder = request.DisplayOrder,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        await repo.AddAsync(neighborhood, ct);
        await _uow.SaveChangesAsync(ct);
        return neighborhood.Id;
    }
}

public sealed record UpdateNeighborhoodCommand(Guid Id, string Name, string? Type, int DisplayOrder, bool IsActive,
    decimal? Latitude = null, decimal? Longitude = null)
    : LookupsInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "update-neighborhood";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Neighborhood";
    public object? AuditDetails => new { name = Name, isActive = IsActive };
}

public sealed class UpdateNeighborhoodCommandHandler : IRequestHandler<UpdateNeighborhoodCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public UpdateNeighborhoodCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateNeighborhoodCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Neighborhood>();
        var neighborhood = await repo.GetByIdAsync(request.Id, ct);
        if (neighborhood == null) return false;

        var (name, slug) = await LookupRules.ValidateSluggedNameAsync(
            repo.Query(), request.Name, excludeId: request.Id, "mahalle", ct);

        neighborhood.Name = name;
        neighborhood.Slug = slug;
        neighborhood.Type = LookupRules.Clean(request.Type);
        neighborhood.DisplayOrder = request.DisplayOrder;
        neighborhood.IsActive = request.IsActive;
        neighborhood.Latitude = request.Latitude;
        neighborhood.Longitude = request.Longitude;

        repo.Update(neighborhood);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

// ---------- Mezarlık / Cami (aynı şekil — ad + adres + koordinat) ----------

public sealed record CreateCemeteryCommand(string Name, string? Address, decimal? Latitude = null, decimal? Longitude = null)
    : LookupsInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "create-cemetery";
    public string? AuditAffectedType => "Cemetery";
    public object? AuditDetails => new { name = Name };
}

public sealed class CreateCemeteryCommandHandler : IRequestHandler<CreateCemeteryCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateCemeteryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateCemeteryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Cemetery>();
        var name = await LookupRules.ValidateUniqueNameAsync(repo.Query(), request.Name, null, "mezarlık", ct);

        var cemetery = new Cemetery
        {
            Name = name,
            Address = LookupRules.Clean(request.Address),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };
        await repo.AddAsync(cemetery, ct);
        await _uow.SaveChangesAsync(ct);
        return cemetery.Id;
    }
}

public sealed record UpdateCemeteryCommand(Guid Id, string Name, string? Address, decimal? Latitude = null, decimal? Longitude = null)
    : LookupsInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "update-cemetery";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Cemetery";
    public object? AuditDetails => new { name = Name };
}

public sealed class UpdateCemeteryCommandHandler : IRequestHandler<UpdateCemeteryCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public UpdateCemeteryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateCemeteryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Cemetery>();
        var cemetery = await repo.GetByIdAsync(request.Id, ct);
        if (cemetery == null) return false;

        cemetery.Name = await LookupRules.ValidateUniqueNameAsync(repo.Query(), request.Name, request.Id, "mezarlık", ct);
        cemetery.Address = LookupRules.Clean(request.Address);
        cemetery.Latitude = request.Latitude;
        cemetery.Longitude = request.Longitude;

        repo.Update(cemetery);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record CreateMosqueCommand(string Name, string? Address, decimal? Latitude = null, decimal? Longitude = null)
    : LookupsInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "create-mosque";
    public string? AuditAffectedType => "Mosque";
    public object? AuditDetails => new { name = Name };
}

public sealed class CreateMosqueCommandHandler : IRequestHandler<CreateMosqueCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateMosqueCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateMosqueCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Mosque>();
        var name = await LookupRules.ValidateUniqueNameAsync(repo.Query(), request.Name, null, "cami", ct);

        var mosque = new Mosque
        {
            Name = name,
            Address = LookupRules.Clean(request.Address),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };
        await repo.AddAsync(mosque, ct);
        await _uow.SaveChangesAsync(ct);
        return mosque.Id;
    }
}

public sealed record UpdateMosqueCommand(Guid Id, string Name, string? Address, decimal? Latitude = null, decimal? Longitude = null)
    : LookupsInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "update-mosque";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Mosque";
    public object? AuditDetails => new { name = Name };
}

public sealed class UpdateMosqueCommandHandler : IRequestHandler<UpdateMosqueCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public UpdateMosqueCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateMosqueCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Mosque>();
        var mosque = await repo.GetByIdAsync(request.Id, ct);
        if (mosque == null) return false;

        mosque.Name = await LookupRules.ValidateUniqueNameAsync(repo.Query(), request.Name, request.Id, "cami", ct);
        mosque.Address = LookupRules.Clean(request.Address);
        mosque.Latitude = request.Latitude;
        mosque.Longitude = request.Longitude;

        repo.Update(mosque);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

// ---------- Etkinlik kategorisi ----------

public sealed record CreateEventCategoryCommand(string Name)
    : LookupsInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "create-event-category";
    public string? AuditAffectedType => "EventCategory";
    public object? AuditDetails => new { name = Name };
}

public sealed class CreateEventCategoryCommandHandler : IRequestHandler<CreateEventCategoryCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateEventCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateEventCategoryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<EventCategory>();
        var (name, slug) = await LookupRules.ValidateSluggedNameAsync(
            repo.Query(), request.Name, excludeId: null, "etkinlik kategorisi", ct);

        var category = new EventCategory { Name = name, Slug = slug };
        await repo.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);
        return category.Id;
    }
}

public sealed record UpdateEventCategoryCommand(Guid Id, string Name)
    : LookupsInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "update-event-category";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "EventCategory";
    public object? AuditDetails => new { name = Name };
}

public sealed class UpdateEventCategoryCommandHandler : IRequestHandler<UpdateEventCategoryCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public UpdateEventCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateEventCategoryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<EventCategory>();
        var category = await repo.GetByIdAsync(request.Id, ct);
        if (category == null) return false;

        var (name, slug) = await LookupRules.ValidateSluggedNameAsync(
            repo.Query(), request.Name, excludeId: request.Id, "etkinlik kategorisi", ct);

        category.Name = name;
        category.Slug = slug;

        repo.Update(category);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

// ---------- Mekan kategorisi ----------

public sealed record CreatePlaceCategoryCommand(string Name, string? Icon, int DisplayOrder)
    : LookupsInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "create-place-category";
    public string? AuditAffectedType => "PlaceCategory";
    public object? AuditDetails => new { name = Name };
}

public sealed class CreatePlaceCategoryCommandHandler : IRequestHandler<CreatePlaceCategoryCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreatePlaceCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreatePlaceCategoryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<PlaceCategory>();
        var (name, slug) = await LookupRules.ValidateSluggedNameAsync(
            repo.Query(), request.Name, excludeId: null, "mekan kategorisi", ct);

        var category = new PlaceCategory
        {
            Name = name,
            Slug = slug,
            Icon = LookupRules.Clean(request.Icon),
            DisplayOrder = request.DisplayOrder
        };
        await repo.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);
        return category.Id;
    }
}

public sealed record UpdatePlaceCategoryCommand(Guid Id, string Name, string? Icon, int DisplayOrder)
    : LookupsInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "lookups";
    public string AuditAction => "update-place-category";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "PlaceCategory";
    public object? AuditDetails => new { name = Name };
}

public sealed class UpdatePlaceCategoryCommandHandler : IRequestHandler<UpdatePlaceCategoryCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public UpdatePlaceCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdatePlaceCategoryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<PlaceCategory>();
        var category = await repo.GetByIdAsync(request.Id, ct);
        if (category == null) return false;

        var (name, slug) = await LookupRules.ValidateSluggedNameAsync(
            repo.Query(), request.Name, excludeId: request.Id, "mekan kategorisi", ct);

        category.Name = name;
        category.Slug = slug;
        category.Icon = LookupRules.Clean(request.Icon);
        category.DisplayOrder = request.DisplayOrder;

        repo.Update(category);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

// ---------- Ortak kurallar ----------

internal static class LookupRules
{
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Ad denetimi + benzersizlik (slug'sız tablolar: mezarlık/cami).</summary>
    public static async Task<string> ValidateUniqueNameAsync<T>(
        IQueryable<T> query, string? rawName, Guid? excludeId, string label, CancellationToken ct)
        where T : Domain.Common.BaseEntity
    {
        var name = rawName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            throw new AppException($"Geçerli bir {label} adı girin (en az 2 karakter).", "VALIDATION_ERROR");

        var lowered = name.ToLower();
        if (await query.AnyAsync(x => EF.Property<string>(x, "Name").ToLower() == lowered
                                      && (excludeId == null || x.Id != excludeId), ct))
            throw new ConflictException($"Bu adla bir {label} kaydı zaten var.");

        return name;
    }

    /// <summary>Ad denetimi + slug üretimi + slug benzersizliği (mahalle/etkinlik+mekan kategorisi).</summary>
    public static async Task<(string Name, string Slug)> ValidateSluggedNameAsync<T>(
        IQueryable<T> query, string? rawName, Guid? excludeId, string label, CancellationToken ct)
        where T : Domain.Common.BaseEntity
    {
        var name = await ValidateUniqueNameAsync(query, rawName, excludeId, label, ct);
        var slug = SlugHelper.Slugify(name);
        if (slug.Length == 0)
            throw new AppException($"{label} adı geçerli karakter içermiyor.", "VALIDATION_ERROR");

        if (await query.AnyAsync(x => EF.Property<string>(x, "Slug") == slug
                                      && (excludeId == null || x.Id != excludeId), ct))
            throw new ConflictException($"Bu slug ile bir {label} kaydı zaten var.");

        return (name, slug);
    }
}
