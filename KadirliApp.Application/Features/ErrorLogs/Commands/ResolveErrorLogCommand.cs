using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.ErrorLogs.Commands;

/// <summary>
/// Faz 12.1 — hata kaydını "çözüldü" (veya tekrar "açık") olarak işaretler.
///
/// 🔑 <b>Çözme, silme DEĞİLDİR.</b> Kayıt durur, yalnız işaretlenir — çöp kutusundaki
/// "geri getirme yayına almak değildir" kararıyla (görünmez sözleşme #28) aynı aile:
/// bir yönetim eylemi, kaydın kendisiyle ilgili gerçeği değiştirmez. Aynı parmak izi
/// tekrar gelirse kayıt <b>kendiliğinden yeniden açılır</b> (yazıcı işaretleri temizler) —
/// aksi hâlde "çözdüm" denen bir hata sessizce tekrar ederdi ve panel bunu göstermezdi.
///
/// ⚠️ <c>AuditModule</c> "error-logs" — izin matrisinde <b>olmayan</b> bir anahtar
/// (ekran yalnız-admin). <c>PanelDisplay.ModuleLabel</c> bunu Türkçeleştirmezse denetim
/// izi ekranı ham İngilizce basar (Değişmez Kural #6); karşılığı orada tanımlı.
/// </summary>
public sealed record ResolveErrorLogCommand(Guid Id, bool Resolved, string? Note)
    : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "error-logs";
    public string AuditAction => Resolved ? "resolve-error" : "reopen-error";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(ErrorLog);
}

public sealed class ResolveErrorLogCommandHandler : IRequestHandler<ResolveErrorLogCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditContext _audit;

    public ResolveErrorLogCommandHandler(IUnitOfWork uow, IAuditContext audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<bool> Handle(ResolveErrorLogCommand request, CancellationToken ct)
    {
        var entity = await _uow.Repository<ErrorLog>().Query(tracking: true)
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct);

        if (entity is null) return false;

        entity.IsResolved = request.Resolved;

        if (request.Resolved)
        {
            entity.ResolvedBy = _audit.UserId;
            entity.ResolvedAt = DateTime.UtcNow;
            entity.ResolvedNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        }
        else
        {
            // Tekrar açıldığında eski çözüm izi temizlenir — "Açık" rozetinin yanında
            // bayat bir "X kişisi çözdü" notu durmamalı (11.15b'nin onay/red dersi).
            entity.ResolvedBy = null;
            entity.ResolvedAt = null;
            entity.ResolvedNote = null;
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
