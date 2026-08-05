namespace KadirliApp.Application.Common.Interfaces;

/// <summary>Hata kaydının nereden geldiği. İstemciden gelen istekte <b>sunucuda sabitlenir</b>.</summary>
public static class ErrorLogSources
{
    public const string Api = "api";
    public const string Web = "web";
    public const string Mobile = "mobile";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Api, Web, Mobile };
}

public static class ErrorLogLevels
{
    public const string Error = "error";
    public const string Fatal = "fatal";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Error, Fatal };
}

/// <summary>
/// Sink'e verilen tek bir hata olayı. Parmak izi ve maskeleme <b>yazıcıda</b> yapılır —
/// çağıran tarafın (middleware, controller) bunu bilmesi gerekmez.
/// </summary>
public sealed record ErrorLogEntry(
    string Source,
    string Level,
    string Code,
    string Message,
    string? StackTrace = null,
    string? Path = null,
    string? Method = null,
    int? StatusCode = null,
    string? TraceId = null,
    Guid? UserId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? AppVersion = null,
    string? Platform = null,
    string? OsVersion = null,
    DateTime? OccurredAt = null);

/// <summary>
/// Faz 12.1 — hata kaydının **isteği düşürmeyen** yazma yolu.
///
/// 🔴 <b>Neden kuyruk, neden senkron değil:</b> en sezgisel tasarım
/// <c>ExceptionMiddleware</c>'in <c>catch</c> bloğunda doğrudan DB'ye yazmaktır ve
/// tam olarak yanlış olan da odur. Veritabanı çöktüğünde hata yazma denemesi de patlar;
/// istisna <c>catch</c>'in <b>içinde</b> doğar, yanıt zarfı hiç yazılmaz ve istemci
/// <b>zarfsız ham 500</b> alır — yani görünmez sözleşme #10 (her yanıt
/// <c>{success,data,meta}</c> + <c>traceId</c>) tam da her şeyin kötü gittiği anda kırılır.
/// Üstüne yanıt süresi DB'nin zaman aşımı kadar uzar.
///
/// Bu yüzden sözleşme nettir: <see cref="TryWrite"/> <b>asla fırlatmaz ve asla beklemez.</b>
/// Kuyruk doluysa olay düşer — hata kaydı kaybetmek, isteği kaybetmekten iyidir.
/// </summary>
public interface IErrorLogSink
{
    /// <summary>Olayı kuyruğa bırakır. Kuyruk doluysa <c>false</c> döner; **fırlatmaz**.</summary>
    bool TryWrite(ErrorLogEntry entry);
}
