using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Businesses.Commands;

/// <summary>
/// Faz 10.9(b): Business CRUD ilk kez yazıldı — öncesinde işletmeler yalnız MockDataSeeder'dan gelebiliyordu
/// ve kampanya modülü gerçek işletmelerle fiilen kullanılamıyordu.
/// </summary>
public sealed record CreateBusinessCommand(Dtos.CreateBusinessDto Dto) : IRequest<Guid>;

public sealed class CreateBusinessCommandHandler : IRequestHandler<CreateBusinessCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateBusinessCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var name = await BusinessRules.ValidateAsync(_uow, dto.BusinessName, dto.CategoryId, cancellationToken);

        var business = new Business
        {
            BusinessName = name,
            CategoryId = dto.CategoryId,
            TaxNumber = Clean(dto.TaxNumber),
            Address = Clean(dto.Address),
            Phone = Clean(dto.Phone),
            Email = Clean(dto.Email),
            WebsiteUrl = Clean(dto.WebsiteUrl),
            InstagramHandle = Clean(dto.InstagramHandle)?.TrimStart('@'),
            LogoFileId = dto.LogoFileId
        };

        await _uow.Repository<Business>().AddAsync(business, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return business.Id;
    }

    internal static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record UpdateBusinessCommand(Guid Id, Dtos.UpdateBusinessDto Dto) : IRequest<bool>;

public sealed class UpdateBusinessCommandHandler : IRequestHandler<UpdateBusinessCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateBusinessCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var repo = _uow.Repository<Business>();
        var business = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (business == null) return false;

        var name = await BusinessRules.ValidateAsync(_uow, dto.BusinessName, dto.CategoryId, cancellationToken);

        business.BusinessName = name;
        business.CategoryId = dto.CategoryId;
        business.TaxNumber = CreateBusinessCommandHandler.Clean(dto.TaxNumber);
        business.Address = CreateBusinessCommandHandler.Clean(dto.Address);
        business.Phone = CreateBusinessCommandHandler.Clean(dto.Phone);
        business.Email = CreateBusinessCommandHandler.Clean(dto.Email);
        business.WebsiteUrl = CreateBusinessCommandHandler.Clean(dto.WebsiteUrl);
        business.InstagramHandle = CreateBusinessCommandHandler.Clean(dto.InstagramHandle)?.TrimStart('@');
        business.LogoFileId = dto.LogoFileId;

        repo.Update(business);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// KARAR: Business soft-delete DEĞİL (entity'de DeletedAt yok) ve campaigns FK'sı DB'de CASCADE —
/// silmek kampanya geçmişini (soft-silinmişler dahil) fiziksel yok eder. Bu yüzden DB seviyesinde
/// (IgnoreQueryFilters) HERHANGİ bir kampanyası olan işletme silinemez → 409 (DeleteGuideCategory emsali).
/// </summary>
public sealed record DeleteBusinessCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "businesses";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Business";
}

public sealed class DeleteBusinessCommandHandler : IRequestHandler<DeleteBusinessCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteBusinessCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Business>();
        var business = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (business == null) return false;

        var hasCampaigns = await _uow.Repository<Campaign>().Query()
            .IgnoreQueryFilters()
            .AnyAsync(c => c.BusinessId == request.Id, cancellationToken);
        if (hasCampaigns)
            throw new ConflictException("Bu işletmenin kampanya kaydı var — silinemez. Önce kampanyaları başka işletmeye taşıyın ya da işletmeyi pasif kabul edin.");

        repo.Remove(business);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Doğrulama rozeti — VerifyTaxiDriver emsali; geri alma da destekli (Verified=false izleri temizler).</summary>
public sealed record SetBusinessVerificationCommand(Guid Id, bool Verified, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "businesses";
    public string AuditAction => Verified ? "verify" : "unverify";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Business";
}

public sealed class SetBusinessVerificationCommandHandler : IRequestHandler<SetBusinessVerificationCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public SetBusinessVerificationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(SetBusinessVerificationCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Business>();
        var business = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (business == null) return false;

        business.IsVerified = request.Verified;
        business.VerifiedBy = request.Verified ? request.AdminId : null;
        business.VerifiedAt = request.Verified ? DateTime.UtcNow : null;

        repo.Update(business);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Panel formundan hızlı kategori ekleme (AnnouncementType modal emsali) — aynı ada 409.</summary>
public sealed record CreateBusinessCategoryCommand(string Name, Guid? ParentId = null) : IRequest<Guid>;

public sealed class CreateBusinessCategoryCommandHandler : IRequestHandler<CreateBusinessCategoryCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateBusinessCategoryCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Guid> Handle(CreateBusinessCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new AppException("Kategori adı boş olamaz.", "VALIDATION_ERROR");

        var repo = _uow.Repository<BusinessCategory>();

        if (await repo.Query().AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken))
            throw new ConflictException("Bu isimde bir işletme kategorisi zaten var.");

        if (request.ParentId is { } parentId &&
            !await repo.Query().AnyAsync(x => x.Id == parentId, cancellationToken))
            throw new AppException("Üst kategori bulunamadı.", "VALIDATION_ERROR");

        var category = new BusinessCategory
        {
            Name = name,
            Slug = BusinessRules.Slugify(name),
            ParentId = request.ParentId
        };

        await repo.AddAsync(category, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}

internal static class BusinessRules
{
    /// <summary>Ad + kategori denetimi; temizlenmiş adı döner (Create/Update paylaşır — AdSubmissionRules deseni).</summary>
    public static async Task<string> ValidateAsync(IUnitOfWork uow, string? businessName, Guid categoryId, CancellationToken ct)
    {
        var name = businessName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            throw new AppException("İşletme adı en az 2 karakter olmalıdır.", "VALIDATION_ERROR");

        var categoryExists = await uow.Repository<BusinessCategory>().Query()
            .AnyAsync(c => c.Id == categoryId, ct);
        if (!categoryExists)
            throw new AppException("Geçersiz işletme kategorisi.", "VALIDATION_ERROR");

        return name;
    }

    /// <summary>Türkçe karakter destekli slug — Faz 10.9'da Common/Utils/SlugHelper'a ortaklaştı.</summary>
    public static string Slugify(string value) => Common.Utils.SlugHelper.Slugify(value);
}
