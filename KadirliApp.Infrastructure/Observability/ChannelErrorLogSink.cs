using System.Net;
using System.Threading.Channels;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Observability;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Observability;

/// <summary>
/// Faz 12.1 — hata kayıtlarının **isteği bloklamayan** yazıcısı.
///
/// İki iş yapar: <see cref="IErrorLogSink"/> olarak olayı kuyruğa bırakır (istek yolunda,
/// mikrosaniyeler), <see cref="BackgroundService"/> olarak kuyruğu toplu hâlde veritabanına
/// yazar (istek yolunun dışında).
///
/// 🔴 <b>Neden böyle:</b> sezgisel tasarım <c>ExceptionMiddleware</c>'in <c>catch</c>
/// bloğunda doğrudan DB'ye yazmaktır ve tam olarak yanlış olan odur — veritabanı
/// çöktüğünde hata yazma denemesi de patlar, istisna <c>catch</c>'in içinde doğar,
/// yanıt zarfı hiç yazılmaz ve istemci <b>zarfsız ham 500</b> alır. Yani görünmez
/// sözleşme #10 tam da her şeyin kötü gittiği anda kırılır.
///
/// 🔴 <b>Kendi kendini besleyen döngü yasağı:</b> bu sınıfın kendi hatası <b>asla</b>
/// <c>error_logs</c>'a yazılmaz, yalnız <see cref="ILogger"/>'a düşer. Aksi hâlde
/// DB hatası → hata kaydı denemesi → DB hatası… sonsuz döngü.
/// </summary>
public sealed class ChannelErrorLogSink : BackgroundService, IErrorLogSink
{
    /// <summary>Kuyruk kapasitesi. Yazıcı normalde saniyeler içinde boşaltır; bu sayı
    /// yalnız veritabanı yavaşladığında/durduğunda anlam kazanır.</summary>
    private const int Capacity = 1_000;

    /// <summary>Tek <c>SaveChanges</c>'te işlenecek en fazla olay.</summary>
    private const int BatchSize = 200;

    private readonly Channel<ErrorLogEntry> _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChannelErrorLogSink> _log;

    private long _dropped;
    private DateTime _lastDropWarningUtc = DateTime.MinValue;

    public ChannelErrorLogSink(IServiceScopeFactory scopeFactory, ILogger<ChannelErrorLogSink> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;

        // ⚠️ DropWrite (en yeniyi düşür), DropOldest DEĞİL — bilinçli:
        // (1) TryWrite dolulukta `false` dönebilsin diye. DropOldest'te her yazma "başarılı"
        //     görünür ve olay kaybı SESSİZ olur — bu fazın tam olarak savaştığı sınıf.
        // (2) Kuyruk yalnız DB yavaşladığında dolar; o anda elde tutulması gereken şey
        //     sorunun BAŞLADIĞI andaki olaylardır (kök neden), 900. tekrarı değil.
        _queue = Channel.CreateBounded<ErrorLogEntry>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>Testlerin "kaç olay düştü" sorusunu sorabilmesi için.</summary>
    public long DroppedCount => Interlocked.Read(ref _dropped);

    public bool TryWrite(ErrorLogEntry entry)
    {
        // 🔑 Sözleşme: bu metot ASLA fırlatmaz ve ASLA beklemez. Çağıran taraf
        // (ExceptionMiddleware'in catch bloğu) zaten hata işliyor; orada ikinci bir
        // istisna doğarsa istemci zarfsız yanıt alır.
        try
        {
            if (_queue.Writer.TryWrite(entry))
                return true;

            Interlocked.Increment(ref _dropped);
            WarnAboutDropsThrottled();
            return false;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Hata kaydı kuyruğa yazılamadı (yutuldu — istek etkilenmedi).");
            return false;
        }
    }

    /// <summary>Kuyruk taşması da bir olaydır ama log'u kendi başına sele dönüşmemeli.</summary>
    private void WarnAboutDropsThrottled()
    {
        var now = DateTime.UtcNow;
        if (now - _lastDropWarningUtc < TimeSpan.FromMinutes(1)) return;

        _lastDropWarningUtc = now;
        _log.LogWarning(
            "Hata kaydı kuyruğu dolu — olaylar düşürülüyor (toplam {Dropped}). " +
            "Veritabanı yavaş olabilir.", DroppedCount);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var first in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            var batch = new List<ErrorLogEntry>(BatchSize) { first };
            while (batch.Count < BatchSize && _queue.Reader.TryRead(out var next))
                batch.Add(next);

            try
            {
                await PersistAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // 🔴 DÖNGÜ YASAĞI: buradaki hata error_logs'a YAZILMAZ.
                _log.LogError(ex, "Hata kayıtları yazılamadı; {Count} olay atlandı.", batch.Count);
            }
        }
    }

    private async Task PersistAsync(IReadOnlyList<ErrorLogEntry> batch, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Aynı parti içinde aynı hata onlarca kez olabilir (döngüye giren bir istek) —
        // önce bellekte topla, veritabanına parmak izi başına TEK dokunuş yap.
        var groups = batch
            .Select(e => (Entry: e, Fingerprint: ErrorFingerprint.Compute(e.Source, e.Code, e.Message, e.StackTrace)))
            .GroupBy(x => x.Fingerprint)
            .ToList();

        foreach (var group in groups)
        {
            var newest = group.OrderByDescending(x => x.Entry.OccurredAt ?? DateTime.UtcNow).First().Entry;
            var occurredAt = newest.OccurredAt ?? DateTime.UtcNow;

            var existing = await db.ErrorLogs.FirstOrDefaultAsync(x => x.Fingerprint == group.Key, ct);

            if (existing is null)
                db.ErrorLogs.Add(Build(group.Key, newest, occurredAt, group.Count()));
            else
                Touch(existing, newest, occurredAt, group.Count());
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // 🔑 Beklenen yarış: Api ve Web ayrı süreçler; ikisi aynı anda aynı YENİ hatayı
            // görüp ikisi de "kayıt yok" diyebilir. Benzersiz indeks kaybedeni burada
            // patlatır. Doğru davranış susmak değil, eklemeyi güncellemeye çevirmek —
            // aksi hâlde o parti sessizce kaybolurdu.
            db.ChangeTracker.Clear();
            await RetryAsUpdatesAsync(db, groups, ct);
        }
    }

    private async Task RetryAsUpdatesAsync(
        AppDbContext db,
        IReadOnlyList<IGrouping<string, (ErrorLogEntry Entry, string Fingerprint)>> groups,
        CancellationToken ct)
    {
        foreach (var group in groups)
        {
            var newest = group.OrderByDescending(x => x.Entry.OccurredAt ?? DateTime.UtcNow).First().Entry;
            var occurredAt = newest.OccurredAt ?? DateTime.UtcNow;

            var existing = await db.ErrorLogs.FirstOrDefaultAsync(x => x.Fingerprint == group.Key, ct);
            if (existing is null)
            {
                db.ErrorLogs.Add(Build(group.Key, newest, occurredAt, group.Count()));
                continue;
            }

            Touch(existing, newest, occurredAt, group.Count());
        }

        await db.SaveChangesAsync(ct);
    }

    private static ErrorLog Build(string fingerprint, ErrorLogEntry e, DateTime occurredAt, int count) => new()
    {
        Source = e.Source,
        Level = e.Level,
        Code = e.Code,
        Message = e.Message,
        StackTrace = e.StackTrace,
        Path = e.Path,
        Method = e.Method,
        StatusCode = e.StatusCode,
        TraceId = e.TraceId,
        UserId = e.UserId,
        IpAddress = Parse(e.IpAddress),
        UserAgent = e.UserAgent,
        AppVersion = e.AppVersion,
        Platform = e.Platform,
        OsVersion = e.OsVersion,
        Fingerprint = fingerprint,
        OccurrenceCount = count,
        FirstSeenAt = occurredAt,
        LastSeenAt = occurredAt
    };

    /// <summary>
    /// Var olan kaydı tazeler. 🔑 <b>Çözülmüş bir hata tekrar ederse KENDİLİĞİNDEN açılır</b> —
    /// aksi hâlde "çözdüm" denen bir hata sessizce devam eder ve panel bunu göstermez;
    /// yönetici de listeye bakıp "temiz" sanır.
    /// </summary>
    private static void Touch(ErrorLog existing, ErrorLogEntry e, DateTime occurredAt, int count)
    {
        existing.OccurrenceCount += count;

        if (occurredAt > existing.LastSeenAt)
        {
            existing.LastSeenAt = occurredAt;
            // Son görülen örneğin bağlamı tutulur: traceId eskisi kalırsa Seq'te
            // aylar önceki isteği ararız.
            existing.TraceId = e.TraceId;
            existing.Path = e.Path;
            existing.Method = e.Method;
            existing.StatusCode = e.StatusCode;
            existing.UserId = e.UserId;
            existing.IpAddress = Parse(e.IpAddress);
            existing.UserAgent = e.UserAgent;
            existing.AppVersion = e.AppVersion;
            existing.Platform = e.Platform;
            existing.OsVersion = e.OsVersion;

            if (!string.IsNullOrWhiteSpace(e.StackTrace))
                existing.StackTrace = e.StackTrace;
        }

        if (existing.IsResolved)
        {
            existing.IsResolved = false;
            existing.ResolvedBy = null;
            existing.ResolvedAt = null;
            existing.ResolvedNote = null;
        }
    }

    private static IPAddress? Parse(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value, out var ip) ? ip : null;
}
