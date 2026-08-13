using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Auth.Commands.SocialLogin;

/// <summary>
/// Faz 12.7 — sosyal giriş akışının tek sahibi.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>E-POSTA EŞLEŞMESİYLE OTOMATİK BAĞLAMA YAPILMAZ (§7 madde 69).</b> Bu, akışın en
/// çekici ve en tehlikeli kısayolu: "Google'dan gelen e-posta bir kullanıcının
/// <c>User.Email</c>'iyle aynıysa o hesaba bağla". Yapılmıyor çünkü <c>User.Email</c>
/// <b>panelden elle girilebiliyor ve hiçbir zaman doğrulanmıyor</b> — yani saldırgan
/// kurbanın e-postasıyla bir Google hesabı açıp (ya da yönetici bir yazım hatası yapıp)
/// <b>doğrudan o hesaba girerdi</b>. Bağlama <b>yalnız</b> hesabın sahibinin oturumundan
/// yapılır (<c>POST /v1/users/me/identities</c>).
/// </para>
/// <para>
/// ⚠️ Eşleştirmenin tek ölçütü <c>(provider, sub)</c>'dır. <c>sub</c> sağlayıcıda sabittir;
/// e-posta değişebilir.
/// </para>
/// </remarks>
public sealed class SocialLoginCommandHandler : IRequestHandler<SocialLoginCommand, SocialLoginResult>
{
    private readonly ISocialTokenVerifier _verifier;
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _uow;
    private readonly ILoginAttemptRecorder _loginAttempts;

    public SocialLoginCommandHandler(
        ISocialTokenVerifier verifier,
        IJwtProvider jwtProvider,
        IUnitOfWork uow,
        ILoginAttemptRecorder loginAttempts)
    {
        _verifier = verifier;
        _jwtProvider = jwtProvider;
        _uow = uow;
        _loginAttempts = loginAttempts;
    }

    public async Task<SocialLoginResult> Handle(SocialLoginCommand request, CancellationToken cancellationToken)
    {
        var provider = SocialProviders.Normalize(request.Provider)
            ?? throw new AppException("Desteklenmeyen giriş yöntemi.", "VALIDATION_ERROR");

        // Sağlayıcı yapılandırılmamışsa: açık ve anlamlı hata. Sessizce "geçersiz jeton"
        // demek, yapılandırma hatasını bir güvenlik hatası gibi gösterip saatler kaybettirirdi.
        if (!_verifier.IsEnabled(provider))
            throw new AppException("Bu giriş yöntemi şu anda kullanılamıyor.", "SOCIAL_PROVIDER_DISABLED");

        var identity = await _verifier.VerifyAsync(provider, request.IdToken, cancellationToken);
        if (identity is null)
        {
            // 🔑 Kimlik olarak SAĞLAYICI ADI yazılır — jeton doğrulanamadığı için elimizde
            // `sub` yok. Böylece bütün geçersiz denemeler tek bir kimlik altında toplanır ve
            // R1 eşiği aşıldığında panelde işaretlenir: "sosyal girişte bir şeyler ters"
            // sinyali, hiçbir sinyal olmamasından iyi. ⚠️ Rozetin metni ("aynı hesaba yoğun
            // deneme") bu satırlarda tam doğru değildir; ayrım FailureReason'da duruyor.
            await RecordAsync(provider, null, succeeded: false,
                LoginFailureReasons.BadSocialToken, cancellationToken);

            throw new UnauthorizedException("Sosyal giriş doğrulanamadı. Lütfen tekrar deneyin.");
        }

        var identities = _uow.Repository<UserIdentity>();
        var link = await identities.Query(tracking: true)
            .FirstOrDefaultAsync(
                x => x.Provider == identity.Provider && x.ProviderUserId == identity.ProviderUserId,
                cancellationToken);

        if (link is null)
        {
            // Yeni kullanıcı: hesap YOK. Telefon + OTP akışı burada başlar.
            // 🔴 Bu dal BAŞARILI bir doğrulamadır (jeton geçerliydi), yalnız hesap yok —
            // "başarısız" saymak yeni kullanıcı akışını sürekli şüpheli gösterirdi
            // (VerifyOtpCommandHandler'daki aynı karar).
            await RecordAsync(SocialIdentifierFor(identity), null, succeeded: true, null, cancellationToken);

            return SocialLoginResult.NewUser(
                _jwtProvider.GenerateSocialTempToken(identity),
                new SocialPrefill(identity.Provider, identity.Email, identity.DisplayName));
        }

        // ⚠️ IgnoreQueryFilters: silinmiş hesabı "yok" saymak, kullanıcıyı kayıt akışına
        // sokup telefon benzersizliğinde 500'e düşürürdü (verify-otp'deki aynı koruma).
        var user = await _uow.Repository<User>().Query(tracking: true).IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == link.UserId, cancellationToken);

        if (user is null || user.DeletedAt != null)
        {
            // Buraya normalde düşülmez: hesap silinirken kimlik satırları da silinir
            // (DeleteMyAccountCommand). Düşülüyorsa veri tutarsızlığı var — kullanıcıyı
            // yeni kayıt akışına sokmak yerine durumu SÖYLERİZ.
            await RecordAsync(SocialIdentifierFor(identity), user?.Id, succeeded: false,
                LoginFailureReasons.Banned, cancellationToken);
            throw new ForbiddenException("Bu hesap silinmiş. Destek ile iletişime geçin.");
        }

        if (user.IsBanned)
        {
            // 🔴 Ban sosyal girişte de geçerli. Ayrı bir kapı olsaydı banlanan kullanıcı
            // "Google ile giriş"e basıp içeri girerdi ve moderasyon kararı sessizce delinirdi.
            await RecordAsync(SocialIdentifierFor(identity), user.Id, succeeded: false,
                LoginFailureReasons.Banned, cancellationToken);
            throw new ForbiddenException("Hesabınız engellenmiştir.");
        }

        if (!user.IsActive)
        {
            await RecordAsync(SocialIdentifierFor(identity), user.Id, succeeded: false,
                LoginFailureReasons.Inactive, cancellationToken);
            throw new ForbiddenException("Hesabınız pasif durumdadır.");
        }

        // Sağlayıcıdaki güncel bilgiyi tazele: e-posta/ad değişmiş olabilir ve panelde
        // bayat bir e-posta göstermek, hiç göstermemekten kötüdür.
        link.Email = identity.Email;
        link.EmailVerified = identity.EmailVerified;
        link.DisplayName = identity.DisplayName;
        link.LastUsedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(cancellationToken);

        await RecordAsync(SocialIdentifierFor(identity), user.Id, succeeded: true, null, cancellationToken);

        return SocialLoginResult.ExistingUser(
            _jwtProvider.GenerateTokens(user.Id, user.Role.ToRoleString(), user.Phone));
    }

    /// <summary>
    /// Giriş denemesi kaydındaki ham kimlik. <b>Telefon değil</b> — sosyal girişte telefon
    /// hiç geçmiyor; maskeleyici bunu kullanıcı adı gibi maskeler (<c>goo***</c>).
    /// </summary>
    private static string SocialIdentifierFor(SocialIdentityPayload identity)
        => $"{identity.Provider}:{identity.ProviderUserId}";

    /// <summary>
    /// ⚠️ <c>IsPanelUser: false</c> — R3 ("hiç görülmemiş IP") mobil şebekede her gün yanar;
    /// <c>VerifyOtpCommandHandler</c>'daki aynı karar.
    /// </summary>
    private Task RecordAsync(
        string rawIdentifier, Guid? userId, bool succeeded, string? failureReason, CancellationToken ct) =>
        _loginAttempts.RecordAsync(new LoginAttemptRecord(
            Channel: LoginChannels.Social,
            RawIdentifier: rawIdentifier,
            UserId: userId,
            Succeeded: succeeded,
            FailureReason: failureReason,
            IsPanelUser: false), ct);
}
