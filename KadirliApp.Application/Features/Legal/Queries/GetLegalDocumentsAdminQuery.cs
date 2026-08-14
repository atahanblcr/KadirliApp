using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Legal.Queries;

/// <summary>Faz 12.16 — panelin belge listesi (pasif belgeler de görünür).</summary>
public record GetLegalDocumentsAdminQuery : IRequest<List<LegalDocumentAdminDto>>;

public class GetLegalDocumentsAdminQueryHandler
    : IRequestHandler<GetLegalDocumentsAdminQuery, List<LegalDocumentAdminDto>>
{
    private readonly IUnitOfWork _uow;

    public GetLegalDocumentsAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<LegalDocumentAdminDto>> Handle(GetLegalDocumentsAdminQuery request, CancellationToken ct)
    {
        var documents = await _uow.Repository<LegalDocument>().Query()
            .Include(d => d.Versions)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Title)
            .ToListAsync(ct);

        var liveIds = documents
            .Select(d => LegalConsentRules.LiveVersionOf(d)?.Id)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();

        // 🔑 "Kaç kişi onayladı" GERÇEK RIZA SORGUSUNDAN gelir, ayrı bir sayaç kolonundan
        // değil (§7 madde 59'un önizleme dersi): sayaç kolonu bir yazma yolunda unutulduğu
        // an panel yanlış bir rakamı **kendinden emin** gösterir ve kimse fark etmez.
        var grantedCounts = await _uow.Repository<UserConsent>().Query()
            .Where(c => liveIds.Contains(c.DocumentVersionId) && c.Granted)
            .GroupBy(c => c.DocumentVersionId)
            .Select(g => new { VersionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VersionId, x => x.Count, ct);

        return documents.Select(d =>
        {
            var live = LegalConsentRules.LiveVersionOf(d);
            return new LegalDocumentAdminDto
            {
                Id = d.Id,
                Type = d.Type,
                Title = d.Title,
                IsMandatory = d.IsMandatory,
                ShowAtRegistration = d.ShowAtRegistration,
                IsActive = d.IsActive,
                SortOrder = d.SortOrder,
                VersionCount = d.Versions.Count,
                LiveVersionNumber = live?.VersionNumber,
                LivePublishedAt = live?.PublishedAt,
                LiveGrantedCount = live is not null && grantedCounts.TryGetValue(live.Id, out var c) ? c : 0,
                HasDraft = d.Versions.Any(v => v.IsDraft)
            };
        }).ToList();
    }
}

/// <summary>Faz 12.16 — bir belgenin sürümleri (panelin sürüm ekranı).</summary>
public record GetLegalDocumentVersionsQuery(Guid DocumentId) : IRequest<List<LegalDocumentVersionAdminDto>>;

public class GetLegalDocumentVersionsQueryHandler
    : IRequestHandler<GetLegalDocumentVersionsQuery, List<LegalDocumentVersionAdminDto>>
{
    private readonly IUnitOfWork _uow;

    public GetLegalDocumentVersionsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<LegalDocumentVersionAdminDto>> Handle(
        GetLegalDocumentVersionsQuery request, CancellationToken ct)
    {
        var versions = await _uow.Repository<LegalDocumentVersion>().Query()
            .Where(v => v.DocumentId == request.DocumentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        var ids = versions.Select(v => v.Id).ToList();

        var counts = await _uow.Repository<UserConsent>().Query()
            .Where(c => ids.Contains(c.DocumentVersionId))
            .GroupBy(c => new { c.DocumentVersionId, c.Granted })
            .Select(g => new { g.Key.DocumentVersionId, g.Key.Granted, Count = g.Count() })
            .ToListAsync(ct);

        var publisherIds = versions.Where(v => v.PublishedBy is not null)
            .Select(v => v.PublishedBy!.Value).Distinct().ToList();

        var publishers = await _uow.Repository<User>().Query()
            .Where(u => publisherIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Username })
            .ToDictionaryAsync(x => x.Id, x => x.Username, ct);

        return versions.Select(v => new LegalDocumentVersionAdminDto
        {
            Id = v.Id,
            VersionNumber = v.VersionNumber,
            Summary = v.Summary,
            Body = v.Body,
            RequiresReconsent = v.RequiresReconsent,
            EffectiveFrom = v.EffectiveFrom,
            PublishedAt = v.PublishedAt,
            SupersededAt = v.SupersededAt,
            PublishedByName = v.PublishedBy is not null && publishers.TryGetValue(v.PublishedBy.Value, out var name)
                ? name
                : null,
            IsLive = v.IsLive,
            IsDraft = v.IsDraft,
            GrantedCount = counts.FirstOrDefault(c => c.DocumentVersionId == v.Id && c.Granted)?.Count ?? 0,
            DeniedCount = counts.FirstOrDefault(c => c.DocumentVersionId == v.Id && !c.Granted)?.Count ?? 0
        }).ToList();
    }
}
