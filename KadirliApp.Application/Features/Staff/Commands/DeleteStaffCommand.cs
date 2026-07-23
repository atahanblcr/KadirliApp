using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Staff.Commands;

/// <param name="RequestedBy">İşlemi yapan admin (kendi hesabını silmesin diye).</param>
public record DeleteStaffCommand(Guid Id, Guid RequestedBy) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "staff";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "User";
}

public class DeleteStaffCommandHandler : IRequestHandler<DeleteStaffCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteStaffCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteStaffCommand request, CancellationToken ct)
    {
        if (request.Id == request.RequestedBy)
            throw new AppException("Kendi hesabınızı silemezsiniz.", "SELF_DELETE_FORBIDDEN");

        var repo = _uow.Repository<User>();
        var user = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role != UserRole.User, ct);
        if (user == null) return false;

        if (user.Role == UserRole.SuperAdmin)
            throw new ForbiddenException("super_admin hesabı silinemez.");

        repo.SoftRemove(user);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
