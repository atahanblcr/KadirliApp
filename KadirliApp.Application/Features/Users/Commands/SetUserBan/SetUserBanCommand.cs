using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Users.Commands.SetUserBan;

public record SetUserBanCommand(Guid UserId, bool Banned, Guid AdminId, string? Reason = null) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "users";
    public string AuditAction => Banned ? "ban" : "unban";
    public Guid? AuditAffectedId => UserId;
    public string? AuditAffectedType => "User";
    public object? AuditDetails => Banned && Reason is not null ? new { reason = Reason } : null;
}

public class SetUserBanCommandHandler : IRequestHandler<SetUserBanCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public SetUserBanCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(SetUserBanCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<User>();
        var user = await repo.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.IsBanned = request.Banned;
        user.BanReason = request.Banned ? request.Reason : null;
        user.BannedAt = request.Banned ? DateTime.UtcNow : null;
        user.BannedBy = request.Banned ? request.AdminId : null;

        repo.Update(user);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
