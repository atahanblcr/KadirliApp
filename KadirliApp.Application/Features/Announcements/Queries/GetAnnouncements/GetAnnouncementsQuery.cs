using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Queries.GetAnnouncements;

// Faz 10.8: sayfalama + tür filtresi eklendi; dönüş ApiResponse<List> yerine PagedResult
// (tercih edilen desen — handler sade döner, zarfı ApiResponseWrapperFilter sarar; 10.3 kararı).
public class GetAnnouncementsQuery : IRequest<PagedResult<AnnouncementDto>>
{
    /// <summary>Mobil için: yalnızca yayında olan (active) ve görünürlük süresi dolmamış duyurular.</summary>
    public bool OnlyPublished { get; set; }

    /// <summary>Duyuru türüne göre filtre (announcement_types.id).</summary>
    public Guid? TypeId { get; set; }

    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Faz 11.18 — panel sütun sıralaması. **Additive**: boş geldiğinde 11.18 öncesindeki
    /// sıra (oluşturma tarihi azalan) birebir korunur; bilinmeyen anahtar varsayılana düşer.
    /// </summary>
    public string? Sort { get; set; }
}

public class GetAnnouncementsQueryHandler : IRequestHandler<GetAnnouncementsQuery, PagedResult<AnnouncementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnnouncementsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AnnouncementDto>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Announcement>().Query();

        if (request.OnlyPublished)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.Status == "active" && (x.VisibleUntil == null || x.VisibleUntil > now));
        }

        if (request.TypeId.HasValue)
            query = query.Where(x => x.TypeId == request.TypeId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(request.Page, request.Limit,
            request.OnlyPublished ? Pagination.MaxLimit : Pagination.AdminMaxLimit);

        // Faz 11.18: panel sütun sıralaması (boş Sort → eski davranışın birebir aynısı).
        var announcements = await Common.Sorting.PanelSorts.Announcements.Apply(query, request.Sort)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new AnnouncementDto
            {
                Id = x.Id,
                Title = x.Title,
                Body = x.Body,
                TypeId = x.TypeId,
                TypeName = x.Type.Name,
                Priority = x.Priority,
                Status = x.Status,
                TargetType = x.TargetType,
                ScheduledFor = x.ScheduledFor,
                SentAt = x.SentAt,
                VisibleUntil = x.VisibleUntil,
                SendPushNotification = x.SendPushNotification,
                Source = x.Source,
                SourceUrl = x.SourceUrl,
                HasLink = x.HasLink,
                ExternalLink = x.ExternalLink,
                ImageFileId = x.ImageFileId,
                ImageUrl = x.ImageFile != null ? x.ImageFile.CdnUrl : null,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                LocationName = x.LocationName,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AnnouncementDto>
        {
            Items = announcements,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
