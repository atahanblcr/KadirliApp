using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Auth;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Users.Commands.RemoveUserIdentity;

/// <summary>
/// Faz 12.7 — <b>yöneticinin</b> bir kullanıcının sosyal bağlantısını kaldırması (panel).
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden kullanıcının kendi ucundan AYRI bir komut:</b> tek fark denetim izidir ve o
/// fark önemlidir. Vatandaşın kendi hesabında yaptığı işlem <c>audit_logs</c>'a
/// <b>yazılmamalı</b> (o tablo yönetici eylemlerinin defteri; vatandaş eylemleriyle
/// doldurulursa "kim ne yaptı" sorusu okunamaz hâle gelir), yöneticinin başkasının hesabında
/// yaptığı işlem ise <b>mutlaka</b> yazılmalı. Aynı komuta <c>IAuditableCommand</c> eklenseydi
/// ikisi ayrılamazdı.
/// </para>
/// <para>
/// ⚠️ Silmenin kendisi <b>tek sahiplidir</b> (<see cref="SocialIdentityLinker.UnlinkAsync"/>) —
/// ayrılan yalnız denetim izi.
/// </para>
/// <para>
/// 📌 <b>Bu işlem kullanıcıyı hesabından KİLİTLEMEZ</b> — telefon + OTP her zaman ayakta
/// (telefon-çıpa kararının ikinci somut kazancı). Yönetici bu düğmeye "yanlış hesap bağlanmış"
/// şikâyetinde basar.
/// </para>
/// </remarks>
public sealed record RemoveUserIdentityCommand(Guid UserId, string Provider)
    : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "users";
    public string AuditAction => "unlink_identity";
    public Guid? AuditAffectedId => UserId;
    public string? AuditAffectedType => nameof(User);

    // ⚠️ Yalnız sağlayıcı adı yazılır. `provider_user_id` ve e-posta KİŞİSEL VERİDİR ve
    // audit_logs panelde görülüyor + CSV'ye çıkıyor (§7 madde 33/34'ün aynı sınıfı).
    public object? AuditDetails => new { Provider };
}

public sealed class RemoveUserIdentityCommandHandler
    : IRequestHandler<RemoveUserIdentityCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RemoveUserIdentityCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(RemoveUserIdentityCommand request, CancellationToken cancellationToken)
    {
        var provider = SocialProviders.Normalize(request.Provider)
            ?? throw new AppException("Desteklenmeyen giriş yöntemi.", "VALIDATION_ERROR");

        // ⚠️ IgnoreQueryFilters: silinmiş bir hesabın artık kimliği olmaması gerekir
        // (DeleteMyAccountCommand siliyor), ama panelin bu düğmesi bir TEMİZLİK aracıdır —
        // tutarsız bir satır varsa yöneticinin onu kaldırabilmesi lazım.
        var exists = await _uow.Repository<User>().Query().IgnoreQueryFilters()
            .AnyAsync(x => x.Id == request.UserId, cancellationToken);

        if (!exists)
            throw new NotFoundException(nameof(User), request.UserId);

        var removed = await SocialIdentityLinker.UnlinkAsync(
            _uow, request.UserId, provider, cancellationToken);

        if (removed)
            await _uow.SaveChangesAsync(cancellationToken);

        return removed;
    }
}
