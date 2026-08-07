using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Commands.UpdatePowerOutage;

public class UpdatePowerOutageCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public string AuditModule => "power-outages";
    public string AuditAction => "update";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "PowerOutage";

    public Guid Id { get; set; }
    public UpdatePowerOutageDto Dto { get; set; } = default!;
    public Guid? UpdatedBy { get; set; }

    public PowerOutageNotifyOutcome NotifyOutcome { get; private set; } = PowerOutageNotifyOutcome.NotRequested;
    public int NotifiedCount { get; private set; }

    internal void RecordNotification(PowerOutageAnnouncementResult result)
    {
        NotifyOutcome = result.Outcome;
        NotifiedCount = result.RecipientCount;
    }
}

public class UpdatePowerOutageCommandHandler : IRequestHandler<UpdatePowerOutageCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerOutageAnnouncementWriter _announcements;

    public UpdatePowerOutageCommandHandler(
        IUnitOfWork unitOfWork, IPowerOutageAnnouncementWriter announcements)
    {
        _unitOfWork = unitOfWork;
        _announcements = announcements;
    }

    public async Task<ApiResponse<bool>> Handle(UpdatePowerOutageCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var outage = await _unitOfWork.Repository<PowerOutage>().GetByIdAsync(request.Id, cancellationToken);
        if (outage == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Elektrik kesintisi bulunamadı.");

        if (dto.EndTime <= dto.StartTime)
            return ApiResponse<bool>.FailureResponse("VALIDATION", "Kesinti bitiş zamanı başlangıçtan sonra olmalıdır.");

        var resolved = await PowerOutageNeighborhoodResolver.ResolveAsync(
            _unitOfWork, dto.NeighborhoodId, dto.Neighborhood, cancellationToken);

        if (resolved.NotFound)
            return ApiResponse<bool>.FailureResponse("VALIDATION", "Seçilen mahalle bulunamadı.");

        outage.NeighborhoodId = resolved.Id;
        outage.Neighborhood = resolved.Name;
        outage.AreaDetail = string.IsNullOrWhiteSpace(dto.AreaDetail) ? null : dto.AreaDetail.Trim();
        outage.StartTime = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc);
        outage.EndTime = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc);
        outage.Reason = dto.Reason;

        _unitOfWork.Repository<PowerOutage>().Update(outage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 🔴 İKİNCİ DUYURU ÜRETİLMEZ. Var olan duyuru tazelenir (saat değiştiyse VisibleUntil
        // de) — yazıcı bunu tek yerde yapıyor. Her güncellemede yeniden üretilseydi bir
        // yazım düzeltmesi bile şehre ikinci bir push atardı.
        var notification = await _announcements.SyncAsync(
            outage, dto.SendNotification, dto.TargetNeighborhoodIds, request.UpdatedBy, cancellationToken);

        request.RecordNotification(notification);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
