using FluentAssertions;
using KadirliApp.Application;
using KadirliApp.Application.Common.Behaviors;
using KadirliApp.Application.Common.Performance;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Performance;

/// <summary>
/// Faz 12.22a — görünmez sözleşme <b>#83</b>'ün <b>davranış</b> ayağı: ölçüm halkasının kendisi.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>En önemli iddia sıra hakkında ve sebebi ölçümün yalan söyleme biçimi.</b> Halka
/// <c>CachingBehavior</c>'ın <b>dışında</b> durmak zorunda: ölçülmek istenen şey
/// <i>handler ne kadar sürdü</i> değil, <b>çağıran ne kadar bekledi</b>. Cache HIT'te
/// handler hiç koşmaz ama bekleyen yine bekler — halka cache'in içine konsaydı sıcak
/// uçların p95'i <b>sistematik olarak iyi</b> görünürdü ve bunu hiçbir şey söylemezdi.
/// </para>
/// <para>
/// ⚠️ Sıranın ilk halkası <b>kasıtlı olarak bu değil</b>: birinci sıra
/// <c>DevelopmentOnlyBehavior</c>'ındır (<c>DevelopmentOnlyCommandTests</c>). İki iddia
/// birbirini tamamlıyor — biri "kapı en başta", diğeri "ölçüm cache'ten önce".
/// </para>
/// </remarks>
public class PerformanceBehaviorTests
{
    private sealed record Ping : IRequest<string>;

    private sealed class RecordingMetrics : IRequestMetricsRecorder
    {
        public List<(string Handler, double ElapsedMs, bool Failed, bool Slow)> Records { get; } = new();

        public void Record(string handler, double elapsedMs, bool failed, bool slow)
            => Records.Add((handler, elapsedMs, failed, slow));
    }

    private sealed class ThrowingMetrics : IRequestMetricsRecorder
    {
        public void Record(string handler, double elapsedMs, bool failed, bool slow)
            => throw new InvalidOperationException("ölçüm deposu bozuk");
    }

    private static PerformanceBehavior<Ping, string> Behavior(
        IRequestMetricsRecorder metrics, int thresholdMs = 500, bool enabled = true)
        => new(metrics,
               Options.Create(new PerformanceSettings { SlowRequestThresholdMs = thresholdMs, Enabled = enabled }),
               NullLogger<PerformanceBehavior<Ping, string>>.Instance);

    [Fact]
    public async Task Records_EveryRequest_WithItsRequestTypeName()
    {
        var metrics = new RecordingMetrics();

        var result = await Behavior(metrics).Handle(new Ping(), _ => Task.FromResult("ok"), default);

        result.Should().Be("ok");
        metrics.Records.Should().ContainSingle();
        metrics.Records[0].Handler.Should().Be(nameof(Ping),
            "kapsam TİPTEN türer — elle tutulan bir liste olsaydı yarınki handler ölçülmezdi");
        metrics.Records[0].Failed.Should().BeFalse();
    }

    /// <summary>
    /// 🔴 Bir gözlem halkası davranışı değiştirirse gözlem olmaktan çıkar: istisna
    /// <b>yutulmaz</b>, yalnız "başarısız" olarak işaretlenir ve yeniden fırlatılır.
    /// </summary>
    [Fact]
    public async Task Rethrows_TheHandlerException_ButStillRecordsIt()
    {
        var metrics = new RecordingMetrics();

        var act = () => Behavior(metrics).Handle(
            new Ping(),
            _ => throw new InvalidOperationException("patladı"),
            default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("patladı");
        metrics.Records.Should().ContainSingle();
        metrics.Records[0].Failed.Should().BeTrue(
            "istisnayla biten istek de bir gecikmedir — ölçümden düşerse hata anındaki " +
            "yavaşlama tam da bakılacak yerde GÖRÜNMEZ olur");
    }

    /// <summary>
    /// ⚠️ Ölçüm, ölçtüğü uygulamayı düşürmemeli. Depo patlarsa istek <b>normal döner</b>.
    /// </summary>
    [Fact]
    public async Task AFailingMetricsStore_DoesNotBreakTheRequest()
    {
        var result = await Behavior(new ThrowingMetrics()).Handle(new Ping(), _ => Task.FromResult("ok"), default);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Disabled_SkipsMeasurementEntirely()
    {
        var metrics = new RecordingMetrics();

        await Behavior(metrics, enabled: false).Handle(new Ping(), _ => Task.FromResult("ok"), default);

        metrics.Records.Should().BeEmpty("kapatma yolu gerçekten kapatmalı — yoksa bayrak bir yalandır");
    }

    /// <summary>
    /// 🔑 <b>İki yönlü:</b> yalnız "eşiği aşan yavaş sayılır" denseydi, <b>her isteği</b>
    /// yavaş işaretleyen bir gerçekleme de yeşil kalırdı — ve o hâlde Seq her istekte bir
    /// <c>Warning</c> alır, gerçek uyarı o çöplükte kaybolurdu (§7 madde 36'nın dersi).
    /// </summary>
    [Fact]
    public async Task SlowFlag_IsSetOnlyAboveTheThreshold()
    {
        var fast = new RecordingMetrics();
        await Behavior(fast, thresholdMs: 10_000).Handle(new Ping(), _ => Task.FromResult("ok"), default);
        fast.Records[0].Slow.Should().BeFalse("eşiğin çok altındaki istek yavaş DEĞİLDİR");

        var slow = new RecordingMetrics();
        await Behavior(slow, thresholdMs: 0).Handle(new Ping(), _ => Task.FromResult("ok"), default);
        slow.Records[0].Slow.Should().BeTrue("eşik 0 ise her istek eşiği aşar — kapı gerçekten eşiğe bakmalı");
    }

    // ── Boru hattındaki YERİ ───────────────────────────────────────────────────

    private static List<Type?> PipelineBehaviors()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        return services
            .Where(d => d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();
    }

    [Fact]
    public void ThePipeline_RegistersTheMeasurementRing()
    {
        PipelineBehaviors().Should().Contain(typeof(PerformanceBehavior<,>),
            "kayıt düşerse HİÇBİR handler ölçülmez ve panel boş bir tablo gösterir — " +
            "boş tablo 'hiç istek gelmedi' ile 'ölçüm yok'u ayırt edemez");
    }

    /// <summary>
    /// 🔴 <b>Bu testin kilitlediği şey bir sıra değil, bir SAYININ DOĞRULUĞU.</b>
    /// </summary>
    [Fact]
    public void Measurement_WrapsTheCache_NotTheOtherWayAround()
    {
        var behaviors = PipelineBehaviors();

        var performance = behaviors.IndexOf(typeof(PerformanceBehavior<,>));
        var caching = behaviors.IndexOf(typeof(CachingBehavior<,>));

        performance.Should().BeGreaterThanOrEqualTo(0);
        caching.Should().BeGreaterThanOrEqualTo(0);
        performance.Should().BeLessThan(caching,
            "ölçüm CACHE'İ SARMALI. İçine konsaydı cache HIT'lerinde handler hiç koşmadığı " +
            "için sıcak uçların p95'i sistematik olarak İYİ görünürdü — oysa çağıran yine " +
            "bekliyor. Bugünkü sıra: {0}",
            string.Join(" → ", behaviors.Select(b => b!.Name)));
    }

    /// <summary>
    /// ⚠️ Ölçüm halkası boru hattının <b>birinci</b> halkası olamaz: o yer ortam kapısınındır
    /// (<c>DevelopmentOnlyCommandTests.TheGuard_RunsBeforeEveryOtherBehavior</c>). İki iddia
    /// çakışırsa biri diğerini <b>sessizce</b> geçersiz kılar — bu test o çakışmayı yazıya döker.
    /// </summary>
    [Fact]
    public void Measurement_RunsAfterTheEnvironmentGuard()
    {
        var behaviors = PipelineBehaviors();

        behaviors.IndexOf(typeof(PerformanceBehavior<,>))
            .Should().Be(behaviors.IndexOf(typeof(DevelopmentOnlyBehavior<,>)) + 1,
                "ölçüm ortam kapısının HEMEN ardından gelmeli: kapı en başta kalmalı, " +
                "ölçüm ise geri kalan her şeyi sarmalı");
    }
}
