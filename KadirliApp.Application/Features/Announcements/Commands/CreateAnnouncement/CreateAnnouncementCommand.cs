using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncement;

public class CreateAnnouncementCommand : IRequest<ApiResponse<Guid>>
{
    public CreateAnnouncementDto Dto { get; set; } = default!;
    public Guid? CreatedBy { get; set; }
}

public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, ApiResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAnnouncementNotificationGenerator _notificationGenerator;

    public CreateAnnouncementCommandHandler(IUnitOfWork unitOfWork, IAnnouncementNotificationGenerator notificationGenerator)
    {
        _unitOfWork = unitOfWork;
        _notificationGenerator = notificationGenerator;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        if (dto.TargetType == "neighborhood" && (dto.TargetNeighborhoodIds == null || dto.TargetNeighborhoodIds.Count == 0))
            return ApiResponse<Guid>.FailureResponse("VALIDATION", "Hedef kitle 'Belirli Mahalleler' seçildiğinde en az bir mahalle seçmelisiniz.");

        var now = DateTime.UtcNow;
        var scheduledFor = dto.ScheduledFor.HasValue
            ? DateTime.SpecifyKind(dto.ScheduledFor.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        // Yayın kuralı: planlanan zaman boşsa veya geçmişteyse duyuru HEMEN yayınlanır;
        // ilerideyse 'scheduled' durumunda bekler ve zamanı gelince job yayınlar.
        var isScheduled = scheduledFor.HasValue && scheduledFor.Value > now;

        var announcement = new Announcement
        {
            Title = dto.Title,
            Body = dto.Body,
            TypeId = dto.TypeId,
            Priority = dto.Priority,
            TargetType = string.IsNullOrWhiteSpace(dto.TargetType) ? "all" : dto.TargetType,
            TargetNeighborhoods = dto.TargetType == "neighborhood" && dto.TargetNeighborhoodIds is { Count: > 0 }
                ? JsonSerializer.Serialize(dto.TargetNeighborhoodIds)
                : null,
            ScheduledFor = scheduledFor,
            SentAt = isScheduled ? null : now,
            IsRecurring = dto.IsRecurring,
            RecurrencePattern = dto.RecurrencePattern,
            SendPushNotification = dto.SendPushNotification,
            Source = dto.Source,
            SourceUrl = dto.SourceUrl,
            VisibleUntil = dto.VisibleUntil.HasValue ? DateTime.SpecifyKind(dto.VisibleUntil.Value, DateTimeKind.Utc) : null,
            HasPdf = dto.HasPdf,
            HasLink = dto.HasLink || !string.IsNullOrWhiteSpace(dto.ExternalLink),
            ExternalLink = dto.ExternalLink,
            ImageFileId = dto.ImageFileId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LocationName = dto.LocationName,
            CreatedBy = request.CreatedBy,
            Status = isScheduled ? "scheduled" : "active"
        };

        await _unitOfWork.Repository<Announcement>().AddAsync(announcement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Faz 10.10: anında yayınlanan duyuru bildirimlerini üret (scheduled olanları job üretir).
        if (!isScheduled)
            await _notificationGenerator.GenerateForAnnouncementAsync(announcement, ct: cancellationToken);

        return ApiResponse<Guid>.SuccessResponse(announcement.Id);
    }
}
