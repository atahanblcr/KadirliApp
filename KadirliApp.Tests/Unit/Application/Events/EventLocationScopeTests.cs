using FluentAssertions;
using KadirliApp.Application.Features.Events;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Events;

/// <summary>
/// Faz 12.4 — konum kapsamının çözümlenmesi (görünmez sözleşme #44).
/// </summary>
/// <remarks>
/// 🔑 <c>onlyLocal</c> ayrı bir süzgeç değil, bu enum'a çevrilen bir kısayoldur. İki ayrı
/// gerçekleme olsaydı "yerel" tanımı iki yerde yaşardı ve biri güncellendiğinde diğeri
/// <b>sessizce</b> eski tanımı uygulamaya devam ederdi.
/// </remarks>
public class EventLocationScopeTests
{
    [Theory]
    [InlineData("local", EventLocationScope.Local)]
    [InlineData("away", EventLocationScope.Away)]
    [InlineData("province", EventLocationScope.Province)]
    [InlineData("nearby", EventLocationScope.Nearby)]
    [InlineData("NEARBY", EventLocationScope.Nearby)]
    [InlineData("  local  ", EventLocationScope.Local)]
    public void Parse_ReadsKnownValues(string raw, EventLocationScope expected)
        => EventLocationScopes.Parse(raw, null).Should().Be(expected);

    /// <summary>
    /// 🔴 <c>ARCHITECTURE.md</c> §5: bilinmeyen değer <b>varsayılana düşer</b>.
    /// 400 dönseydi mağazadaki bir istemcinin yazım hatası listeyi tamamen boşaltırdı.
    /// </summary>
    [Theory]
    [InlineData("kadirli")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("çevre")]
    public void Parse_FallsBackToDefaultForUnknownValues(string? raw)
        => EventLocationScopes.Parse(raw, null).Should().Be(EventLocationScope.All);

    /// <summary>Kısayol enum'a çevrilir — "yerel"in tanımı tek yerde kalır.</summary>
    [Theory]
    [InlineData(true, EventLocationScope.Local)]
    [InlineData(false, EventLocationScope.Away)]
    [InlineData(null, EventLocationScope.All)]
    public void Parse_MapsOnlyLocalShortcut(bool? onlyLocal, EventLocationScope expected)
        => EventLocationScopes.Parse(null, onlyLocal).Should().Be(expected);

    /// <summary>Açık kapsam kısayolu ezer: ikisi birden gelirse belirsizlik kalmamalı.</summary>
    [Fact]
    public void Parse_PrefersExplicitScopeOverShortcut()
        => EventLocationScopes.Parse("nearby", onlyLocal: true).Should().Be(EventLocationScope.Nearby);

    /// <summary>
    /// Ham değer ekrana basılmaz (Değişmez Kural #6) — her kapsamın Türkçe karşılığı olmalı.
    /// </summary>
    [Theory]
    [InlineData(EventLocationScope.All)]
    [InlineData(EventLocationScope.Local)]
    [InlineData(EventLocationScope.Away)]
    [InlineData(EventLocationScope.Province)]
    [InlineData(EventLocationScope.Nearby)]
    public void Label_IsTurkishAndNeverRaw(EventLocationScope scope)
    {
        var label = EventLocationScopes.Label(scope);
        label.Should().NotBeNullOrWhiteSpace();
        label.Should().NotBe(scope.ToString());
        label.Should().NotBe(EventLocationScopes.Value(scope));
    }

    /// <summary>
    /// Değer ↔ enum gidiş-dönüşü bozulmamalı: panel şeridi <c>Value()</c> ile bağlantı üretip
    /// <c>Parse()</c> ile geri okuyor. Ayrışsalardı seçili chip <b>hiçbir zaman</b> seçili görünmezdi.
    /// </summary>
    [Theory]
    [InlineData(EventLocationScope.All)]
    [InlineData(EventLocationScope.Local)]
    [InlineData(EventLocationScope.Away)]
    [InlineData(EventLocationScope.Province)]
    [InlineData(EventLocationScope.Nearby)]
    public void ValueAndParse_RoundTrip(EventLocationScope scope)
        => EventLocationScopes.Parse(EventLocationScopes.Value(scope), null).Should().Be(scope);
}
