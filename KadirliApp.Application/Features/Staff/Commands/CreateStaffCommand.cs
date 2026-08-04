using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Security;
using KadirliApp.Application.Features.Staff.DTOs;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Staff.Commands;

public class CreateStaffCommand : IRequest<Guid>, IAuditableCommand
{
    public string AuditModule => "staff";
    public string AuditAction => "create";
    public string? AuditAffectedType => "User";
    public object? AuditDetails => new { role = Role };

    public string Phone { get; set; } = default!;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string Password { get; set; } = default!;
    /// <summary>"moderator" | "admin" (kontrat snake_case).</summary>
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public List<StaffPermissionDto> Permissions { get; set; } = new();
}

public class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public CreateStaffCommandHandler(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public async Task<Guid> Handle(CreateStaffCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new AppException("Telefon numarası zorunludur.", "VALIDATION_ERROR");
        // Faz 11.18: politika tek sahipten (elle "< 6" denetimi kaldırıldı).
        PanelPasswordPolicy.Enforce(request.Password, request.Username, request.Phone);

        var role = StaffRole.Parse(request.Role);
        var phone = request.Phone.Trim();

        var repo = _uow.Repository<User>();
        if (await repo.Query().AnyAsync(u => u.Phone == phone, ct))
            throw new ConflictException("Bu telefon numarasıyla kayıtlı kullanıcı zaten var.");
        if (!string.IsNullOrWhiteSpace(request.Username) &&
            await repo.Query().AnyAsync(u => u.Username == request.Username, ct))
            throw new ConflictException("Bu kullanıcı adı zaten kullanılıyor.");
        if (!string.IsNullOrWhiteSpace(request.Email) &&
            await repo.Query().AnyAsync(u => u.Email == request.Email, ct))
            throw new ConflictException("Bu e-posta adresi zaten kullanılıyor.");

        var user = new User
        {
            Phone = phone,
            Username = request.Username,
            Email = request.Email,
            Password = _hasher.HashPassword(request.Password),
            Role = role,
            IsActive = request.IsActive,
            // Faz 11.18: parolayı personelin kendisi değil YÖNETİCİ belirledi — yönetici
            // onu bir yere yazmış/iletmiş olabilir. Personel kendi parolasını seçene kadar
            // panelde hiçbir sayfa açılmaz.
            MustChangePassword = true,
            AdminPermissions = request.Permissions.Select(p => new AdminPermission
            {
                Module = p.Module,
                CanRead = p.CanRead,
                CanCreate = p.CanCreate,
                CanUpdate = p.CanUpdate,
                CanDelete = p.CanDelete,
                CanApprove = p.CanApprove
            }).ToList()
        };

        await repo.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return user.Id;
    }
}
