using System.Net;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Security;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Security;

/// <summary>
/// Faz 12.2 — <see cref="ILoginAttemptRecorder"/> gerçeklemesi.
///
/// Üç iş yapar, sırayla: kimliği <b>maskeler</b>, geçmişi <b>tek sorguda</b> toplar,
/// <c>SuspiciousLoginRules</c>'ı çalıştırıp satırı yazar.
///
/// 🔴 <b>Asla fırlatmaz.</b> Kimlik doğrulama kendi gözlemcisi yüzünden başarısız olamaz —
/// bir yöneticinin panele girememesi, o girişin kaydedilememesinden çok daha ağır bir arıza.
/// Bu, 12.1'in <c>IErrorLogSink</c> sözleşmesiyle aynı aile; farkı yazmanın <b>senkron</b>
/// olması: bu satır bir güvenlik kanıtı, kuyruk taşmasında düşürülebilecek telemetri değil.
///
/// ⚠️ IP'yi <b>yalnız burası</b> okur (<c>IHttpContextAccessor</c> → <c>RemoteIpAddress</c>).
/// Değerin doğruluğu <c>ForwardedHeaders</c> ara katmanına bağlı: o kurulmazsa proxy
/// arkasında her deneme aynı IP'den görünür, R2 herkeste yanar, R3 hiç yanmaz.
/// </summary>
public sealed class LoginAttemptRecorder : ILoginAttemptRecorder
{
    /// <summary>
    /// Geçmiş sorgusunun tarayacağı en fazla satır. Yoğun bir saldırıda pencere içinde
    /// on binlerce satır olabilir; sayımlar indeksli olsa da tavan koymak, gözlem
    /// katmanının saldırı anında <b>kendisinin</b> yük üretmesini engeller.
    /// </summary>
    private const int HistoryScanLimit = 500;

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<LoginAttemptRecorder> _log;
    private readonly SuspicionThresholds _thresholds;

    public LoginAttemptRecorder(
        AppDbContext db,
        IHttpContextAccessor http,
        IConfiguration cfg,
        ILogger<LoginAttemptRecorder> log)
    {
        _db = db;
        _http = http;
        _log = log;
        _thresholds = ReadThresholds(cfg);
    }

    /// <summary>
    /// Eşikleri yapılandırmadan okur. ⚠️ Eksik anahtar <b>varsayılana</b> düşer, sıfıra
    /// değil — yapılandırma boşsa kural kapanmaz ("bayrakla kapalı yol" tuzağı).
    /// </summary>
    private static SuspicionThresholds ReadThresholds(IConfiguration cfg)
    {
        var d = SuspicionThresholds.Default;
        return new SuspicionThresholds
        {
            Window = TimeSpan.FromMinutes(
                cfg.GetValue("Security:Suspicion:WindowMinutes", (int)d.Window.TotalMinutes)),
            AccountFailureThreshold =
                cfg.GetValue("Security:Suspicion:AccountFailureThreshold", d.AccountFailureThreshold),
            DistinctAccountsFromIpThreshold =
                cfg.GetValue("Security:Suspicion:DistinctAccountsFromIpThreshold", d.DistinctAccountsFromIpThreshold),
            IpFailureThreshold =
                cfg.GetValue("Security:Suspicion:IpFailureThreshold", d.IpFailureThreshold),
            JustAfterLockoutWindow = TimeSpan.FromMinutes(
                cfg.GetValue("Security:Suspicion:JustAfterLockoutMinutes", (int)d.JustAfterLockoutWindow.TotalMinutes))
        };
    }

    public async Task RecordAsync(LoginAttemptRecord record, CancellationToken ct = default)
    {
        try
        {
            await WriteAsync(record, ct);
        }
        catch (Exception ex)
        {
            // 🔴 Yutulur. Giriş akışı bu istisnayı görmemeli.
            _log.LogError(ex,
                "Giriş denemesi kaydedilemedi (yutuldu — giriş akışı etkilenmedi). Kanal: {Channel}",
                record.Channel);
        }
    }

    private async Task WriteAsync(LoginAttemptRecord record, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var ctx = _http.HttpContext;
        var ip = ctx?.Connection.RemoteIpAddress;
        var userAgent = ctx?.Request.Headers.UserAgent.ToString();

        var identifier = LoginIdentifierMasker.MaskIdentifier(record.RawIdentifier);
        var history = await BuildHistoryAsync(record, identifier, ip, now, ct);

        var rule = SuspiciousLoginRules.Evaluate(record, history, _thresholds, now);

        _db.LoginAttempts.Add(new LoginAttempt
        {
            Channel = record.Channel,
            Identifier = identifier,
            UserId = record.UserId,
            Succeeded = record.Succeeded,
            FailureReason = record.Succeeded ? null : record.FailureReason,
            IpAddress = ip,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : Truncate(userAgent, 500),
            IsSuspicious = rule is not null,
            SuspicionRule = rule
        });

        await _db.SaveChangesAsync(ct);

        if (rule is not null)
        {
            // Uyarı e-postası 5 dakikada bir toplu gider; log satırı ANINDA düşer.
            // Seq'e bakan biri postayı beklemek zorunda kalmasın.
            _log.LogWarning(
                "Şüpheli giriş denemesi: kural {Rule}, kanal {Channel}, kimlik {Identifier}, IP {Ip}",
                rule, record.Channel, identifier, ip?.ToString() ?? "-");
        }
    }

    /// <summary>
    /// Kuralların ihtiyaç duyduğu geçmişi toplar.
    /// </summary>
    /// <remarks>
    /// ⚠️ Sayımlar <b>değerlendirilen denemeyi de kapsar</b>: satır henüz yazılmadığı için
    /// veritabanından gelen sayıya <c>+1</c> eklenir. Eklenmeseydi R1 eşiği fiilen bir
    /// fazlaya kayar ve <b>kilitlenen hesap için uyarı doğmazdı</b> — kilit 5. denemede
    /// kapanır, uyarı 6.'da yanardı ama 6. deneme hiç gelmez (hesap kilitli).
    /// Tam olarak "iki taraf farklı gerçeklik görür" hatası.
    /// </remarks>
    private async Task<LoginHistorySnapshot> BuildHistoryAsync(
        LoginAttemptRecord record, string maskedIdentifier, IPAddress? ip, DateTime now, CancellationToken ct)
    {
        var since = now - _thresholds.Window;

        var accountFailures = 0;
        if (record.UserId is { } userId)
        {
            accountFailures = await _db.LoginAttempts
                .Where(x => x.UserId == userId && !x.Succeeded && x.CreatedAt >= since)
                .Take(HistoryScanLimit)
                .CountAsync(ct);
        }
        else
        {
            // Hesabı olmayan denemede (unknown_user) kimlik üzerinden say — aksi hâlde
            // var olmayan bir kullanıcı adına yapılan kaba kuvvet HİÇ sayılmazdı.
            accountFailures = await _db.LoginAttempts
                .Where(x => x.Identifier == maskedIdentifier && !x.Succeeded && x.CreatedAt >= since)
                .Take(HistoryScanLimit)
                .CountAsync(ct);
        }

        var ipFailures = 0;
        var distinctIdentifiers = 0;
        var ipSeenBefore = true;

        if (ip is not null)
        {
            var recentFromIp = await _db.LoginAttempts
                .Where(x => x.IpAddress != null && x.IpAddress == ip && !x.Succeeded && x.CreatedAt >= since)
                .Select(x => x.Identifier)
                .Take(HistoryScanLimit)
                .ToListAsync(ct);

            ipFailures = recentFromIp.Count;
            distinctIdentifiers = recentFromIp.Distinct(StringComparer.Ordinal).Count();

            // Değerlendirilen deneme başarısızsa kendi kimliğini de kümeye kat.
            if (!record.Succeeded && !recentFromIp.Contains(maskedIdentifier, StringComparer.Ordinal))
                distinctIdentifiers++;

            if (record.Succeeded && record.UserId is { } uid)
            {
                ipSeenBefore = await _db.LoginAttempts
                    .AnyAsync(x => x.UserId == uid && x.Succeeded && x.IpAddress != null && x.IpAddress == ip, ct);
            }
        }
        else if (record.Succeeded)
        {
            // IP okunamıyorsa (in-process test sunucusu, unix soketi) R3 kararı verilemez.
            // "Bilmiyorum" halinde uyarı ÜRETMEMEK doğru davranış: bilinmeyeni şüpheli
            // saymak, ilk günden itibaren susturulan bir alarm demektir.
            ipSeenBefore = true;
        }

        return new LoginHistorySnapshot(
            RecentAccountFailures: record.Succeeded ? accountFailures : accountFailures + 1,
            RecentIpFailures: record.Succeeded ? ipFailures : ipFailures + 1,
            DistinctIdentifiersFromIp: distinctIdentifiers,
            IpSeenBeforeForUser: ipSeenBefore,
            LockedOutUntil: record.LockedOutUntil);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
