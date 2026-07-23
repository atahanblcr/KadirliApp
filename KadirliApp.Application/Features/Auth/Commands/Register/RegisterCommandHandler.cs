using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthTokens>
{
    private readonly IJwtProvider _jwtProvider;
    private readonly IUnitOfWork _uow;

    public RegisterCommandHandler(IJwtProvider jwtProvider, IUnitOfWork uow)
    {
        _jwtProvider = jwtProvider;
        _uow = uow;
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
        await _uow.SaveChangesAsync(cancellationToken);

        return _jwtProvider.GenerateTokens(user.Id, user.Role.ToRoleString(), user.Phone);
    }
}
