using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;

namespace KadirliApp.Application.Features.Users.Commands.ChangeMyPassword;

/// <summary>
/// Faz 10.9(f): panel kullanıcısının KENDİ şifresini değiştirmesi (mevcut şifre doğrulanır).
/// ResetStaffPasswordCommand'den farkı: başkasının değil kendi hesabı — yetki kontrolü UserId'nin claim'den gelmesiyle sağlanır.
/// Yalnız şifreli (panel) hesaplar içindir; OTP ile giren mobil kullanıcının şifresi yoktur.
/// </summary>
public sealed record ChangeMyPasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<bool>, IAuditableCommand
{
    // DİKKAT: AuditDetails bilinçli null — şifreler asla loglanmaz (ResetStaffPassword emsali).
    public string AuditModule => "staff";
    public string AuditAction => "change-password";
    public Guid? AuditAffectedId => UserId;
    public string? AuditAffectedType => "User";
}

public sealed class ChangeMyPasswordCommandHandler : IRequestHandler<ChangeMyPasswordCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public ChangeMyPasswordCommandHandler(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public async Task<bool> Handle(ChangeMyPasswordCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new AppException("Yeni şifre en az 6 karakter olmalıdır.", "VALIDATION_ERROR");
        if (request.NewPassword == request.CurrentPassword)
            throw new AppException("Yeni şifre mevcut şifreyle aynı olamaz.", "VALIDATION_ERROR");

        var repo = _uow.Repository<User>();
        var user = await repo.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.Password == null || user.Role == UserRole.User)
            throw new AppException("Bu hesabın panel şifresi bulunmuyor.", "VALIDATION_ERROR");

        if (!_hasher.VerifyPassword(request.CurrentPassword, user.Password))
            throw new AppException("Mevcut şifreniz hatalı.", "INVALID_PASSWORD");

        user.Password = _hasher.HashPassword(request.NewPassword);
        repo.Update(user);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
