using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public string AuditModule => "announcements";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Announcement";

    public Guid Id { get; set; }
}

public class DeleteAnnouncementCommandHandler : IRequestHandler<DeleteAnnouncementCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAnnouncementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _unitOfWork.Repository<Announcement>().GetByIdAsync(request.Id, cancellationToken);
        if (announcement == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Duyuru bulunamadı.");

        _unitOfWork.Repository<Announcement>().SoftRemove(announcement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
