using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Faz 10.3: PATCH /v1/users/me — null olan alanlar değiştirilmez (PATCH semantiği).
/// Username/mahalle değişimi masterclass kuralı gereği 30 günde bir yapılabilir
/// (username_last_changed_at / neighborhood_last_changed_at; ilk değişiklik serbest — kayıt anı sayaç başlatmaz).
/// </summary>
public sealed class UpdateMyProfileCommand : IRequest<MyProfileDto>
{
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public int? Age { get; set; }
    public Guid? PrimaryNeighborhoodId { get; set; }
    /// <summary>Kendi yüklediği bir files kaydının id'si; ProfilePhotoUrl bu dosyanın cdn_url'inden set edilir.</summary>
    public Guid? ProfilePhotoFileId { get; set; }
    /// <summary>true → profil fotoğrafı kaldırılır (ProfilePhotoFileId'den bağımsız).</summary>
    public bool RemoveProfilePhoto { get; set; }
}

public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, MyProfileDto>
{
    // Masterclass kuralı "örn. 30 günde bir" — sabit; ihtiyaç olursa config'e taşınır.
    internal const int UsernameChangeDays = 30;
    internal const int NeighborhoodChangeDays = 30;

    private readonly IUnitOfWork _uow;

    public UpdateMyProfileCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<MyProfileDto> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var users = _uow.Repository<User>();
        var user = await users.Query()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var now = DateTime.UtcNow;

        if (request.Username is not null)
        {
            var username = request.Username.Trim();
            if (username != user.Username)
            {
                // RegisterCommand'deki format kurallarıyla birebir aynı
                if (username.Length is < 3 or > 30 || username.Any(char.IsWhiteSpace))
                    throw new AppException("Kullanıcı adı 3-30 karakter olmalı ve boşluk içermemelidir.", "VALIDATION_ERROR");

                if (user.UsernameLastChangedAt is { } lastUsername && now < lastUsername.AddDays(UsernameChangeDays))
                    throw new AppException(
                        $"Kullanıcı adı {UsernameChangeDays} günde bir değiştirilebilir. Son değişiklik: {lastUsername:yyyy-MM-dd}.",
                        "USERNAME_CHANGE_LIMIT");

                var lowered = username.ToLower();
                var taken = await users.Query().AnyAsync(
                    x => x.Id != user.Id && x.Username != null && x.Username.ToLower() == lowered, cancellationToken);
                if (taken)
                    throw new ConflictException("Bu kullanıcı adı zaten kullanılıyor.");

                user.Username = username;
                user.UsernameLastChangedAt = now;
            }
        }

        if (request.Age is not null)
        {
            if (request.Age is < 13 or > 120)
                throw new AppException("Yaş 13-120 aralığında olmalıdır.", "VALIDATION_ERROR");
            user.Age = request.Age;
        }

        if (request.PrimaryNeighborhoodId is { } neighborhoodId && neighborhoodId != user.PrimaryNeighborhoodId)
        {
            if (user.NeighborhoodLastChangedAt is { } lastNeighborhood && now < lastNeighborhood.AddDays(NeighborhoodChangeDays))
                throw new AppException(
                    $"Mahalle {NeighborhoodChangeDays} günde bir değiştirilebilir. Son değişiklik: {lastNeighborhood:yyyy-MM-dd}.",
                    "NEIGHBORHOOD_CHANGE_LIMIT");

            var neighborhoodExists = await _uow.Repository<Neighborhood>().Query()
                .AnyAsync(x => x.Id == neighborhoodId && x.IsActive, cancellationToken);
            if (!neighborhoodExists)
                throw new AppException("Geçersiz mahalle seçimi.", "VALIDATION_ERROR");

            user.PrimaryNeighborhoodId = neighborhoodId;
            user.NeighborhoodLastChangedAt = now;
        }

        if (request.RemoveProfilePhoto)
        {
            user.ProfilePhotoUrl = null;
        }
        else if (request.ProfilePhotoFileId is { } fileId)
        {
            // Yalnız kullanıcının KENDİ yüklediği dosya kabul edilir (başkasının dosyasını sahiplenme engeli).
            var file = await _uow.Repository<Domain.Entities.File>().Query()
                .FirstOrDefaultAsync(x => x.Id == fileId && x.UploadedBy == user.Id, cancellationToken)
                ?? throw new AppException("Geçersiz profil fotoğrafı dosyası.", "VALIDATION_ERROR");

            user.ProfilePhotoUrl = file.CdnUrl;
        }

        users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        var neighborhoodName = user.PrimaryNeighborhoodId is null
            ? null
            : await _uow.Repository<Neighborhood>().Query()
                .Where(x => x.Id == user.PrimaryNeighborhoodId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);

        return MyProfileDto.FromUser(user, neighborhoodName);
    }
}
