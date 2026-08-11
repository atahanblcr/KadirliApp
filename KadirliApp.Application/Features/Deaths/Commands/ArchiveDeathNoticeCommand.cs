using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

/// <summary>
/// Faz 12.10 (plan dışı, <b>zorunlu</b>) — yayındaki bir vefat ilanını erken arşivler.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden bu komut 12.10'da doğdu:</b> <c>RejectDeathNoticeCommand</c> ile aynı sebep —
/// <c>archived</c> durumunu elle yazmanın tek yolu Düzenle formunun durum menüsüydü.
/// Aynı geçişi <c>ArchiveDeathsJob</c> <c>AutoArchiveAt</c> dolduğunda kendiliğinden
/// yapıyor; buradaki elle yol ailenin erken kaldırma talebi içindir.
/// </para>
/// <para>
/// ⚠️ <b>Aksiyon adı önemli:</b> <c>Archive</c> öneki <c>PanelPermissionFilter.ActionFor</c>'un
/// moderasyon listesine 12.10'da <b>eklendi</b>. Eklenmeseydi POST olduğu için sessizce
/// <c>update</c>'e düşerdi ve yalnız düzenleme yetkisi olan moderatör bir ilanı yayından
/// kaldırabilirdi — #29'daki <c>BulkApprove</c> hatasının aynısı.
/// </para>
/// <para>
/// Geri almanın yolu <c>ApproveDeathNoticeCommand</c>'dir: ikinci bir "arşivden çıkar"
/// komutu, aynı geçişi yazan ikinci bir sahip demek olurdu.
/// </para>
/// </remarks>
public record ArchiveDeathNoticeCommand(Guid Id, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "deaths";
    public string AuditAction => "archive";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "DeathNotice";
}

public class ArchiveDeathNoticeCommandHandler : IRequestHandler<ArchiveDeathNoticeCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public ArchiveDeathNoticeCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(ArchiveDeathNoticeCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<DeathNotice>();
        var notice = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (notice == null) return false;

        notice.Archive();

        repo.Update(notice);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
