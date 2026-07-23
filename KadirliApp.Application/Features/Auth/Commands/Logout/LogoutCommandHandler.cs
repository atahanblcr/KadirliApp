using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IJwtProvider _jwtProvider;
    private readonly ITokenBlacklistService _blacklist;
    private readonly IUnitOfWork _uow;

    public LogoutCommandHandler(IJwtProvider jwtProvider, ITokenBlacklistService blacklist, IUnitOfWork uow)
    {
        _jwtProvider = jwtProvider;
        _blacklist = blacklist;
        _uow = uow;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Refresh token best-effort iptal edilir: geçersiz/süresi dolmuşsa zaten kullanılamaz,
        // logout yine başarılı sayılır. Yalnız kullanıcının KENDİ token'ı iptal edilebilir.
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var payload = _jwtProvider.ValidateRefreshToken(request.RefreshToken);
            if (payload != null && payload.UserId == request.UserId)
            {
                var remaining = payload.ExpiresAtUtc - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                    await _blacklist.RevokeAsync(payload.Jti, remaining);
            }
        }

        // FcmToken temizlenir — cihaz başka hesapla girerse eski hesabın push'ları oraya gitmesin.
        var user = await _uow.Repository<User>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user?.FcmToken != null)
        {
            user.FcmToken = null;
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
