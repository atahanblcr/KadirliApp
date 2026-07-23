using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Users.Commands.DeleteMyAccount;

/// <summary>
/// Faz 10.8: hesap silme (App Store 5.1.1(v) + Google Play zorunluluğu; KVKK).
/// KARAR — SOFT DELETE + ANONİMLEŞTİRME (hard delete değil): FK bütünlüğü korunur (şikayetler,
/// vefat ilanı AddedBy, audit izleri), kişisel veri alanları temizlenir:
///   Phone → "del"+id'nin ilk 12 hex'i (15 kolon sınırı; unique index korunur, telefon YENİDEN KAYDA AÇILIR),
///   Username/Email/Age/FcmToken/ProfilePhotoUrl/PrimaryNeighborhoodId → null, IsActive=false, DeletedAt=now.
/// İçerik akıbeti: kullanıcının ilanları SOFT-SİLİNİR (yayından düşer), favori satırları silinir;
/// vefat ilanları/şikayetler kalır (topluluk içeriği/işletme kaydı — sahibi "silinmiş kullanıcı").
/// Token'lar: body'deki refresh token jti'si iptal listesine (logout deseni); diğer refresh'ler
/// user DB'den taze okunduğundan IsActive=false ile reddedilir. Access token en fazla 1 gün yaşar (kabul edilen sınır).
/// Yalnız Role=User kendi hesabını silebilir — admin/staff hesapları panelden yönetilir (403).
/// </summary>
public record DeleteMyAccountCommand(Guid UserId, string? RefreshToken) : IRequest<bool>;

public class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtProvider _jwtProvider;
    private readonly ITokenBlacklistService _blacklist;

    public DeleteMyAccountCommandHandler(IUnitOfWork uow, IJwtProvider jwtProvider, ITokenBlacklistService blacklist)
    {
        _uow = uow;
        _jwtProvider = jwtProvider;
        _blacklist = blacklist;
    }

    public async Task<bool> Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Repository<User>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.UserId && x.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.Role != UserRole.User)
            throw new ForbiddenException("Yönetici/personel hesapları bu uçtan silinemez; panel üzerinden yönetilir.");

        // Refresh token best-effort iptali (logout deseni — geçersizse sessizce geçilir).
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

        var now = DateTime.UtcNow;

        // Kullanıcının ilanları yayından düşer (soft delete, atomik toplu UPDATE).
        await _uow.Repository<Ad>().Query()
            .Where(x => x.UserId == request.UserId && x.DeletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, now), cancellationToken);

        // Favori satırları tamamen silinir (join tablosu — kişisel tercih verisi).
        await _uow.Repository<AdFavorite>().Query()
            .Where(x => x.UserId == request.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        // Anonimleştirme: "del" + id'nin ilk 12 hex'i = 15 karakter (kolon sınırı), pratikte benzersiz.
        user.Phone = $"del{user.Id:N}"[..15];
        user.Username = null;
        user.Email = null;
        user.Age = null;
        user.FcmToken = null;
        user.ProfilePhotoUrl = null;
        user.PrimaryNeighborhoodId = null;
        user.IsActive = false;
        user.DeletedAt = now;

        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
