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
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Commands;

/// <summary>
/// Faz 10.9(c): İlan kategori ağacı + kategori özellikleri (property/option) CRUD'u ilk kez yazıldı —
/// öncesinde bu tablolar YALNIZ DbSeeder'dan doluyordu. Tüm command'ler `ads-lookup` cache grubunu
/// invalidate eder (10.5 notundaki şart) ve audit izi bırakır.
/// KARARLAR: (1) Update'te ParentId değiştirilemez (döngü riski + ilanların kategori ağacı kayar);
/// (2) rename slug'ı yeniden üretir (slug unique — çakışma 409); (3) PropertyType sonradan değiştirilemez
/// (mevcut ilan değerleri anlamsızlaşır); (4) option güncelleme yok — sil+ekle yeterli
/// (AdPropertyValue option'a FK ile değil değer kopyasıyla bağlı, silme mevcut ilanları bozmaz).
/// </summary>
public abstract record AdsLookupInvalidatorBase : ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.AdsLookup };
}

public sealed record CreateAdCategoryCommand(string Name, Guid? ParentId, string? Icon, int DisplayOrder, bool IsActive = true)
    : AdsLookupInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "create-category";
    public string? AuditAffectedType => "AdCategory";
    public object? AuditDetails => new { name = Name, parentId = ParentId };
}

public sealed class CreateAdCategoryCommandHandler : IRequestHandler<CreateAdCategoryCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateAdCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateAdCategoryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<AdCategory>();
        var (name, slug) = await AdCategoryRules.ValidateNameAsync(_uow, request.Name, excludeId: null, ct);

        if (request.ParentId is { } parentId && !await repo.Query().AnyAsync(c => c.Id == parentId, ct))
            throw new AppException("Üst kategori bulunamadı.", "VALIDATION_ERROR");

        var category = new AdCategory
        {
            Name = name,
            Slug = slug,
            ParentId = request.ParentId,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive
        };

        await repo.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);
        return category.Id;
    }
}

/// <summary>ParentId bilinçli yok — ağaçta taşıma desteklenmiyor (KARAR, özet üstte).</summary>
public sealed record UpdateAdCategoryCommand(Guid Id, string Name, string? Icon, int DisplayOrder, bool IsActive)
    : AdsLookupInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "update-category";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "AdCategory";
    public object? AuditDetails => new { name = Name, isActive = IsActive };
}

public sealed class UpdateAdCategoryCommandHandler : IRequestHandler<UpdateAdCategoryCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public UpdateAdCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateAdCategoryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<AdCategory>();
        var category = await repo.GetByIdAsync(request.Id, ct);
        if (category == null) return false;

        var (name, slug) = await AdCategoryRules.ValidateNameAsync(_uow, request.Name, excludeId: request.Id, ct);

        category.Name = name;
        category.Slug = slug;
        category.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        repo.Update(category);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>
/// KARAR: alt kategorisi, (soft-silinmiş dahil — FK hâlâ işaret eder) ilanı veya özelliği olan
/// kategori silinemez → 409 (DeleteGuideCategory emsali; ads FK'sı DB'de RESTRICT, properties CASCADE
/// olsa da form tanımlarının sessizce yok olmaması için property varlığı da engel sayılır).
/// </summary>
public sealed record DeleteAdCategoryCommand(Guid Id)
    : AdsLookupInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "delete-category";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "AdCategory";
}

public sealed class DeleteAdCategoryCommandHandler : IRequestHandler<DeleteAdCategoryCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public DeleteAdCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeleteAdCategoryCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<AdCategory>();
        var category = await repo.GetByIdAsync(request.Id, ct);
        if (category == null) return false;

        if (await repo.Query().AnyAsync(c => c.ParentId == request.Id, ct))
            throw new ConflictException("Bu kategorinin alt kategorileri var — önce onları silin.");

        if (await _uow.Repository<Ad>().Query().IgnoreQueryFilters().AnyAsync(a => a.CategoryId == request.Id, ct))
            throw new ConflictException("Bu kategoride ilan kaydı var (silinmiş ilanlar dahil) — kategori silinemez, pasife alın.");

        if (await _uow.Repository<CategoryProperty>().Query().AnyAsync(p => p.CategoryId == request.Id, ct))
            throw new ConflictException("Bu kategorinin özellik tanımları var — önce özellikleri silin.");

        repo.Remove(category);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record CreateCategoryPropertyCommand(
    Guid CategoryId, string PropertyName, string PropertyType, bool IsRequired,
    string? DefaultValue, int DisplayOrder, List<string>? Options = null)
    : AdsLookupInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "create-property";
    public string? AuditAffectedType => "CategoryProperty";
    public object? AuditDetails => new { categoryId = CategoryId, name = PropertyName, type = PropertyType };
}

public sealed class CreateCategoryPropertyCommandHandler : IRequestHandler<CreateCategoryPropertyCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreateCategoryPropertyCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateCategoryPropertyCommand request, CancellationToken ct)
    {
        var name = request.PropertyName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new AppException("Özellik adı boş olamaz.", "VALIDATION_ERROR");

        if (!Enum.TryParse<PropertyType>(request.PropertyType, ignoreCase: true, out var type))
            throw new AppException(
                $"Geçersiz özellik tipi. Geçerli değerler: {string.Join(", ", Enum.GetNames<PropertyType>())}.", "VALIDATION_ERROR");

        if (!await _uow.Repository<AdCategory>().Query().AnyAsync(c => c.Id == request.CategoryId, ct))
            throw new NotFoundException(nameof(AdCategory), request.CategoryId);

        var repo = _uow.Repository<CategoryProperty>();
        if (await repo.Query().AnyAsync(p => p.CategoryId == request.CategoryId && p.PropertyName.ToLower() == name.ToLower(), ct))
            throw new ConflictException("Bu kategoride aynı adla bir özellik zaten var.");

        var options = (request.Options ?? new List<string>())
            .Select(o => o?.Trim()).Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (type is PropertyType.Select or PropertyType.MultiSelect && options.Count == 0)
            throw new AppException("Select/MultiSelect tipindeki özellik en az bir seçenekle oluşturulmalıdır.", "VALIDATION_ERROR");

        var property = new CategoryProperty
        {
            CategoryId = request.CategoryId,
            PropertyName = name,
            PropertyType = type,
            IsRequired = request.IsRequired,
            DefaultValue = string.IsNullOrWhiteSpace(request.DefaultValue) ? null : request.DefaultValue.Trim(),
            DisplayOrder = request.DisplayOrder,
            Options = options.Select((o, i) => new PropertyOption { OptionValue = o, DisplayOrder = i + 1 }).ToList()
        };

        await repo.AddAsync(property, ct);
        await _uow.SaveChangesAsync(ct);
        return property.Id;
    }
}

/// <summary>PropertyType bilinçli değiştirilemez (KARAR, özet üstte).</summary>
public sealed record UpdateCategoryPropertyCommand(Guid Id, string PropertyName, bool IsRequired, string? DefaultValue, int DisplayOrder)
    : AdsLookupInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "update-property";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "CategoryProperty";
    public object? AuditDetails => new { name = PropertyName, isRequired = IsRequired };
}

public sealed class UpdateCategoryPropertyCommandHandler : IRequestHandler<UpdateCategoryPropertyCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public UpdateCategoryPropertyCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateCategoryPropertyCommand request, CancellationToken ct)
    {
        var name = request.PropertyName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new AppException("Özellik adı boş olamaz.", "VALIDATION_ERROR");

        var repo = _uow.Repository<CategoryProperty>();
        var property = await repo.GetByIdAsync(request.Id, ct);
        if (property == null) return false;

        if (await repo.Query().AnyAsync(
                p => p.CategoryId == property.CategoryId && p.Id != request.Id && p.PropertyName.ToLower() == name.ToLower(), ct))
            throw new ConflictException("Bu kategoride aynı adla bir özellik zaten var.");

        property.PropertyName = name;
        property.IsRequired = request.IsRequired;
        property.DefaultValue = string.IsNullOrWhiteSpace(request.DefaultValue) ? null : request.DefaultValue.Trim();
        property.DisplayOrder = request.DisplayOrder;

        repo.Update(property);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

/// <summary>KARAR: ilan değeri (ad_property_values, FK RESTRICT) olan özellik silinemez → 409.</summary>
public sealed record DeleteCategoryPropertyCommand(Guid Id)
    : AdsLookupInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "delete-property";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "CategoryProperty";
}

public sealed class DeleteCategoryPropertyCommandHandler : IRequestHandler<DeleteCategoryPropertyCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public DeleteCategoryPropertyCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeleteCategoryPropertyCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<CategoryProperty>();
        var property = await repo.GetByIdAsync(request.Id, ct);
        if (property == null) return false;

        if (await _uow.Repository<AdPropertyValue>().Query().AnyAsync(v => v.PropertyId == request.Id, ct))
            throw new ConflictException("Bu özelliğe değer girmiş ilanlar var — özellik silinemez.");

        repo.Remove(property); // seçenekler DB'de CASCADE
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

public sealed record CreatePropertyOptionCommand(Guid PropertyId, string OptionValue, int DisplayOrder)
    : AdsLookupInvalidatorBase, IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "create-option";
    public string? AuditAffectedType => "PropertyOption";
    public object? AuditDetails => new { propertyId = PropertyId, value = OptionValue };
}

public sealed class CreatePropertyOptionCommandHandler : IRequestHandler<CreatePropertyOptionCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    public CreatePropertyOptionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreatePropertyOptionCommand request, CancellationToken ct)
    {
        var value = request.OptionValue?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new AppException("Seçenek değeri boş olamaz.", "VALIDATION_ERROR");

        var property = await _uow.Repository<CategoryProperty>().GetByIdAsync(request.PropertyId, ct)
            ?? throw new NotFoundException(nameof(CategoryProperty), request.PropertyId);

        if (property.PropertyType is not (PropertyType.Select or PropertyType.MultiSelect))
            throw new AppException("Yalnızca Select/MultiSelect tipindeki özelliklere seçenek eklenebilir.", "VALIDATION_ERROR");

        var repo = _uow.Repository<PropertyOption>();
        if (await repo.Query().AnyAsync(o => o.PropertyId == request.PropertyId && o.OptionValue.ToLower() == value.ToLower(), ct))
            throw new ConflictException("Bu özellikte aynı seçenek zaten var.");

        var option = new PropertyOption { PropertyId = request.PropertyId, OptionValue = value, DisplayOrder = request.DisplayOrder };
        await repo.AddAsync(option, ct);
        await _uow.SaveChangesAsync(ct);
        return option.Id;
    }
}

/// <summary>Serbestçe silinebilir — ilan değerleri seçeneğe FK ile değil değer kopyasıyla bağlı (KARAR, özet üstte).</summary>
public sealed record DeletePropertyOptionCommand(Guid Id)
    : AdsLookupInvalidatorBase, IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "delete-option";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "PropertyOption";
}

public sealed class DeletePropertyOptionCommandHandler : IRequestHandler<DeletePropertyOptionCommand, bool>
{
    private readonly IUnitOfWork _uow;
    public DeletePropertyOptionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeletePropertyOptionCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<PropertyOption>();
        var option = await repo.GetByIdAsync(request.Id, ct);
        if (option == null) return false;

        repo.Remove(option);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

internal static class AdCategoryRules
{
    /// <summary>Ad denetimi + slug üretimi + slug çakışma kontrolü (Create/Update paylaşır).</summary>
    public static async Task<(string Name, string Slug)> ValidateNameAsync(IUnitOfWork uow, string? rawName, Guid? excludeId, CancellationToken ct)
    {
        var name = rawName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            throw new AppException("Kategori adı en az 2 karakter olmalıdır.", "VALIDATION_ERROR");

        var slug = SlugHelper.Slugify(name);
        if (slug.Length == 0)
            throw new AppException("Kategori adı geçerli karakter içermiyor.", "VALIDATION_ERROR");

        var slugTaken = await uow.Repository<AdCategory>().Query()
            .AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId), ct);
        if (slugTaken)
            throw new ConflictException("Bu ad/slug ile bir ilan kategorisi zaten var.");

        return (name, slug);
    }
}
