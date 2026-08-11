using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.12 — <b>bir senkron koşusu</b>: "ne zaman koştu, ne getirdi, neyi kaçırdı?"
///
/// 🔑 Tasarımı <see cref="PushCampaign"/>'den kopyalandı (12.2b) ve aynı sebeple:
/// sayaçlar <b>artımlı</b> yazılır, sorgu anında <c>COUNT</c> ile hesaplanmaz (§7 madde 39).
///
/// 🔴 <b>Bu tablo bu bloğun 1 numaralı hasar sınıfına karşı var: kaynak sessizce susabilir.</b>
/// Senkron durursa (WP kapandı, imleç bozuldu, job kuyruğu takıldı) uygulama <b>eski haberi
/// göstermeye devam eder</b>, uçlar 200 döner, log temizdir, kimse hata almaz. Bayatlığı
/// görünür kılan tek şey "son başarılı koşu ne zaman?" sorusunun cevabıdır
/// (<c>NewsSyncHealth</c> + 12.13'ün Dashboard kutusu).
/// </summary>
public class NewsSyncRun : BaseEntity
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary><see cref="NewsSyncTriggers"/>: <c>schedule</c> · <c>manual</c>.</summary>
    public string Trigger { get; set; } = NewsSyncTriggers.Schedule;

    /// <summary><see cref="NewsSyncModes"/>: <c>incremental</c> · <c>archive</c> · <c>reconcile</c>.</summary>
    public string Mode { get; set; } = NewsSyncModes.Incremental;

    /// <summary>Elle tetikleyen yönetici; zamanlanmış koşuda <c>null</c>.</summary>
    public Guid? TriggeredBy { get; set; }

    /// <summary>Kaynaktan okunan gönderi sayısı.</summary>
    public int Fetched { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }

    /// <summary>Sağlaması değişmediği için <b>hiç yazılmayan</b> kayıt sayısı.</summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Hata alan sayfa/kayıt sayısı. 🔑 <b>Bir sayfanın hatası koşuyu düşürmez</b> —
    /// sayılır ve koşu devam eder (§7 madde 29'un "kayıt başına hata partiyi durdurmamalı"
    /// kuralının aynısı). Kaynak kararsız: örnekleme sırasında canlıda <c>520</c> görüldü.
    /// </summary>
    public int Failed { get; set; }

    /// <summary><see cref="NewsSyncStatuses"/>: <c>running</c> · <c>completed</c> · <c>failed</c>.</summary>
    public string Status { get; set; } = NewsSyncStatuses.Running;

    public string? ErrorMessage { get; set; }

    /// <summary>Artımlı koşuda sorgulanan pencerenin başı (UTC); arşiv koşusunda <c>null</c>.</summary>
    public DateTime? CursorFrom { get; set; }

    /// <summary>Koşu sonunda ileri imlecin geldiği nokta (UTC).</summary>
    public DateTime? CursorTo { get; set; }

    /// <summary>Mutabakat koşusunda <c>gone</c> işaretlenen kayıt sayısı.</summary>
    public int MarkedGone { get; set; }

    /// <summary>Mutabakat koşusunda kaynağa <b>geri dönen</b> kayıt sayısı.</summary>
    public int Restored { get; set; }
}

public static class NewsSyncTriggers
{
    public const string Schedule = "schedule";
    public const string Manual = "manual";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Schedule, Manual };
}

/// <summary>
/// Koşunun türü. 🔑 <b>İki imleç tek işte birleşmez:</b> artımlı iş 15 dakikada bir koşar,
/// arşiv derinleştirmesi yalnız derinlik eksikse ve istekle koşar.
/// </summary>
public static class NewsSyncModes
{
    /// <summary>İleri imleç: <c>orderby=modified&amp;order=asc</c> + <c>modified_after</c>.</summary>
    public const string Incremental = "incremental";

    /// <summary>Geri imleç: <c>orderby=date&amp;order=desc&amp;page=N</c>, derinlik tavanına kadar.</summary>
    public const string Archive = "archive";

    /// <summary>Silmeyi öğrenmenin tek yolu: kimlik taraması.</summary>
    public const string Reconcile = "reconcile";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Incremental, Archive, Reconcile };
}

public static class NewsSyncStatuses
{
    public const string Running = "running";

    /// <summary>Koşu sonuna geldi. ⚠️ <c>Failed &gt; 0</c> olabilir — kısmi hata koşuyu düşürmez.</summary>
    public const string Completed = "completed";

    /// <summary>Koşu hiçbir şey yapamadan düştü (kaynak tümüyle erişilemez, beklenmeyen istisna).</summary>
    public const string Failed = "failed";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Running, Completed, Failed };
}
