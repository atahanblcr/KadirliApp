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
public class TriggerNewsSyncCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public NewsSyncRequestMode Mode { get; set; } = NewsSyncRequestMode.Incremental;
    public Guid? AdminId { get; set; }

    public string AuditModule => NewsAudit.SyncModule;
    public string AuditAction => "sync";
    public string? AuditAffectedType => nameof(NewsSyncRun);
}

/// <summary>
/// 🔴 Faz 12.13 — komut artık koşuyu <b>çalıştırmıyor, kuyruğa atıyor</b>.
/// </summary>
/// <remarks>
/// 12.12'de bu handler <c>INewsSyncService</c>'i doğrudan çağırıyordu; panelin butonu ona
/// bağlandığı anda istek içinde <b>dakikalarca</b> sürebilen bir iş koşacaktı. Sonuç
/// tahmin edilebilir: panelin zaman aşımı → yönetici F5 → <b>ikinci koşu</b>. Yani
/// engellemeye çalıştığımız şeyi butonun kendisi üretirdi.
/// <para>
/// 🔑 Alım mantığının tek sahipliği <b>bozulmadı</b>: kuyruktaki iş de aynı
/// <c>INewsSyncService</c>'i çağırıyor (<c>NewsSyncTriggerJob</c>). Değişen tek şey
/// <i>ne zaman</i> çağrıldığı.
/// </para>
/// ⚠️ Denetim izi <b>tıklama anında</b> düşer: koşu kilide takılıp hiç açılmasa bile
/// "kim ne zaman tetiklemek istedi" sorusunun cevabı kalmalı.
/// </remarks>
public class TriggerNewsSyncCommandHandler : IRequestHandler<TriggerNewsSyncCommand, ApiResponse<bool>>
{
    private readonly INewsSyncQueue _queue;

    public TriggerNewsSyncCommandHandler(INewsSyncQueue queue) => _queue = queue;

    public Task<ApiResponse<bool>> Handle(TriggerNewsSyncCommand request, CancellationToken ct)
    {
        _queue.Enqueue(request.Mode, request.AdminId);
        return Task.FromResult(ApiResponse<bool>.SuccessResponse(true));
    }
}
