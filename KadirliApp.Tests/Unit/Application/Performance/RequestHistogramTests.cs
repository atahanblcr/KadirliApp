using FluentAssertions;
using KadirliApp.Application.Common.Performance;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Performance;

/// <summary>
/// Faz 12.22a — görünmez sözleşme <b>#83</b>'ün <b>saf</b> ayağı: gecikme dağılımının çekirdeği.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Ölçüm altyapısının kendi hata sınıfı, ölçtüğü şeyinkinden daha sinsidir:</b> yanlış
/// bir p95, bir istisna gibi görünmez — <b>sayı olarak görünür</b>. Bir sonraki oturum o
/// sayıya bakıp "hızlıyız" der ve optimizasyonu erteler. Bu yüzden çekirdek saf tutuldu
/// (zaman/Redis/log yok) ve doğrudan besleniyor.
/// </para>
/// <para>
/// 🔑 <b>İddialar sözleşmenin üç ayrı yüzünü tutuyor:</b> (a) yüzdelik gerçeğin
/// <b>üstünü</b> söyler, asla altını — yaklaşıklığın YÖNÜ bir sözleşmedir, büyüklüğü değil;
/// (b) birleştirme toplamadır — süreçler arası okumanın tamamı buna dayanıyor;
/// (c) serileştirme gidiş-dönüşte kayıpsızdır ve <b>tanımadığı biçimi yok sayar</b>.
/// </para>
/// </remarks>
public class RequestHistogramTests
{
    private static RequestHistogram Filled(params double[] samples)
    {
        var h = new RequestHistogram();
        foreach (var s in samples) h.Add(s, failed: false, slow: false);
        return h;
    }

    [Fact]
    public void EmptyHistogram_ReportsZero_InsteadOfThrowing()
    {
        var h = new RequestHistogram();

        h.Count.Should().Be(0);
        h.AverageMs.Should().Be(0);
        h.MaxMs.Should().Be(0);
        h.Percentile(0.95).Should().Be(0, "hiç ölçüm yokken bir yüzdelik uydurulmaz");
    }

    [Fact]
    public void Counters_TrackCallsFailuresAndSlowSeparately()
    {
        var h = new RequestHistogram();
        h.Add(5, failed: false, slow: false);
        h.Add(900, failed: false, slow: true);
        h.Add(12, failed: true, slow: false);

        h.Count.Should().Be(3);
        h.Failures.Should().Be(1);
        h.SlowCount.Should().Be(1, "'yavaş' ile 'hatalı' AYRI sınıflar — yavaş bir istek başarılı olabilir");
        h.MaxMs.Should().Be(900);
    }

    /// <summary>
    /// 🔑 <b>Yaklaşıklığın YÖNÜ sözleşmedir.</b> Bir yüzdelik gerçeğin altını söylerse
    /// ekran sistemi <b>olduğundan hızlı</b> gösterir — ve performans ekranının yapabileceği
    /// en kötü şey budur: yavaşlığı gizleyen bir yavaşlık ölçeri.
    /// </summary>
    [Fact]
    public void Percentile_NeverUnderReports_TheTrueValue()
    {
        // 100 örnek: 95'i 3 ms, 5'i 400 ms. Gerçek p95 = 3 ms sınırında, p99 = 400 ms.
        var h = new RequestHistogram();
        for (var i = 0; i < 95; i++) h.Add(3, false, false);
        for (var i = 0; i < 5; i++) h.Add(400, false, false);

        h.Percentile(0.50).Should().BeGreaterThanOrEqualTo(3);
        h.Percentile(0.99).Should().BeGreaterThanOrEqualTo(400,
            "kuyruktaki 5 örnek p99'a düşmeli — yoksa 'nadiren çok yavaş' hiç görünmez");
    }

    /// <summary>
    /// 🐛 <b>Bu iddia canlı ölçümde doğdu.</b> İlk gerçek tabloda <c>GetNewsQuery</c>
    /// <i>p99 = 100 ms</i> ama <i>en yüksek = 80 ms</i> yazıyordu (kova üst sınırı).
    /// İkisi de "yanlış" değildi ama ekran <b>kendi kendisiyle çelişiyordu</b> — ve
    /// çelişen bir ölçüm ekranına kimse güvenmez.
    /// </summary>
    [Fact]
    public void Percentile_IsNeverGreaterThanTheObservedMaximum()
    {
        // 80 ms, (50,100] kovasına düşer: tavansız hâlde p99 = 100 ms derdi.
        var h = Filled(1, 2, 80);

        h.MaxMs.Should().Be(80);
        h.Percentile(0.99).Should().BeLessThanOrEqualTo(h.MaxMs,
            "bir yüzdelik, gerçekte GÖRÜLEN en büyük değerden büyük olamaz");
        h.Percentile(1.0).Should().BeLessThanOrEqualTo(h.MaxMs);
    }

    /// <summary>
    /// 🔴 <b>Süreçler arası okumanın tamamı bu iddiaya dayanıyor.</b> API ve panel ayrı
    /// süreçlerdir; birleştirme toplamıyorsa panelin gösterdiği tablo <b>eksik</b> olur ve
    /// bunu hiçbir şey söylemez — sayı yalnız "biraz düşük" görünür.
    /// </summary>
    [Fact]
    public void Merge_SumsCountersAndKeepsTheLargerMaximum()
    {
        var a = Filled(5, 5, 5);
        var b = new RequestHistogram();
        b.Add(700, failed: true, slow: true);

        a.Merge(b);

        a.Count.Should().Be(4);
        a.Failures.Should().Be(1);
        a.SlowCount.Should().Be(1);
        a.MaxMs.Should().Be(700, "birleştirmede en büyük değer KAYBOLMAMALI");
        a.AverageMs.Should().BeApproximately((5 + 5 + 5 + 700) / 4d, 0.01,
            "ortalama toplam süreden hesaplanır — kova ortalaması değil");
    }

    [Fact]
    public void Merge_LeavesTheSourceUntouched()
    {
        var a = Filled(1);
        var b = Filled(2, 2);

        a.Merge(b);

        b.Count.Should().Be(2, "birleştirme KAYNAĞI tüketmemeli: aynı süreç sayaçlarını " +
                               "her okumada yeniden birleştiriyor, tüketilseydi ikinci " +
                               "okuma sayıları SIFIRLARDI");
    }

    [Fact]
    public void Serialize_RoundTripsWithoutLoss()
    {
        var original = new RequestHistogram();
        original.Add(3, false, false);
        original.Add(60, true, false);
        original.Add(900, false, true);

        var restored = RequestHistogram.TryParse(original.Serialize());

        restored.Should().NotBeNull();
        restored!.Count.Should().Be(original.Count);
        restored.Failures.Should().Be(original.Failures);
        restored.SlowCount.Should().Be(original.SlowCount);
        restored.MaxMs.Should().Be(original.MaxMs);
        restored.AverageMs.Should().BeApproximately(original.AverageMs, 0.001);
        restored.Percentile(0.95).Should().Be(original.Percentile(0.95));
    }

    /// <summary>
    /// ⚠️ <b>Tanınmayan biçim yok sayılır, TAHMİN EDİLMEZ.</b> Kova sınırları bir gün
    /// değişirse Redis'te farklı uzunlukta serileştirilmiş histogramlar kalır; onları
    /// "elden geldiğince" okumak, bayat sayıları <b>yanlış kovalara</b> dağıtmak demektir —
    /// veriyi kaybetmekten kötüdür.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("saçma")]
    [InlineData("1|2|3|4|5")]                 // eksik alan
    [InlineData("1|2|3|4|5|1,2,3")]           // kova sayısı uymuyor
    [InlineData("a|2|3|4|5|0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0")]  // sayı değil
    public void TryParse_RejectsUnknownShapes_WithoutThrowing(string? raw)
    {
        RequestHistogram.TryParse(raw).Should().BeNull();
    }

    /// <summary>
    /// 📌 Ters yön: geçerli biçim gerçekten kabul edilmeli. Yoksa "hiçbir şeyi kabul etme"
    /// gerçeklemesi de yukarıdaki testleri geçerdi ve ölçüm süreçler arası HİÇ birleşmezdi
    /// (§7 madde 68'in "iki yönlü kilit" dersi).
    /// </summary>
    [Fact]
    public void TryParse_AcceptsWhatSerializeProduces()
    {
        var raw = Filled(7).Serialize();

        RequestHistogram.TryParse(raw).Should().NotBeNull(
            "kendi ürettiğimiz biçim reddedilirse Redis'e yazılan her ölçüm okunurken sessizce düşer");
    }

    /// <summary>
    /// 🔑 Kova sayısı ile serileştirme uzunluğu <b>birbirine bağlı</b> ve bu bağ görünmez:
    /// biri değişip diğeri değişmezse <see cref="RequestHistogram.TryParse"/> her kaydı
    /// reddeder ve panel <b>sessizce boşalır</b> (hata yok, log yok).
    /// </summary>
    [Fact]
    public void SerializedBucketCount_MatchesTheDeclaredBounds()
    {
        var buckets = Filled(1).Serialize().Split('|')[5].Split(',');

        buckets.Should().HaveCount(RequestHistogram.BucketUpperBoundsMs.Length + 1,
            "kova sayısı = sınır sayısı + 1 (taşma kovası)");
    }
}
