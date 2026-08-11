using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

/// <summary>
/// Faz 12.10 (plan dışı, <b>zorunlu</b>) — vefat ilanını reddeder.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden bu komut 12.10'da doğdu:</b> vefat modülünde reddetmenin tek yolu Düzenle
/// formundaki durum menüsüydü. 12.10 o menüyü kaldırıyor; karşılığı yazılmasaydı
/// "reddet" panelden <b>tamamen kaybolurdu</b> — bir hatayı düzeltirken bir işlevi
/// silmek olurdu. Vefat ilanını vatandaş da gönderebiliyor (<c>POST /v1/deaths</c>),
/// yani reddedilmesi gereken kayıtlar gerçekten oluşuyor.
/// </para>
/// <para>
/// İz <c>reject</c> olarak düşer; izin eylemi aksiyon adının önekinden <c>approve</c>'a
/// türer (#19) — yani <i>düzenleme</i> yetkisi olan moderatör bunu yapamaz.
/// </para>
/// </remarks>
public record RejectDeathNoticeCommand(Guid Id, Guid AdminId, string? Reason = null) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "deaths";
    public string AuditAction => "reject";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "DeathNotice";
    public object? AuditDetails => Reason is not null ? new { reason = Reason } : null;
}

public class RejectDeathNoticeCommandHandler : IRequestHandler<RejectDeathNoticeCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public RejectDeathNoticeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(RejectDeathNoticeCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<DeathNotice>();
        var notice = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (notice == null) return false;

        // Faz 12.10: kuralın tek sahibi varlığın kendisi (Faz 12.11).
        notice.Reject(request.Reason);

        repo.Update(notice);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
