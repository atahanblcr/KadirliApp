using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Legal;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthTokens>
{
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _uow;
    private readonly LegalSettings _legal;

    public RegisterCommandHandler(IJwtProvider jwtProvider, IUnitOfWork uow, LegalSettings legal)
    {
        _jwtProvider = jwtProvider;
        _uow = uow;
        _legal = legal;
    }

    public async Task<AuthTokens> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var phone = _jwtProvider.ValidateTempToken(request.TempToken)
            ?? throw new UnauthorizedException("Geçersiz veya süresi dolmuş kayıt token'ı. Lütfen tekrar OTP isteyin.");

        var username = (request.Username ?? string.Empty).Trim();
        if (username.Length is < 3 or > 30 || username.Any(char.IsWhiteSpace))
            throw new AppException("Kullanıcı adı 3-30 karakter olmalı ve boşluk içermemelidir.", "VALIDATION_ERROR");

        if (request.Age is < 13 or > 120)
            throw new AppException("Yaş 13-120 aralığında olmalıdır.", "VALIDATION_ERROR");

        var users = _uow.Repository<User>();

        if (await users.Query().AnyAsync(x => x.Phone == phone, cancellationToken))
            throw new ConflictException("Bu telefon numarası zaten kayıtlı. Lütfen OTP ile giriş yapın.");

        // verify-otp'deki soft-delete koruması burada da gerekir (temp token 30 dk geçerli).
        var isDeleted = await users.Query().IgnoreQueryFilters()
            .AnyAsync(x => x.Phone == phone && x.DeletedAt != null, cancellationToken);
        if (isDeleted)
            throw new ForbiddenException("Bu hesap silinmiş. Destek ile iletişime geçin.");

        var lowered = username.ToLower();
        if (await users.Query().AnyAsync(
                x => x.Username != null && x.Username.ToLower() == lowered, cancellationToken))
            throw new ConflictException("Bu kullanıcı adı zaten kullanılıyor.");

        var neighborhoodExists = await _uow.Repository<Neighborhood>().Query()
            .AnyAsync(x => x.Id == request.PrimaryNeighborhoodId && x.IsActive, cancellationToken);
        if (!neighborhoodExists)
            throw new AppException("Geçersiz mahalle seçimi.", "VALIDATION_ERROR");

        // Faz 12.7 — sosyal jeton VARSA kaydın en başında doğrulanır.
        // 🔴 Sıra bilinçli: kullanıcı yazıldıktan SONRA doğrulansaydı, geçersiz bir sosyal
        // jeton "hesap açıldı ama bağlanamadı" durumu bırakırdı; kullanıcı hesabının
        // açıldığını bilmeden baştan denerdi ve telefon artık kayıtlı olduğu için
        // "bu numara zaten kayıtlı" hatası alırdı — yani kendi hesabına giremez hâle gelirdi.
        SocialIdentityPayload? socialIdentity = null;
        if (!string.IsNullOrWhiteSpace(request.SocialToken))
        {
            socialIdentity = _jwtProvider.ValidateSocialTempToken(request.SocialToken)
                ?? throw new UnauthorizedException(
                    "Sosyal giriş oturumu sona ermiş. Lütfen tekrar deneyin.");
        }

        var user = new User
        {
            Phone = phone,
            Username = username,
            Age = request.Age,
            PrimaryNeighborhoodId = request.PrimaryNeighborhoodId,
            Role = UserRole.User,
            IsActive = true
        };
        await users.AddAsync(user, cancellationToken);

        // 🐛 Bağ GEZİNME ÖZELLİĞİNDEN kuruluyor, FK skalerinden değil: `users.id`
        // `gen_random_uuid()` varsayılanıyla store-generated, yani `user.Id` bu satırda
        // hâlâ boş (12.2b'nin canlı hatası — checklist §4). İkisi TEK SaveChanges'te yazılır:
        // ayrı yazılsalardı araya düşen bir hata "hesabı var ama sosyal bağlantısı yok"
        // bırakır ve kullanıcı bir daha o düğmeyle giremezdi.
        if (socialIdentity is not null)
            await SocialIdentityLinker.AttachToNewUserAsync(_uow, user, socialIdentity, cancellationToken);

        // 🔴 Faz 12.16 — KVKK rızaları kullanıcı ile AYNI SaveChanges'te yazılır (§7 madde 73).
        // Ayrı yazılsalardı araya düşen bir hata RIZASIZ BİR HESAP bırakırdı: uygulama
        // çalışır, uçlar 200 döner ve o hesabın verisini işlemenin dayanağı hiçbir yerde
        // olmaz. Sosyal kimlikle aynı desen ve aynı gerekçe (12.7).
        var consentResult = await LegalConsentWriter.AttachToNewUserAsync(
            _uow,
            user,
            request.Consents,
            new ConsentContext(ConsentSources.Registration, request.IpAddress, request.UserAgent),
            _legal.RequireConsentAtRegistration,
            DateTime.UtcNow,
            cancellationToken);

        // 🔑 Reddetmek YETMEZ, SEBEBİNİ SÖYLEMEK gerekir: "kayıt olamıyorum" diyen bir
        // kullanıcının ekranında hangi kutunun eksik olduğu yazmalı. Sessizce kaydetmek ise
        // bu bloğun kapatmaya çalıştığı hasarın ta kendisi olurdu.
        if (!consentResult.IsValid)
            throw new AppException(
                $"Kaydı tamamlamak için \"{consentResult.MissingDocumentTitle}\" onayı zorunludur.",
                "MISSING_CONSENT");

        await _uow.SaveChangesAsync(cancellationToken);

        return _jwtProvider.GenerateTokens(user.Id, user.Role.ToRoleString(), user.Phone);
    }
}
