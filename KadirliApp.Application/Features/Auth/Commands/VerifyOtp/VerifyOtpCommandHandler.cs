using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Auth.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, VerifyOtpResult>
{
    private readonly IJwtProvider _jwtProvider;
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _uow;
    private readonly ILoginAttemptRecorder _loginAttempts;

    public VerifyOtpCommandHandler(
        IJwtProvider jwtProvider,
        IOtpService otpService,
        IUnitOfWork uow,
        ILoginAttemptRecorder loginAttempts)
    {
        _jwtProvider = jwtProvider;
        _otpService = otpService;
        _uow = uow;
        _loginAttempts = loginAttempts;
    }

    public async Task<VerifyOtpResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = request.Phone.Trim();
        var repo = _uow.Repository<User>();

        bool valid;
        try
        {
            valid = await _otpService.ValidateAsync(phone, request.Otp);
        }
        catch (RateLimitedException)
        {
            // ⚠️ Faz 12.2: OTP blok kararı da bir giriş denemesidir. Kaydedilmezse
            // vatandaş tarafındaki kaba kuvvet panelde HİÇ görünmez — 11.18'in panel
            // kilidini gözlemleyip mobil tarafı gözlemsiz bırakmak, kapının birini
            // kilitleyip diğerini açık unutmak olurdu.
            await RecordAsync(phone, null, succeeded: false, LoginFailureReasons.OtpBlocked, cancellationToken);
            throw;
        }

        if (!valid)
        {
            // 🔑 <c>UserId</c> bilerek NULL: hatalı OTP dalında kullanıcı tablosuna
            // DOKUNULMUYOR (10.2'den beri geçerli kural, <c>VerifyOtpCommandHandlerTests</c>
            // kilitliyor) — bir OTP kaba kuvveti her denemede fazladan bir sorgu üretmemeli.
            // Kaydın hesaba bağlanması <c>Identifier</c> üzerinden yapılır: maskeleme saf ve
            // deterministik olduğu için aynı telefon her zaman aynı maskeli değeri üretir,
            // panel de kullanıcı ekranında bu iki alanı BİRLİKTE sorgular.
            await RecordAsync(phone, null, succeeded: false, LoginFailureReasons.BadOtp, cancellationToken);
            throw new AppException("Geçersiz veya süresi dolmuş OTP.", "INVALID_OTP");
        }

        var user = await repo.Query()
            .FirstOrDefaultAsync(x => x.Phone == phone, cancellationToken);

        if (user == null)
        {
            // Soft-delete edilmiş hesabın telefonu yeniden kayıt açamaz (phone unique —
            // filtre yüzünden görünmez ama insert patlar; 500 yerine anlamlı hata dön).
            var isDeleted = await repo.Query().IgnoreQueryFilters()
                .AnyAsync(x => x.Phone == phone && x.DeletedAt != null, cancellationToken);
            if (isDeleted)
            {
                await RecordAsync(phone, null, succeeded: false, LoginFailureReasons.Banned, cancellationToken);
                throw new ForbiddenException("Bu hesap silinmiş. Destek ile iletişime geçin.");
            }

            // Faz 10.2 (masterclass 12.3): kullanıcı artık burada OLUŞTURULMAZ — kısa ömürlü
            // kayıt token'ı döner, kayıt POST /v1/auth/register'da username+mahalle ile tamamlanır.
            //
            // 🔑 Bu bir BAŞARILI doğrulamadır (OTP tuttu) ama hesap yok → UserId null.
            // "Başarısız" saymak, yeni kullanıcı akışını sürekli şüpheli gösterirdi.
            await RecordAsync(phone, null, succeeded: true, null, cancellationToken);
            return VerifyOtpResult.NewUser(_jwtProvider.GenerateTempToken(phone));
        }

        if (user.IsBanned)
        {
            await RecordAsync(phone, user.Id, succeeded: false, LoginFailureReasons.Banned, cancellationToken);
            throw new ForbiddenException("Hesabınız engellenmiştir.");
        }

        if (!user.IsActive)
        {
            await RecordAsync(phone, user.Id, succeeded: false, LoginFailureReasons.Inactive, cancellationToken);
            throw new ForbiddenException("Hesabınız pasif durumdadır.");
        }

        await RecordAsync(phone, user.Id, succeeded: true, null, cancellationToken);

        return VerifyOtpResult.ExistingUser(
            _jwtProvider.GenerateTokens(user.Id, user.Role.ToRoleString(), user.Phone));
    }

    /// <summary>
    /// ⚠️ <c>IsPanelUser: false</c> — R3 ("hiç görülmemiş IP") bilinçli olarak yalnız
    /// PANEL kanalında yanar. Mobil şebekede IP her gün değişir; bu kanalda R3 açık
    /// olsaydı her kullanıcının her girişi şüpheli olur ve uyarı listesi ilk günde
    /// kullanılamaz hâle gelirdi.
    /// </summary>
    private Task RecordAsync(string phone, Guid? userId, bool succeeded, string? failureReason, CancellationToken ct) =>
        _loginAttempts.RecordAsync(new LoginAttemptRecord(
            Channel: LoginChannels.MobileOtp,
            RawIdentifier: phone,
            UserId: userId,
            Succeeded: succeeded,
            FailureReason: failureReason,
            IsPanelUser: false), ct);
}
