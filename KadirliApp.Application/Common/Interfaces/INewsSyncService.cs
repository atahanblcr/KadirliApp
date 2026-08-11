using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>Bir koşunun sonucu — panel mesajları ve testler bunu okur.</summary>
public sealed record NewsSyncOutcome(
    Guid RunId,
    string Mode,
    string Status,
    int Fetched,
    int Created,
    int Updated,
    int Skipped,
    int Failed,
    int MarkedGone,
    int Restored,
    string? ErrorMessage)
{
    public bool Succeeded => Status == NewsSyncStatuses.Completed;
}

/// <summary>
/// Faz 12.12 — haber alımının <b>tek sahibi</b>.
///
/// Hangfire işleri (<c>SyncNewsJob</c>, <c>ReconcileNewsJob</c>) ve panelin "Senkronu
/// başlat" butonu (12.13) <b>aynı</b> metotlardan geçer. İkinci bir alım gerçeklemesi
/// yazılırsa iki yol farklı kayıt üretir ve ikisi de hiç hata vermez (§7 madde 38'in sınıfı).
/// </summary>
public interface INewsSyncService
{
    /// <summary>
    /// İleri imleç. ⚠️ İmleç henüz yoksa (boş veritabanı) <b>önce arşiv derinleştirmesi</b>
    /// koşar — yoksa 27.284 haberlik akışın başına dönmek gerekirdi.
    /// </summary>
    Task<NewsSyncOutcome> RunIncrementalAsync(string trigger, Guid? triggeredBy, CancellationToken ct);

    /// <summary>
    /// Geri imleç: yapılandırılmış derinliğe (<c>News:Backfill:MaxPosts</c>) ulaşana kadar.
    /// Derinlik zaten doluysa hiçbir şey yapmaz (idempotent).
    /// </summary>
    Task<NewsSyncOutcome> RunArchiveBackfillAsync(string trigger, Guid? triggeredBy, CancellationToken ct);

    /// <summary>
    /// Mutabakat: kaynakta olmayanı <c>gone</c>, geri döneni <c>published</c> yapar.
    /// <b>Silmenin öğrenilebildiği tek yol</b> — <c>modified_after</c> silmeyi hiç bildirmez.
    /// </summary>
    Task<NewsSyncOutcome> ReconcileAsync(string trigger, Guid? triggeredBy, CancellationToken ct);
}
