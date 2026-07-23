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

public class UpdateStaffCommand : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "staff";
    public string AuditAction => "update";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "User";
    public object? AuditDetails => new { role = Role, isActive = IsActive };

    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    /// <summary>"moderator" | "admin" (kontrat snake_case).</summary>
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class UpdateStaffCommandHandler : IRequestHandler<UpdateStaffCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateStaffCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateStaffCommand request, CancellationToken ct)
    {
        var role = StaffRole.Parse(request.Role);

        var repo = _uow.Repository<User>();
        var user = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role != UserRole.User, ct);
        if (user == null) return false;

        if (user.Role == UserRole.SuperAdmin)
            throw new ForbiddenException("super_admin hesabı bu endpoint'ten değiştirilemez.");

        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username &&
            await repo.Query().AnyAsync(u => u.Username == request.Username && u.Id != user.Id, ct))
            throw new ConflictException("Bu kullanıcı adı zaten kullanılıyor.");
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email &&
            await repo.Query().AnyAsync(u => u.Email == request.Email && u.Id != user.Id, ct))
            throw new ConflictException("Bu e-posta adresi zaten kullanılıyor.");

        user.Username = request.Username;
        user.Email = request.Email;
        user.Role = role;
        user.IsActive = request.IsActive;

        repo.Update(user);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
