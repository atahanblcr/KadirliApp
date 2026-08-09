using FluentAssertions;
using KadirliApp.Domain.Enums;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Transport;

/// <summary>
/// Faz 12.5 — araç tipi metninin saf mantığı (görünmez sözleşme #47).
/// </summary>
/// <remarks>
/// 🔑 İki dönüşüm bilinçli olarak <b>ayrı</b>: kayıt yolunda bilinmeyen değer varsayılana
/// düşer (<see cref="TransportVehicleTypes.Normalize"/>), süzgeç yolunda ise <b>süzmez</b>
/// (<see cref="TransportVehicleTypes.NormalizeFilter"/>). Tek metot olsaydı
/// <c>?vehicleType=otobus</c> yazan bir istemci, tüm listeyi görmesi gerekirken yalnız
/// otobüsleri görürdü — hata vermeyen yanlış liste.
/// </remarks>
public class TransportVehicleTypeTests
{
    [Theory]
    [InlineData("bus", TransportVehicleType.Bus)]
    [InlineData("minibus", TransportVehicleType.Minibus)]
    [InlineData("MINIBUS", TransportVehicleType.Minibus)]
    [InlineData("  minibus  ", TransportVehicleType.Minibus)]
    public void Parse_ReadsKnownValues(string raw, TransportVehicleType expected)
        => TransportVehicleTypes.Parse(raw).Should().Be(expected);

    /// <summary>Bilinmeyen/boş değer <b>otobüs</b>: 12.5 öncesindeki örtük varsayımın kendisi.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("otobus")]
    [InlineData("dolmuş")]
    public void Parse_FallsBackToBus(string? raw)
        => TransportVehicleTypes.Parse(raw).Should().Be(TransportVehicleType.Bus);

    [Fact]
    public void ToValue_RoundTripsWithParse()
    {
        foreach (var value in TransportVehicleTypes.All)
            TransportVehicleTypes.Parse(value).ToValue().Should().Be(value);
    }

    [Fact]
    public void Normalize_WritesTheCanonicalValue()
    {
        TransportVehicleTypes.Normalize("MiniBus").Should().Be(TransportVehicleTypes.Minibus);
        TransportVehicleTypes.Normalize(null).Should().Be(TransportVehicleTypes.Default);
        TransportVehicleTypes.Default.Should().Be(TransportVehicleTypes.Bus,
            "12.5 öncesi satırlar migration'da 'bus' ile göç etti — varsayılan onunla aynı kalmalı");
    }

    /// <summary>
    /// 🔴 Süzgeçte bilinmeyen değer <b>null</b> döner ("süzme yok"): bir yazım hatası listeyi
    /// boşaltmamalı (<c>ARCHITECTURE.md</c> §5, 12.4'te <c>locationScope</c> için verilen karar).
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("otobus", null)]
    [InlineData("bus", "bus")]
    [InlineData("MINIBUS", "minibus")]
    public void NormalizeFilter_OnlyFiltersOnKnownValues(string? raw, string? expected)
        => TransportVehicleTypes.NormalizeFilter(raw).Should().Be(expected);
}
