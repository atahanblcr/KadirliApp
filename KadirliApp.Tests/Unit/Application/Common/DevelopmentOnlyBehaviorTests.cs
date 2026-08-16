using FluentAssertions;
using KadirliApp.Application.Common.Behaviors;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Common;

/// <summary>
/// Faz 12.19a — görünmez sözleşme <b>#78</b>'in <b>saf</b> ayağı: ortam kapısının kendisi.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu kapı, projenin en tehlikeli test sınıfına giriyor: "bayrakla kapalı kod yolu".</b>
/// Geliştirme ortamında <b>hiç çalışmaz</b> (koşul hep sağlanır ve komut geçer), yani
/// gerçekten iş yaptığı ilk an bir yayın anıdır — <c>ProductionReadinessGuard</c>'ın
/// <c>&lt;remarks&gt;</c>'ında yazan uyarının birebir aynısı. Kapının yanlış yazıldığını
/// tam o an öğrenmek istemeyiz.
/// </para>
/// <para>
/// 🔑 <b>İddia İKİ YÖNLÜ ve ikinci yön şart</b> (§7 madde 68/77'nin dersi): yalnız
/// <i>"Production'da reddedilir"</i> denseydi, <b>hiçbir komutu geçirmeyen</b> bir
/// gerçekleme de yeşil kalırdı. <see cref="TheSameCommand_RunsInDevelopment"/> aynı komutun
/// yalnız ortam değiştiği için geçtiğini gösterir → reddin sebebi <b>gerçekten</b> ortamdır.
/// </para>
/// <para>
/// 📌 Üçüncü iddia da ayrı bir hata sınıfını tutuyor: kapı <b>işaretsiz</b> komutlara
/// dokunmamalı. Bir gün <c>request is not IDevelopmentOnlyCommand</c> koşulu ters yazılsa
/// (ya da silinse) uygulama Production'da <b>tamamen ölürdü</b> — o hata gürültülü olurdu,
/// evet, ama bu testin var olma sebebi onu <i>yayından önce</i> duymak.
/// </para>
/// </remarks>
public class DevelopmentOnlyBehaviorTests
{
    private sealed record DevOnlyCommand : IRequest<string>, IDevelopmentOnlyCommand;

    private sealed record OrdinaryCommand : IRequest<string>;

    private sealed class FakeEnvironment : IAppEnvironment
    {
        public FakeEnvironment(string name, bool isDevelopment) => (Name, IsDevelopment) = (name, isDevelopment);
        public string Name { get; }
        public bool IsDevelopment { get; }
    }

    private static Task<string> RunAsync<TRequest>(TRequest request, IAppEnvironment env)
        where TRequest : notnull
    {
        var behavior = new DevelopmentOnlyBehavior<TRequest, string>(
            env, NullLogger<DevelopmentOnlyBehavior<TRequest, string>>.Instance);

        return behavior.Handle(request, _ => Task.FromResult("handler koştu"), CancellationToken.None);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Test")]
    public async Task ADevelopmentOnlyCommand_IsRefusedOutsideDevelopment(string environmentName)
    {
        var act = () => RunAsync(new DevOnlyCommand(), new FakeEnvironment(environmentName, isDevelopment: false));

        // ⚠️ "Staging"/"Test" satırları bilinçli: kapı `!IsProduction()` diye yazılsaydı
        // bu iki ortamda SESSİZCE AÇILIRDI ve kimse fark etmezdi. Kuralın yönü
        // "izin ver" olmak zorunda, "reddet" değil (bkz. IAppEnvironment).
        (await act.Should().ThrowAsync<ForbiddenException>())
            .WithMessage($"*{environmentName}*",
                "hata mesajı hangi ortamda olduğumuzu SÖYLEMELİ — 'neden çalışmadı?' " +
                "sorusunun cevabı başka hiçbir yerde yazmıyor");
    }

    [Fact]
    public async Task TheSameCommand_RunsInDevelopment()
    {
        var result = await RunAsync(new DevOnlyCommand(), new FakeEnvironment("Development", isDevelopment: true));

        result.Should().Be("handler koştu",
            "reddin sebebi GERÇEKTEN ortam olmalı — bu iddia olmadan 'hiçbir komutu " +
            "geçirmeyen' bir gerçekleme de yeşil kalırdı (§7 madde 68'in dersi)");
    }

    [Fact]
    public async Task AnUnmarkedCommand_IsUntouchedInProduction()
    {
        var result = await RunAsync(new OrdinaryCommand(), new FakeEnvironment("Production", isDevelopment: false));

        result.Should().Be("handler koştu",
            "kapı YALNIZ işaretli komutları durdurmalı; koşul ters yazılırsa uygulama " +
            "Production'da tamamen ölür");
    }
}
