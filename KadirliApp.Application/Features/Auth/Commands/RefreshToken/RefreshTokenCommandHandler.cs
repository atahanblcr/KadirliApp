using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokens>
{
    private readonly IJwtProvider _jwtProvider;
    private readonly ITokenBlacklistService _blacklist;
    private readonly IUnitOfWork _uow;

    public RefreshTokenCommandHandler(IJwtProvider jwtProvider, ITokenBlacklistService blacklist, IUnitOfWork uow)
    {
        _jwtProvider = jwtProvider;
        _blacklist = blacklist;
        _uow = uow;
    }

    public async Task<AuthTokens> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var payload = _jwtProvider.ValidateRefreshToken(request.RefreshToken)
            ?? throw new UnauthorizedException("Geçersiz veya süresi dolmuş refresh token. Lütfen tekrar giriş yapın.");

        if (await _blacklist.IsRevokedAsync(payload.Jti))
            throw new UnauthorizedException("Bu refresh token iptal edilmiş. Lütfen tekrar giriş yapın.");

        // Token'daki role/phone'a güvenilmez — kullanıcı DB'den taze okunur (rol değişmiş,
        // hesap banlanmış/pasifleşmiş olabilir).
        var user = await _uow.Repository<User>().Query()
                .FirstOrDefaultAsync(x => x.Id == payload.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Kullanıcı bulunamadı. Lütfen tekrar giriş yapın.");

        if (user.IsBanned)
            throw new ForbiddenException("Hesabınız engellenmiştir.");

        if (!user.IsActive)
            throw new ForbiddenException("Hesabınız pasif durumdadır.");

        // Rotasyon: eski refresh kalan ömrü kadar iptal listesine yazılır — tekrar kullanımı 401.
        var remaining = payload.ExpiresAtUtc - DateTime.UtcNow;
        if (remaining > TimeSpan.Zero)
            await _blacklist.RevokeAsync(payload.Jti, remaining);

        return _jwtProvider.GenerateTokens(user.Id, user.Role.ToRoleString(), user.Phone);
    }
}
