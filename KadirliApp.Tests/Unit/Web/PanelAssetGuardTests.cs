extern alias WebPanel;

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

using Guard = WebPanel::KadirliApp.Web.Common.PanelAssetGuard;

namespace KadirliApp.Tests.Unit.Web;

/// <summary>
/// Faz 12.9 — panelin varlık kapısı. Container gerektirmez.
/// </summary>
/// <remarks>
/// 📌 Bu, <c>ProductionReadinessGuardTests</c>'in panel karşılığı ve aynı felsefeyi
/// taşır: <b>yanlış yapılandırılmış bir sistemin çalışmaması, sessizce çalışmasından
/// iyidir.</b> Buradaki arıza sınıfı özellikle sessiz — <c>npm run build</c> koşmadan
/// dağıtım yapılırsa panel açılır, uçlar 200 döner, loglar temizdir ve yalnız
/// <i>görüntü</i> yanlıştır.
/// </remarks>
public class PanelAssetGuardTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "panel-assets-" + Guid.NewGuid().ToString("N"));

    private IWebHostEnvironment Env(string environmentName)
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        env.SetupGet(e => e.WebRootPath).Returns(_root);
        return env.Object;
    }

    private void WriteAllRequiredAssets(long size = 16)
    {
        foreach (var (path, _) in Guard.RequiredAssets)
        {
            var full = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, new byte[size]);
        }
    }

    [Fact]
    public void Production_WithEveryAssetInPlace_Opens()
    {
        WriteAllRequiredAssets();

        var act = () => Guard.Validate(Env(Environments.Production), NullLogger.Instance);

        act.Should().NotThrow();
    }

    /// <summary>
    /// 🔴 Kapının asıl işi. Leaflet eksikse harita seçici <b>10 formda</b> ölür.
    /// </summary>
    [Fact]
    public void Production_WithAMissingAsset_DoesNotOpen()
    {
        WriteAllRequiredAssets();
        File.Delete(Path.Combine(_root, "lib", "leaflet", "leaflet.js"));

        var act = () => Guard.Validate(Env(Environments.Production), NullLogger.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*leaflet.js*")
            .WithMessage("*HARİTA SEÇİCİ*",
                "hata mesajı neyin kırıldığını söylemeli — 'dosya yok' tek başına " +
                "yayın anındaki kişiye hiçbir şey öğretmez");
    }

    /// <summary>
    /// 🐛 <b>Boş dosya tuzağı.</b> Yarıda kesilen bir derleme adımı 0 baytlık bir
    /// <c>panel.css</c> bırakabilir. Yalnız <i>varlığa</i> bakan bir kapı onu geçerli
    /// sayar ve panel <b>tamamen stilsiz</b> açılır — kapı "var" der, kullanıcı
    /// bembeyaz bir sayfa görür.
    /// </summary>
    [Fact]
    public void Production_WithAnEmptyAsset_DoesNotOpen()
    {
        WriteAllRequiredAssets();
        File.WriteAllBytes(Path.Combine(_root, "css", "panel.css"), []);

        var act = () => Guard.Validate(Env(Environments.Production), NullLogger.Instance);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*panel.css*")
            .WithMessage("*BOŞ*");
    }

    /// <summary>
    /// Kapı yalnız Production'da çalışır: geliştirici <c>npm install</c> yapmadan da
    /// paneli açabilmeli (çıktılar depoda olduğu için zaten açabiliyor, ama kapı
    /// bunu varsaymamalı).
    /// </summary>
    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    public void NonProduction_IsNeverBlocked(string environmentName)
    {
        Directory.CreateDirectory(_root); // hiçbir varlık yok

        var act = () => Guard.Validate(Env(environmentName), NullLogger.Instance);

        act.Should().NotThrow();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
