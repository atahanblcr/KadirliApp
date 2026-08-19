using System.Globalization;
using System.Text;

namespace KadirliApp.Application.Common.Performance;

/// <summary>
/// Faz 12.22a — <b>gecikme dağılımının saf çekirdeği.</b> Sabit sınırlı kovalardan oluşan
/// bir histogram: her ölçüm bir kovanın sayacını artırır, ham örnek <b>saklanmaz</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden histogram, neden ham örnek değil?</b> Üç gereksinim aynı anda karşılanmak
/// zorundaydı ve yalnız bu yapı üçünü birden karşılıyor:
/// </para>
/// <list type="number">
///   <item><b>Sınırlı bellek.</b> Handler başına maliyet sabittir
///   (<see cref="BucketUpperBoundsMs"/> uzunluğu + 1 sayaç), istek sayısından bağımsız.
///   Ham örnek tutan bir tampon ya belleği büyütür ya da örnekleme yapar — ikincisi
///   p95'i <i>sessizce</i> yanıltır.</item>
///   <item><b>Birleştirilebilirlik.</b> İki histogram kova sayaçları <b>toplanarak</b>
///   birleşir (<see cref="Merge"/>). Bu, ölçümün süreç sınırını aşabilmesinin tek
///   sebebidir: API ve panel <b>ayrı süreçlerdir</b> ve panelin gösterdiği p95 yalnız
///   kendi sürecininki olsaydı ekran <i>doğru görünen yanlış bir sayı</i> basardı.</item>
///   <item><b>Ucuzluk.</b> Ölçüm her isteğin sıcak yolunda koşar; kilit alan ya da
///   ayırma yapan bir ölçüm, ölçtüğü şeyi bozar.</item>
/// </list>
/// <para>
/// ⚠️ <b>Bedeli ve bu bedel bilinçlidir:</b> yüzdelikler <b>yaklaşıktır</b> — bir yüzdelik
/// her zaman içine düştüğü kovanın <i>üst sınırını</i> döndürür, yani gerçek değerin
/// üstünü söyler (asla altını değil). Kovalar bu yüzden log'a yakın seçildi: sıcak bölge
/// (5–250 ms) sık, kuyruk seyrek. Prometheus/OpenTelemetry'nin histogramları da tam
/// olarak bu ödünü verir.
/// </para>
/// <para>
/// 📌 <b>Saf sınıf</b> — zaman, Redis, log yok. <c>RequestHistogramTests</c> onu doğrudan
/// besler; sayaçlar <c>Interlocked</c> ile güncellendiği için kilitsiz ve iş parçacığı
/// güvenlidir.
/// </para>
/// </remarks>
public sealed class RequestHistogram
{
    /// <summary>
    /// Kova üst sınırları (ms). Son kova bu listenin <b>ötesidir</b> (taşma kovası).
    /// ⚠️ Bu dizi değişirse Redis'te duran serileştirilmiş histogramlar farklı uzunlukta
    /// olur — <see cref="TryParse"/> bu yüzden uzunluğu doğrular ve uymayanı <b>yok sayar</b>
    /// (bayat veriyi yanlış kovalara dağıtmak, veriyi kaybetmekten kötüdür).
    /// </summary>
    /// <remarks>
    /// 🔬 <b>Sıcak bölgenin çözünürlüğü ÖLÇÜMLE ayarlandı.</b> İlk kova listesinde 10 ms'in
    /// ardından doğrudan 25 ms geliyordu; 12.22a'nın taban çizgisinde k6 (dışarıdan, kesin)
    /// p95'i <b>19 ms</b> ölçerken panel (içeriden, kovalı) <b>≤25 ms</b> diyordu — ikisi de
    /// doğru ama %30 fazla söylüyordu. 15 ve 75 ms eklenerek sıcak bölge sıklaştırıldı;
    /// kuyruk seyrek kaldı, çünkü 2500 ile 5000 ms arasındaki fark kimsenin kararını
    /// değiştirmez ("çok yavaş" ile "çok yavaş" aynı karardır).
    /// </remarks>
    public static readonly double[] BucketUpperBoundsMs =
        { 1, 2, 5, 10, 15, 25, 50, 75, 100, 250, 500, 1000, 2500, 5000, 10000 };

    private static readonly int BucketCount = BucketUpperBoundsMs.Length + 1;

    private readonly long[] _buckets = new long[BucketCount];
    private long _count;
    private long _totalMicros;
    private long _maxMicros;
    private long _failures;
    private long _slow;

    public long Count => Interlocked.Read(ref _count);
    public long Failures => Interlocked.Read(ref _failures);

    /// <summary>Eşiği aşan istek sayısı — "yavaş" tanımı <c>PerformanceSettings</c>'tedir.</summary>
    public long SlowCount => Interlocked.Read(ref _slow);

    public double MaxMs => Interlocked.Read(ref _maxMicros) / 1000d;

    public double AverageMs
    {
        get
        {
            var count = Count;
            return count == 0 ? 0 : Interlocked.Read(ref _totalMicros) / 1000d / count;
        }
    }

    /// <summary>Bir ölçümü kaydeder. Sıcak yolda koşar: kilit yok, ayırma yok.</summary>
    public void Add(double elapsedMs, bool failed, bool slow)
    {
        var micros = (long)Math.Round(Math.Max(elapsedMs, 0) * 1000d);

        Interlocked.Increment(ref _count);
        Interlocked.Add(ref _totalMicros, micros);
        Interlocked.Increment(ref _buckets[BucketIndex(elapsedMs)]);
        if (failed) Interlocked.Increment(ref _failures);
        if (slow) Interlocked.Increment(ref _slow);

        // En büyük değeri yarışsız güncelle (CAS döngüsü).
        long observed;
        while (micros > (observed = Interlocked.Read(ref _maxMicros)))
        {
            if (Interlocked.CompareExchange(ref _maxMicros, micros, observed) == observed)
                break;
        }
    }

    private static int BucketIndex(double ms)
    {
        for (var i = 0; i < BucketUpperBoundsMs.Length; i++)
            if (ms <= BucketUpperBoundsMs[i])
                return i;

        return BucketUpperBoundsMs.Length; // taşma
    }

    /// <summary>
    /// Yaklaşık yüzdelik (0–1). Kovanın <b>üst sınırını</b> döndürür; taşma kovasına
    /// düşerse gerçek en büyük değeri döndürür — "10000+" demek okuyucuya hiçbir şey
    /// söylemez, gerçek tepe söyler.
    /// </summary>
    /// <remarks>
    /// 🐛 <b>Sonuç ayrıca gerçek tepeyle TAVANLANIR ve bu satır canlı ölçümde doğdu:</b>
    /// 30 çağrılık ilk gerçek tabloda <c>GetNewsQuery</c> <i>p99 = 100 ms</i> ama
    /// <i>en yüksek = 80 ms</i> yazıyordu. İkisi de "yanlış" değildi — p99, 80 ms'i
    /// içeren <c>(50,100]</c> kovasının üst sınırını söylüyordu — ama <b>ekran kendi
    /// kendisiyle çelişiyordu</b> ve çelişen bir ölçüm ekranına kimse güvenmez.
    /// Tavan yaklaşıklığı bozmaz: gerçek yüzdelik tanım gereği en büyük değerden
    /// büyük olamaz, yani tavan tahmini <b>gerçeğe yaklaştırır</b>.
    /// </remarks>
    public double Percentile(double quantile)
    {
        var total = Count;
        if (total == 0) return 0;

        var target = (long)Math.Ceiling(quantile * total);
        if (target < 1) target = 1;

        var max = MaxMs;
        long cumulative = 0;
        for (var i = 0; i < BucketCount; i++)
        {
            cumulative += Interlocked.Read(ref _buckets[i]);
            if (cumulative < target) continue;

            return i < BucketUpperBoundsMs.Length ? Math.Min(BucketUpperBoundsMs[i], max) : max;
        }

        return max;
    }

    /// <summary>Başka bir histogramı bu histogramın üstüne toplar (süreçler arası birleştirme).</summary>
    public void Merge(RequestHistogram other)
    {
        Interlocked.Add(ref _count, other.Count);
        Interlocked.Add(ref _totalMicros, Interlocked.Read(ref other._totalMicros));
        Interlocked.Add(ref _failures, other.Failures);
        Interlocked.Add(ref _slow, other.SlowCount);

        for (var i = 0; i < BucketCount; i++)
            Interlocked.Add(ref _buckets[i], Interlocked.Read(ref other._buckets[i]));

        long observed;
        var incoming = Interlocked.Read(ref other._maxMicros);
        while (incoming > (observed = Interlocked.Read(ref _maxMicros)))
        {
            if (Interlocked.CompareExchange(ref _maxMicros, incoming, observed) == observed)
                break;
        }
    }

    /// <summary>Redis'e yazılabilen kompakt biçim: <c>count|totalMicros|maxMicros|failures|slow|b0,b1,…</c></summary>
    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append(Count.ToString(CultureInfo.InvariantCulture)).Append('|')
          .Append(Interlocked.Read(ref _totalMicros).ToString(CultureInfo.InvariantCulture)).Append('|')
          .Append(Interlocked.Read(ref _maxMicros).ToString(CultureInfo.InvariantCulture)).Append('|')
          .Append(Failures.ToString(CultureInfo.InvariantCulture)).Append('|')
          .Append(SlowCount.ToString(CultureInfo.InvariantCulture)).Append('|');

        for (var i = 0; i < BucketCount; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(Interlocked.Read(ref _buckets[i]).ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    /// <summary>
    /// <see cref="Serialize"/>'ın tersi. ⚠️ <b>Asla fırlatmaz</b> ve tanımadığı biçimi
    /// <c>null</c> ile geçer: ölçüm altyapısı, ölçtüğü uygulamayı düşürmemeli.
    /// </summary>
    public static RequestHistogram? TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var parts = raw.Split('|');
        if (parts.Length != 6) return null;

        var buckets = parts[5].Split(',');
        if (buckets.Length != BucketCount) return null;

        var histogram = new RequestHistogram();
        if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out histogram._count)) return null;
        if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out histogram._totalMicros)) return null;
        if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out histogram._maxMicros)) return null;
        if (!long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out histogram._failures)) return null;
        if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out histogram._slow)) return null;

        for (var i = 0; i < BucketCount; i++)
        {
            if (!long.TryParse(buckets[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out histogram._buckets[i]))
                return null;
        }

        return histogram;
    }
}
