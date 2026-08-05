using System.ComponentModel.DataAnnotations;

namespace KadirliApp.Application.Features.ErrorLogs.Dtos;

public class ErrorLogResponseDto
{
    public Guid Id { get; set; }
    public string Source { get; set; } = default!;
    public string Level { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? StackTrace { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public string? TraceId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? AppVersion { get; set; }
    public string? Platform { get; set; }
    public string? OsVersion { get; set; }
    public string Fingerprint { get; set; } = default!;
    public int OccurrenceCount { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedNote { get; set; }
}

public class QueryErrorLogDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 25;
    public string? Source { get; set; }
    public string? Level { get; set; }

    /// <summary>null → tümü, false → yalnız açık, true → yalnız çözülmüş.</summary>
    public bool? IsResolved { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public string? Sort { get; set; }
}

/// <summary>
/// Mobil istemcinin <c>POST /v1/client-errors</c> gövdesi.
///
/// ⚠️ <c>Source</c> alanı <b>yok ve olmayacak</b> — sunucuda sabitlenir. İstemci kendi
/// kaydını <c>api</c> kaynaklı gösterebilseydi, "sunucumuzda kaç hata var?" sorusunun
/// cevabı istemciden zehirlenebilirdi.
/// </summary>
public class ReportClientErrorDto
{
    /// <summary>Tavan aşımında **kırpma değil ret** — sessiz kırpma yığını yarıda keser
    /// ve parmak izini bozar (aynı hata iki ayrı kayda düşer).</summary>
    public const int MaxMessageLength = 2_000;

    public const int MaxStackTraceLength = 16_000;

    [Required]
    [StringLength(MaxMessageLength, MinimumLength = 1)]
    public string Message { get; set; } = default!;

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    /// <summary><c>error</c> (varsayılan) veya <c>fatal</c>. Tanınmayan değer <c>error</c>'a düşer.</summary>
    [StringLength(20)]
    public string? Level { get; set; }

    [StringLength(MaxStackTraceLength)]
    public string? StackTrace { get; set; }

    [StringLength(500)]
    public string? Path { get; set; }

    [StringLength(40)]
    public string? AppVersion { get; set; }

    [StringLength(40)]
    public string? Platform { get; set; }

    [StringLength(80)]
    public string? OsVersion { get; set; }
}
