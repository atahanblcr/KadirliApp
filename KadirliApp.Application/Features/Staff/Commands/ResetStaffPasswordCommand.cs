using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Security;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Staff.Commands;

public class ResetStaffPasswordCommand : IRequest<bool>, IAuditableCommand
{
    // DİKKAT: AuditDetails bilinçli null — yeni şifre asla loglanmaz.
    public string AuditModule => "staff";
    public string AuditAction => "reset-password";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "User";

    public Guid Id { get; set; }
    public string NewPassword { get; set; } = default!;
}

public class ResetStaffPasswordCommandHandler : IRequestHandler<ResetStaffPasswordCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public ResetStaffPasswordCommandHandler(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public async Task<bool> Handle(ResetStaffPasswordCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<User>();
        var user = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role != UserRole.User, ct);
        if (user == null) return false;

        // Faz 11.18: politika tek sahipten (elle "< 6" denetimi kaldırıldı).
        PanelPasswordPolicy.Enforce(request.NewPassword, user.Username, user.Phone);

        user.Password = _hasher.HashPassword(request.NewPassword);
        // Faz 11.18: parolayı sahibi değil yönetici belirledi → ilk girişte değiştirmek zorunda.
        user.MustChangePassword = true;
        // Parola sıfırlandı → o hesabın açık panel oturumları düşer (OnValidatePrincipal).
        // Sıfırlamanın asıl amacı çoğu zaman tam olarak budur: hesabı ele geçirenin oturumunu kesmek.
        user.PasswordChangedAt = DateTime.UtcNow;
        // Sıfırlama aynı zamanda kilidi de açar — yönetici "hesabım kilitlendi" çağrısını böyle çözer.
        PanelLockoutPolicy.RegisterSuccess(user);
        repo.Update(user);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
