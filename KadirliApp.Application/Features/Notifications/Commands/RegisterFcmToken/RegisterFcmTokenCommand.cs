using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Notifications.Commands.RegisterFcmToken;

/// <summary>
/// Faz 10.3: POST /v1/notifications/fcm-token — cihazın FCM token'ını kullanıcıya yazar (10.7 push'un ön koşulu).
/// Aynı token başka kullanıcılarda kayıtlıysa onlardan temizlenir: cihaz logout'suz hesap değiştirdiyse
/// eski hesaba yanlış push gitmesin (LogoutCommand'deki FcmToken temizliğinin tamamlayıcısı).
/// </summary>
public sealed class RegisterFcmTokenCommand : IRequest<Unit>
{
    public Guid UserId { get; set; }
    public string? Token { get; set; }
}

public sealed class RegisterFcmTokenCommandHandler : IRequestHandler<RegisterFcmTokenCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public RegisterFcmTokenCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(RegisterFcmTokenCommand request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token) || token.Length > 4096)
            throw new AppException("Geçersiz FCM token.", "VALIDATION_ERROR");

        var users = _uow.Repository<User>();
        var user = await users.Query()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var others = await users.Query()
            .Where(x => x.Id != user.Id && x.FcmToken == token)
            .ToListAsync(cancellationToken);
        foreach (var other in others)
        {
            other.FcmToken = null;
            users.Update(other);
        }

        user.FcmToken = token;
        users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
