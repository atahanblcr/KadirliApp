namespace KadirliApp.Application.Common.Performance;

/// <summary>
/// Faz 12.22a — <b>"yavaş" kelimesinin tek tanımı.</b>
/// </summary>
/// <remarks>
/// 🔴 Eşik bir sayı değil, bir <b>gürültü kararı</b>: her isteği <c>Information</c>'a
/// yazmak Seq'i çöplüğe çevirir ve gerçek uyarı o çöplükte kaybolur (§7 madde 36'nın
/// <i>"kendimize DoS"</i> dersinin log karşılığı). Bu yüzden ölçüm <b>her zaman</b>
/// toplanır ama log'a yalnız eşiği aşan istek düşer.
/// ⚠️ Değer <b>çözülme anında</b> okunur (<c>IOptions</c>), DI kaydında değil —
/// kayıt anında okunan bir değer <c>ConfigureAppConfiguration</c> ile ezilemez, yani
/// kod kendi testinden erişilemez olurdu (12.7'nin bulduğu hata).
/// </remarks>
public class PerformanceSettings
{
    public const string SectionName = "Performance";

    /// <summary>Bu süreyi aşan handler <c>Warning</c> olarak loglanır (ms).</summary>
    public int SlowRequestThresholdMs { get; set; } = 500;

    /// <summary>
    /// Ölçüm açık mı? 🔴 Kapatma yolu <b>bilinçli olarak var</b>: ölçüm altyapısı
    /// ölçtüğü uygulamayı düşürmemeli ve düşürüyorsa kapatılabilmeli.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Faz 12.22a — ölçümün <b>yazma</b> ucu. Sıcak yolda koşar; gerçeklemesi <b>asla
/// fırlatmamalı</b> ve bloklamamalıdır.
/// </summary>
public interface IRequestMetricsRecorder
{
    void Record(string handler, double elapsedMs, bool failed, bool slow);
}

/// <summary>
/// Faz 12.22a — ölçümün <b>okuma</b> ucu. Panel bunu kullanır.
/// </summary>
/// <remarks>
/// 🔑 Okuma ayrı bir arayüz, çünkü <b>kapsamı farklı</b>: yazma tek sürecin kendi
/// sayaçlarına, okuma <b>bütün süreçlerin birleşimine</b> bakar (API ve panel ayrı
/// süreçlerdir — bkz. <see cref="RequestHistogram"/>).
/// </remarks>
public interface IRequestMetricsReader
{
    Task<RequestMetricsSnapshot> ReadAsync(CancellationToken ct = default);

    /// <summary>Bütün sayaçları sıfırlar — taban çizgisi ölçümünden önce temiz sayfa açmak için.</summary>
    Task ResetAsync(CancellationToken ct = default);
}

/// <summary>Tek bir handler'ın birleştirilmiş ölçümü.</summary>
/// <param name="Handler">MediatR istek tipinin adı (ör. <c>GetNewsQuery</c>).</param>
/// <param name="Kind">"Sorgu" / "Komut" — <b>addan türetilir</b>, ayrıca saklanmaz.</param>
/// <param name="Count">Toplam çağrı.</param>
/// <param name="Failures">İstisnayla biten çağrı.</param>
/// <param name="SlowCount">Eşiği aşan çağrı.</param>
public record HandlerMetrics(
    string Handler,
    string Kind,
    long Count,
    long Failures,
    long SlowCount,
    double AverageMs,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs);

/// <summary>
/// Panelin gösterdiği tablo + <b>ölçümün kendisi hakkında</b> bilinmesi gerekenler.
/// </summary>
/// <param name="Handlers">p95'e göre azalan sıralı.</param>
/// <param name="Sources">Ölçüme katkı veren süreçler (ör. <c>KadirliApp.Api</c>).</param>
/// <param name="Degraded">
/// 🔴 Ölçüm okunamadı mı? Redis erişilemezse tablo <b>boş</b> döner — ve boş bir tablo
/// <i>"hiç istek gelmedi"</i> ile <i>"ölçüm çalışmıyor"</i> arasında ayrım yapamaz.
/// Bu bayrak o ayrımı yapar; ekran onu yazar.
/// </param>
/// <param name="ThresholdMs">Yürürlükteki yavaşlık eşiği — ekran "yavaş" sütununu bununla açıklar.</param>
public record RequestMetricsSnapshot(
    IReadOnlyList<HandlerMetrics> Handlers,
    IReadOnlyList<string> Sources,
    bool Degraded,
    int ThresholdMs)
{
    public static RequestMetricsSnapshot Empty(int thresholdMs, bool degraded = false)
        => new(Array.Empty<HandlerMetrics>(), Array.Empty<string>(), degraded, thresholdMs);
}

/// <summary>
/// Handler adından "sorgu mu komut mu" türetmenin <b>tek sahibi</b>.
/// </summary>
/// <remarks>
/// ⚠️ Ayrıca saklanmıyor, çünkü saklanan bir alan <b>ikinci bir doğruluk kaynağıdır</b>:
/// Redis'te duran bayat bir kayıt, adı değişen bir isteği yanlış sınıflandırır ve bunu
/// hiçbir şey söylemez.
/// </remarks>
public static class RequestKind
{
    public const string Query = "Sorgu";
    public const string Command = "Komut";
    public const string Other = "Diğer";

    public static string FromName(string handler)
        => handler.EndsWith("Query", StringComparison.Ordinal) ? Query
         : handler.EndsWith("Command", StringComparison.Ordinal) ? Command
         : Other;
}
