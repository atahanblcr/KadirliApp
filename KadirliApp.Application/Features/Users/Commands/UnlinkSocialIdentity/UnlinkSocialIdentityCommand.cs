using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Auth;
using KadirliApp.Domain.Enums;
using MediatR;

namespace KadirliApp.Application.Features.Users.Commands.UnlinkSocialIdentity;

/// <summary>
/// Faz 12.7 — <c>DELETE /v1/users/me/identities/{provider}</c>: kullanıcı bağlantıyı çözer.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>SON bağlantı da çözülebilir ve bu, telefon-çıpa kararının somut kazancıdır.</b>
/// Telefonsuz bir kimlik modelinde bu uç bir tuzak olurdu: kullanıcı tek giriş yöntemini
/// kaldırır ve hesabına <b>bir daha hiç</b> giremezdi — bu yüzden çoğu uygulama "son
/// bağlantı çözülemez" kuralı yazar ve o kural da kendi başına bir destek yüküdür. Burada
/// telefon + OTP her zaman ayakta olduğu için böyle bir kurala <b>ihtiyaç yok</b>.
/// </para>
/// <para>
/// ⚠️ Bağlantı <b>fiziksel</b> silinir (soft-delete yok): <c>ProviderUserId</c> + e-posta
/// kişisel veridir ve "kaldırdım" diyen bir düğmenin arkasında duran satır, kullanıcıya
/// verilen sözün ihlalidir. Ayrıca benzersiz indeks yüzünden soft-delete edilmiş bir satır
/// aynı hesabın <b>yeniden bağlanmasını</b> engellerdi.
/// </para>
/// </remarks>
public sealed record UnlinkSocialIdentityCommand(Guid UserId, string Provider) : IRequest<bool>;

public sealed class UnlinkSocialIdentityCommandHandler
    : IRequestHandler<UnlinkSocialIdentityCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UnlinkSocialIdentityCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(UnlinkSocialIdentityCommand request, CancellationToken cancellationToken)
    {
        var provider = SocialProviders.Normalize(request.Provider)
            ?? throw new AppException("Desteklenmeyen giriş yöntemi.", "VALIDATION_ERROR");

        // Zaten bağlı değilse `false` döner. 🔑 404 yerine "başarılı" demek istemciyi
        // yalanlamaz: kullanıcının istediği son durum ("bağlı değil") sağlanmış durumda ve
        // iki kez basılan bir düğmenin ikinci basışı hata göstermemeli.
        var removed = await SocialIdentityLinker.UnlinkAsync(
            _uow, request.UserId, provider, cancellationToken);

        if (removed)
            await _uow.SaveChangesAsync(cancellationToken);

        return removed;
    }
}
