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

namespace KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementById;

public class GetAnnouncementByIdQuery : IRequest<ApiResponse<AnnouncementDto>>
{
    public Guid Id { get; set; }

    /// <summary>Public uç için: yalnız yayında (active) ve görünürlük süresi dolmamış duyuru döner (liste kuralıyla tutarlı).</summary>
    public bool OnlyPublished { get; set; }
}

public class GetAnnouncementByIdQueryHandler : IRequestHandler<GetAnnouncementByIdQuery, ApiResponse<AnnouncementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnnouncementByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AnnouncementDto>> Handle(GetAnnouncementByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Announcement>().Query()
            .Where(x => x.Id == request.Id);

        // Faz 10.7 düzeltmesi: id bilinirse pending/scheduled duyuru dönüyordu (liste zaten OnlyPublished filtreli).
        if (request.OnlyPublished)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.Status == "active" && (x.VisibleUntil == null || x.VisibleUntil > now));
        }

        var dto = await query
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
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
            return ApiResponse<AnnouncementDto>.FailureResponse("NOT_FOUND", "Duyuru bulunamadı.");

        // TargetNeighborhoods jsonb kolonu -> Id listesi
        var raw = await _unitOfWork.Repository<Announcement>().Query()
            .Where(x => x.Id == request.Id)
            .Select(x => x.TargetNeighborhoods)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try { dto.TargetNeighborhoodIds = JsonSerializer.Deserialize<List<Guid>>(raw); }
            catch { /* eski/bozuk veri varsa yoksay */ }
        }

        return ApiResponse<AnnouncementDto>.SuccessResponse(dto);
    }
}
