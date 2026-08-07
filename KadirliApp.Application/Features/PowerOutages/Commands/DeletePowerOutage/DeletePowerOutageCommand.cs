using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;

public class DeletePowerOutageCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public string AuditModule => "power-outages";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "PowerOutage";

    public Guid Id { get; set; }
}

public class DeletePowerOutageCommandHandler : IRequestHandler<DeletePowerOutageCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPowerOutageAnnouncementWriter _announcements;

    public DeletePowerOutageCommandHandler(
        IUnitOfWork unitOfWork, IPowerOutageAnnouncementWriter announcements)
    {
        _unitOfWork = unitOfWork;
        _announcements = announcements;
    }

    public async Task<ApiResponse<bool>> Handle(DeletePowerOutageCommand request, CancellationToken cancellationToken)
    {
        var outage = await _unitOfWork.Repository<PowerOutage>().GetByIdAsync(request.Id, cancellationToken);
        if (outage == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Elektrik kesintisi bulunamadı.");

        // 🔴 Faz 12.3: kesinti gidince DUYURUSU ve ONUN BİLDİRİMLERİ de gider.
        // Görünmez sözleşme #24'ün uzantısı — kalsalardı vatandaş bildirime dokunup boş
        // sayfaya düşerdi (11.15c'de duyurularda birebir bu yaşandı: 9 ölü bildirim).
        await _announcements.RemoveAsync(outage, cancellationToken);

        _unitOfWork.Repository<PowerOutage>().Remove(outage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
