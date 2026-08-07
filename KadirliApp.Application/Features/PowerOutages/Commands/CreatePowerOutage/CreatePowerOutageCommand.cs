using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Commands.CreatePowerOutage;

public class CreatePowerOutageCommand : IRequest<ApiResponse<Guid>>, IAuditableCommand
{
    public string AuditModule => "power-outages";
    public string AuditAction => "create";
    public Guid? AuditAffectedId => null;
    public string? AuditAffectedType => "PowerOutage";

    public CreatePowerOutageDto Dto { get; set; } = default!;

    /// <summary>Faz 12.3: duyuruyu kimin adına açtığımız (panelde butona basan yönetici).</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>Faz 12.3: kaç kişiye bildirim yazıldığı — panel mesajı bu sayıyı söyler.</summary>
    public PowerOutageNotifyOutcome NotifyOutcome { get; private set; } = PowerOutageNotifyOutcome.NotRequested;
    public int NotifiedCount { get; private set; }

    internal void RecordNotification(PowerOutageAnnouncementResult result)
    {
        NotifyOutcome = result.Outcome;
        NotifiedCount = result.RecipientCount;
    }
}

public class CreatePowerOutageCommandHandler : IRequestHandler<CreatePowerOutageCommand, ApiResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerOutageAnnouncementWriter _announcements;

    public CreatePowerOutageCommandHandler(
        IUnitOfWork unitOfWork, IPowerOutageAnnouncementWriter announcements)
    {
        _unitOfWork = unitOfWork;
        _announcements = announcements;
    }

    public async Task<ApiResponse<Guid>> Handle(CreatePowerOutageCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        if (dto.EndTime <= dto.StartTime)
            return ApiResponse<Guid>.FailureResponse("VALIDATION", "Kesinti bitiş zamanı başlangıçtan sonra olmalıdır.");

        var resolved = await PowerOutageNeighborhoodResolver.ResolveAsync(
            _unitOfWork, dto.NeighborhoodId, dto.Neighborhood, cancellationToken);

        if (resolved.NotFound)
            return ApiResponse<Guid>.FailureResponse("VALIDATION", "Seçilen mahalle bulunamadı.");

        var outage = new PowerOutage
        {
            NeighborhoodId = resolved.Id,
            Neighborhood = resolved.Name,
            AreaDetail = string.IsNullOrWhiteSpace(dto.AreaDetail) ? null : dto.AreaDetail.Trim(),
            StartTime = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc),
            Reason = dto.Reason
        };

        await _unitOfWork.Repository<PowerOutage>().AddAsync(outage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var notification = await _announcements.SyncAsync(
            outage, dto.SendNotification, dto.TargetNeighborhoodIds, request.CreatedBy, cancellationToken);

        request.RecordNotification(notification);

        return ApiResponse<Guid>.SuccessResponse(outage.Id);
    }
}
