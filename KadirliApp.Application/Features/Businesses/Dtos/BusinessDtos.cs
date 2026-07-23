using System;

namespace KadirliApp.Application.Features.Businesses.Dtos;

/// <summary>Faz 10.9(b): Business CRUD ilk kez Application'da — kampanya modülünün ön koşulu.</summary>
public record BusinessResponseDto(
    Guid Id,
    string BusinessName,
    Guid CategoryId,
    string CategoryName,
    string? TaxNumber,
    string? Address,
    string? Phone,
    string? Email,
    string? WebsiteUrl,
    string? InstagramHandle,
    Guid? LogoFileId,
    string? LogoUrl,
    bool IsVerified,
    DateTime? VerifiedAt,
    int CampaignCount,
    DateTime CreatedAt
);

public record QueryBusinessDto(
    string? Search,
    Guid? CategoryId,
    bool? IsVerified,
    int Page = 1,
    int Limit = 20
);

public record CreateBusinessDto(
    string BusinessName,
    Guid CategoryId,
    string? TaxNumber,
    string? Address,
    string? Phone,
    string? Email,
    string? WebsiteUrl,
    string? InstagramHandle,
    Guid? LogoFileId
);

public record UpdateBusinessDto(
    string BusinessName,
    Guid CategoryId,
    string? TaxNumber,
    string? Address,
    string? Phone,
    string? Email,
    string? WebsiteUrl,
    string? InstagramHandle,
    Guid? LogoFileId
);

public record BusinessCategoryDto(Guid Id, string Name, string Slug, Guid? ParentId);
