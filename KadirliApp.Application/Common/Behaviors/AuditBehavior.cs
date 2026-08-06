using System.Text.Json;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Application.Common.Behaviors;

/// <summary>
/// Faz 10.9(i): IAuditableCommand başarıyla tamamlandıktan sonra audit_logs'a satır yazar.
/// KARAR: audit yazımı isteği düşürmez (iş zaten yapıldı — CacheInvalidationBehavior emsali),
/// hata loglanır. bool dönen komut false döndüyse (kayıt bulunamadı) iz yazılmaz;
/// Guid dönen create komutlarında AffectedId yanıttan alınır.
/// </summary>
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditContext _auditContext;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _log;

    public AuditBehavior(IUnitOfWork uow, IAuditContext auditContext, ILogger<AuditBehavior<TRequest, TResponse>> log)
        => (_uow, _auditContext, _log) = (uow, auditContext, log);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        if (request is not IAuditableCommand auditable || _auditContext.UserId is not { } userId)
            return response;

        if (response is bool ok && !ok)
            return response; // kayıt bulunamadı — değişiklik olmadı, iz yok

        // Eski desen komutlar (announcements/power-outages delete) ApiResponse<bool> döner —
        // Success=false yine "değişiklik olmadı" demektir, iz yazılmaz.
        if (response is Models.ApiResponse<bool> { Success: false })
            return response;

        // 🐛 Faz 12.2b: aynı kural ApiResponse<Guid> için EKSİKTİ ve fark edilmemişti, çünkü
        // o güne kadar Guid dönen hiçbir komut IAuditableCommand değildi. İlk öyle komut
        // (SendPushCampaignCommand) yazılırken çıktı: doğrulamadan dönen bir RET,
        // denetim izine "bildirim gönderdi" satırı yazacaktı — hiçbir bildirim gitmeden.
        // Denetim izinin yalan söylemesi, iz tutmamasından kötüdür.
        if (response is Models.ApiResponse<Guid> { Success: false })
            return response;

        try
        {
            var audit = new AuditLog
            {
                UserId = userId,
                Action = auditable.AuditAction,
                Module = auditable.AuditModule,
                // ⚠️ `response as Guid?` yalnız komut DÜZ Guid döndüğünde çalışır;
                // ApiResponse<Guid> saran komutlarda kimlik zarfın içindedir ve
                // sarılmadan bakılırsa iz "hangi kayıt" sorusunu boş bırakır.
                AffectedId = auditable.AuditAffectedId
                             ?? response as Guid?
                             ?? (response as Models.ApiResponse<Guid>)?.Data,
                AffectedType = auditable.AuditAffectedType,
                Details = auditable.AuditDetails is { } details ? JsonSerializer.Serialize(details) : null,
                IpAddress = _auditContext.IpAddress,
                UserAgent = _auditContext.UserAgent
            };

            await _uow.Repository<AuditLog>().AddAsync(audit, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Audit yazımı başarısız: {Module}/{Action} ({Request})",
                auditable.AuditModule, auditable.AuditAction, typeof(TRequest).Name);
        }

        return response;
    }
}
