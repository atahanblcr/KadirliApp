using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Complaints.Commands;

/// <summary>
/// Şikayet durum geçişi: "in_progress" (işleme al), "resolved" (çözüldü), "rejected" (reddet).
/// resolved/rejected geçişlerinde ResolvedBy/ResolvedAt doldurulur.
/// </summary>
public record ResolveComplaintCommand(Guid Id, Guid AdminId, string Status, string? AdminNotes = null) : IRequest<bool>, IAuditableCommand
{
    // Not: AdminNotes bilinçli details dışında — serbest metin gürültüsü yerine yalnız durum geçişi izlenir.
    public string AuditModule => "complaints";
    public string AuditAction => "resolve";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Complaint";
    public object? AuditDetails => new { status = Status };
}

public class ResolveComplaintCommandHandler : IRequestHandler<ResolveComplaintCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public ResolveComplaintCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(ResolveComplaintCommand request, CancellationToken cancellationToken)
    {
        // 10.9 denetimi: whitelist artık handler'da — panel kendi kontrolünü yapıyordu ama Admin API
        // body'deki status'u ham geçiriyordu (ör. "banana" DB'ye ve audit details'a yazılabilirdi).
        if (request.Status is not ("in_progress" or "resolved" or "rejected"))
            throw new AppException(
                "Geçersiz şikayet durumu. 'in_progress', 'resolved' veya 'rejected' olmalı.", "VALIDATION_ERROR");

        var repo = _uow.Repository<Complaint>();
        var complaint = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (complaint == null) return false;

        complaint.Status = request.Status;

        if (!string.IsNullOrWhiteSpace(request.AdminNotes))
            complaint.AdminNotes = request.AdminNotes;

        if (request.Status is "resolved" or "rejected")
        {
            complaint.ResolvedBy = request.AdminId;
            complaint.ResolvedAt = DateTime.UtcNow;
        }

        repo.Update(complaint);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
