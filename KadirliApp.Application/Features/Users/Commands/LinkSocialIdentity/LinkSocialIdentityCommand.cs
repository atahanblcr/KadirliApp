using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Auth;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Users.Commands.LinkSocialIdentity;

/// <summary>
/// Faz 12.7 — <c>POST /v1/users/me/identities</c>: oturum açmış kullanıcı kendi hesabına
/// bir sosyal hesap bağlar.
/// </summary>
/// <remarks>
/// 🔑 <b>Bağlamanın TEK meşru yolu budur</b> — çünkü burada kimliğin sahibi iki şeyi birden
/// kanıtlamış olur: KadirliApp hesabına erişimi (JWT) <b>ve</b> sosyal hesaba erişimi
/// (imzalı <c>id_token</c>). <c>SocialLoginCommandHandler</c>'ın reddettiği "e-posta
/// eşleşiyorsa bağla" kısayolu (§7 madde 69) yalnız <b>ikincisini</b> kanıtlardı.
/// </remarks>
public sealed record LinkSocialIdentityCommand(Guid UserId, string Provider, string IdToken)
    : IRequest<LinkedIdentityDto>;

public sealed class LinkSocialIdentityCommandHandler
    : IRequestHandler<LinkSocialIdentityCommand, LinkedIdentityDto>
{
    private readonly ISocialTokenVerifier _verifier;
    private readonly IUnitOfWork _uow;

    public LinkSocialIdentityCommandHandler(ISocialTokenVerifier verifier, IUnitOfWork uow)
    {
        _verifier = verifier;
        _uow = uow;
    }

    public async Task<LinkedIdentityDto> Handle(
        LinkSocialIdentityCommand request, CancellationToken cancellationToken)
    {
        var provider = SocialProviders.Normalize(request.Provider)
            ?? throw new AppException("Desteklenmeyen giriş yöntemi.", "VALIDATION_ERROR");

        if (!_verifier.IsEnabled(provider))
            throw new AppException("Bu giriş yöntemi şu anda kullanılamıyor.", "SOCIAL_PROVIDER_DISABLED");

        var identity = await _verifier.VerifyAsync(provider, request.IdToken, cancellationToken)
            ?? throw new UnauthorizedException("Sosyal giriş doğrulanamadı. Lütfen tekrar deneyin.");

        // Hesabın hâlâ yaşadığını doğrula: silinmiş/banlı bir hesaba bağlantı eklemek,
        // o hesaba ileride bir giriş yolu açmak olurdu.
        var user = await _uow.Repository<User>().Query()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.IsBanned || !user.IsActive)
            throw new ForbiddenException("Hesabınız bu işlemi yapamaz.");

        var link = await SocialIdentityLinker.LinkAsync(_uow, user.Id, identity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return LinkedIdentityDto.FromEntity(link);
    }
}
