using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Staff.DTOs;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Staff.Commands;

/// <summary>Personelin modül izinlerini komple değiştirir (mevcut satırlar silinir, yenileri yazılır).</summary>
public class SetStaffPermissionsCommand : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "staff";
    public string AuditAction => "set-permissions";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "User";
    public object? AuditDetails => new { modules = Permissions.Select(p => p.Module).ToList() };

    public Guid Id { get; set; }
    public List<StaffPermissionDto> Permissions { get; set; } = new();
}

public class SetStaffPermissionsCommandHandler : IRequestHandler<SetStaffPermissionsCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public SetStaffPermissionsCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(SetStaffPermissionsCommand request, CancellationToken ct)
    {
        var duplicate = request.Permissions
            .GroupBy(p => p.Module, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            throw new AppException($"'{duplicate.Key}' modülü birden fazla kez gönderildi.", "VALIDATION_ERROR");

        var user = await _uow.Repository<User>().Query(tracking: true)
            .Include(u => u.AdminPermissions)
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role != UserRole.User, ct);
        if (user == null) return false;

        var permRepo = _uow.Repository<AdminPermission>();
        foreach (var existing in user.AdminPermissions.ToList())
            permRepo.Remove(existing);

        foreach (var p in request.Permissions)
        {
            await permRepo.AddAsync(new AdminPermission
            {
                UserId = user.Id,
                Module = p.Module,
                CanRead = p.CanRead,
                CanCreate = p.CanCreate,
                CanUpdate = p.CanUpdate,
                CanDelete = p.CanDelete,
                CanApprove = p.CanApprove
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
