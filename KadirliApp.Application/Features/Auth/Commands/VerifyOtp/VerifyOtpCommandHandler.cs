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

    public VerifyOtpCommandHandler(IJwtProvider jwtProvider, IOtpService otpService, IUnitOfWork uow)
    {
        _jwtProvider = jwtProvider;
        _otpService = otpService;
        _uow = uow;
    }

    public async Task<VerifyOtpResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = request.Phone.Trim();

        var valid = await _otpService.ValidateAsync(phone, request.Otp);
        if (!valid)
            throw new AppException("Geçersiz veya süresi dolmuş OTP.", "INVALID_OTP");

        var repo = _uow.Repository<User>();
        var user = await repo.Query()
            .FirstOrDefaultAsync(x => x.Phone == phone, cancellationToken);

        if (user == null)
        {
            // Soft-delete edilmiş hesabın telefonu yeniden kayıt açamaz (phone unique —
            // filtre yüzünden görünmez ama insert patlar; 500 yerine anlamlı hata dön).
            var isDeleted = await repo.Query().IgnoreQueryFilters()
                .AnyAsync(x => x.Phone == phone && x.DeletedAt != null, cancellationToken);
            if (isDeleted)
                throw new ForbiddenException("Bu hesap silinmiş. Destek ile iletişime geçin.");

            // Faz 10.2 (masterclass 12.3): kullanıcı artık burada OLUŞTURULMAZ — kısa ömürlü
            // kayıt token'ı döner, kayıt POST /v1/auth/register'da username+mahalle ile tamamlanır.
            return VerifyOtpResult.NewUser(_jwtProvider.GenerateTempToken(phone));
        }

        if (user.IsBanned)
            throw new ForbiddenException("Hesabınız engellenmiştir.");

        if (!user.IsActive)
            throw new ForbiddenException("Hesabınız pasif durumdadır.");

        return VerifyOtpResult.ExistingUser(
            _jwtProvider.GenerateTokens(user.Id, user.Role.ToRoleString(), user.Phone));
    }
}
