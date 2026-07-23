using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Commands.UpdateAnnouncement;

public class UpdateAnnouncementCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public UpdateAnnouncementDto Dto { get; set; } = default!;
}

public class UpdateAnnouncementCommandHandler : IRequestHandler<UpdateAnnouncementCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAnnouncementNotificationGenerator _notificationGenerator;

    public UpdateAnnouncementCommandHandler(IUnitOfWork unitOfWork, IAnnouncementNotificationGenerator notificationGenerator)
    {
        _unitOfWork = unitOfWork;
        _notificationGenerator = notificationGenerator;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _unitOfWork.Repository<Announcement>().GetByIdAsync(request.Id, cancellationToken);
        if (announcement == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Duyuru bulunamadı.");

        var dto = request.Dto;

        if (dto.TargetType == "neighborhood" && (dto.TargetNeighborhoodIds == null || dto.TargetNeighborhoodIds.Count == 0))
            return ApiResponse<bool>.FailureResponse("VALIDATION", "Hedef kitle 'Belirli Mahalleler' seçildiğinde en az bir mahalle seçmelisiniz.");

        var now = DateTime.UtcNow;
        var scheduledFor = dto.ScheduledFor.HasValue
            ? DateTime.SpecifyKind(dto.ScheduledFor.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        announcement.Title = dto.Title;
        announcement.Body = dto.Body;
        announcement.TypeId = dto.TypeId;
        announcement.Priority = dto.Priority;
        announcement.TargetType = string.IsNullOrWhiteSpace(dto.TargetType) ? "all" : dto.TargetType;
        announcement.TargetNeighborhoods = dto.TargetType == "neighborhood" && dto.TargetNeighborhoodIds is { Count: > 0 }
            ? JsonSerializer.Serialize(dto.TargetNeighborhoodIds)
            : null;
        announcement.ScheduledFor = scheduledFor;
        announcement.IsRecurring = dto.IsRecurring;
        announcement.RecurrencePattern = dto.RecurrencePattern;
        announcement.SendPushNotification = dto.SendPushNotification;
        announcement.Source = dto.Source;
        announcement.SourceUrl = dto.SourceUrl;
        announcement.VisibleUntil = dto.VisibleUntil.HasValue ? DateTime.SpecifyKind(dto.VisibleUntil.Value, DateTimeKind.Utc) : null;
        announcement.HasPdf = dto.HasPdf;
        announcement.HasLink = dto.HasLink || !string.IsNullOrWhiteSpace(dto.ExternalLink);
        announcement.ExternalLink = dto.ExternalLink;
        announcement.Latitude = dto.Latitude;
        announcement.Longitude = dto.Longitude;
        announcement.LocationName = dto.LocationName;

        if (dto.RemoveImage)
            announcement.ImageFileId = null;
        else if (dto.ImageFileId.HasValue)
            announcement.ImageFileId = dto.ImageFileId;

        // Henüz yayınlanmamış bir duyuruda zamanlama değiştirilirse durumu yeniden hesapla:
        // ileri tarih -> scheduled, boş/geçmiş -> hemen yayınla.
        var publishedNow = false;
        if (announcement.SentAt == null || announcement.Status == "scheduled")
        {
            var isScheduled = scheduledFor.HasValue && scheduledFor.Value > now;
            announcement.Status = isScheduled ? "scheduled" : "active";
            announcement.SentAt = isScheduled ? null : now;
            publishedNow = !isScheduled;
        }

        _unitOfWork.Repository<Announcement>().Update(announcement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Faz 10.10: bu güncellemeyle yayına giren duyurunun bildirimlerini üret
        // (generator idempotent — daha önce üretildiyse mükerrer satır yazmaz).
        if (publishedNow)
            await _notificationGenerator.GenerateForAnnouncementAsync(announcement, cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
