using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.News.Commands;

/// <summary>Elle tetiklenen koşunun türü — panelin üç butonu (12.13).</summary>
public enum NewsSyncRequestMode
{
    /// <summary>Yeni ve değişen haberler (varsayılan).</summary>
    Incremental,

    /// <summary>Arşivde daha geriye in (<c>News:Backfill:MaxPosts</c> büyütüldüyse).</summary>
    Archive,

    /// <summary>Kaynakta kalkmış haberleri işaretle / geri geleni yayına al.</summary>
    Reconcile
}

/// <summary>
/// Faz 12.12 — senkronu <b>elle</b> tetikler.
/// </summary>
/// <remarks>
/// 🔑 <b>Neden 12.12'de, panelden önce:</b> bu, checklist §11'deki <i>"kanalı elle dene"</i>
/// maddesinin haber karşılığı. Alım yolu yalnız zamanlanmış işle koşsaydı, yanlış
/// yapılandırmayı (erişilemeyen kaynak, bozuk imleç) <b>en kötü anda</b> ve dolaylı olarak
/// öğrenirdik. Elle tetikleme, "bayrakla kapalı yol = hiç çalıştırılmamış yol" tuzağını da
/// kapatır.
/// </remarks>
public class TriggerNewsSyncCommand : IRequest<ApiResponse<NewsSyncOutcome>>, IAuditableCommand
{
    public NewsSyncRequestMode Mode { get; set; } = NewsSyncRequestMode.Incremental;
    public Guid? AdminId { get; set; }

    public string AuditModule => NewsAudit.Module;
    public string AuditAction => "sync";
    public string? AuditAffectedType => nameof(NewsSyncRun);
}

public class TriggerNewsSyncCommandHandler : IRequestHandler<TriggerNewsSyncCommand, ApiResponse<NewsSyncOutcome>>
{
    private readonly INewsSyncService _sync;

    public TriggerNewsSyncCommandHandler(INewsSyncService sync) => _sync = sync;

    public async Task<ApiResponse<NewsSyncOutcome>> Handle(TriggerNewsSyncCommand request, CancellationToken ct)
    {
        // Hedefleme/alım mantığının ikinci bir gerçeklemesi YOK — üç yol da aynı tek sahipten
        // (INewsSyncService) geçer.
        var outcome = request.Mode switch
        {
            NewsSyncRequestMode.Archive => await _sync.RunArchiveBackfillAsync(NewsSyncTriggers.Manual, request.AdminId, ct),
            NewsSyncRequestMode.Reconcile => await _sync.ReconcileAsync(NewsSyncTriggers.Manual, request.AdminId, ct),
            _ => await _sync.RunIncrementalAsync(NewsSyncTriggers.Manual, request.AdminId, ct)
        };

        return outcome.Succeeded
            ? ApiResponse<NewsSyncOutcome>.SuccessResponse(outcome)
            // ⚠️ "Başlattım" demek yetmez: koşu düştüyse sebebini söyle. Sessizce başarı
            // bildiren bir buton, işlevsiz butondan kötüdür (§7 madde 37).
            : ApiResponse<NewsSyncOutcome>.FailureResponse("SYNC_FAILED",
                outcome.ErrorMessage ?? "Haber senkronu tamamlanamadı.");
    }
}
