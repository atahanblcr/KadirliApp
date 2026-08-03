using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Notifications.Services;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

        // 🔴 Faz 11.15c: duyuruyla birlikte ONUN ÜRETTİĞİ BİLDİRİMLER de gider.
        //
        // Önceki hâlde kalıyorlardı: canlıda push'lu bir duyuru silindikten sonra 9
        // notifications satırı ayakta kaldı ve mobilde "dokun → boş sayfa" üretti.
        // Bildirim türetilmiş veridir (kullanıcının kendi içeriği değil), kaynağı yok
        // olduğunda saklanmasının bir anlamı yok — bu yüzden soft değil GERÇEK silme.
        //
        // (GetMyNotificationsQuery'de ayrıca "hedefi yaşayan" süzgeci var; o, silme
        //  DIŞINDAKİ görünmezleşme yollarını — draft'a çekme, VisibleUntil'in geçmesi —
        //  kapatan ikinci katman. İkisi birbirinin yerine geçmez.)
        var orphaned = await _unitOfWork.Repository<Notification>().Query()
            .Where(n => n.RelatedType == AnnouncementNotificationGenerator.RelatedTypeAnnouncement
                        && n.RelatedId == announcement.Id)
            .ToListAsync(cancellationToken);

        foreach (var notification in orphaned)
            _unitOfWork.Repository<Notification>().Remove(notification);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
