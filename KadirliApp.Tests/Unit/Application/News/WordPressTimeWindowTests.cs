using FluentAssertions;
using KadirliApp.Application.Features.News;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.12 — <b>görünmez sözleşme #54: <c>modified_after</c> site-yerel saattedir.</b>
///
/// 🔴 Bu, §7 madde 6'daki "TR günü, 00:00 UTC" tuzağının birebir kardeşi ve o sınıf bu
/// projede <b>4 kez</b> tekrarladı. Buradaki testlerin hepsi <b>yönü</b> kilitliyor:
/// pencereyi geniş tutmak zararsız (mükerrer okuma, idempotent upsert), <b>dar tutmak</b>
/// her koşuda 3 saatlik haberi <b>sessizce atlar</b> — ne hata, ne log, ne panelde belirti.
///
/// Kanıt (canlı API, 11 Ağustos 2026):
/// <code>
/// modified_after=2026-08-11T10:11:36 (yerel) -> X-WP-Total: 0
/// modified_after=2026-08-11T07:11:36 (UTC)   -> X-WP-Total: 4
/// </code>
/// </summary>
public class WordPressTimeWindowTests
{
    [Fact]
    public void SiteLocal_IsThreeHoursAheadOfUtc()
    {
        var utc = new DateTime(2026, 8, 11, 7, 11, 36, DateTimeKind.Utc);

        WordPressTimeWindow.ToSiteLocal(utc).Should().Be(new DateTime(2026, 8, 11, 10, 11, 36));
    }

    [Fact]
    public void ToUtc_AndBack_IsALosslessRoundTrip()
    {
        var utc = new DateTime(2026, 3, 1, 23, 45, 0, DateTimeKind.Utc);

        WordPressTimeWindow.ToUtc(WordPressTimeWindow.ToSiteLocal(utc)).Should().Be(utc);
    }

    /// <summary>
    /// 🔴 <b>Yön testi — bu fazın 1 numaralı tuzağı.</b> Sorgu damgası imleçten <b>ileride</b>
    /// olursa aradaki haberler hiç sorulmaz. Bu iddia, "UTC'ye çevireyim" diye yazılmış ters
    /// bir dönüşümü <b>tek başına</b> yakalar.
    /// </summary>
    [Fact]
    public void QueryFloor_IsNeverAheadOfTheCursor_EvenAfterConversion()
    {
        var cursorUtc = new DateTime(2026, 8, 11, 7, 11, 36, DateTimeKind.Utc);

        var floorLocal = WordPressTimeWindow.QueryFloor(cursorUtc);

        // Yerel damganın UTC karşılığı imleçten GERİDE olmalı.
        WordPressTimeWindow.ToUtc(floorLocal).Should().BeBefore(cursorUtc);
    }

    [Fact]
    public void QueryFloor_SubtractsTheDeliberateOverlap()
    {
        var cursorUtc = new DateTime(2026, 8, 11, 7, 0, 0, DateTimeKind.Utc);

        WordPressTimeWindow.QueryFloor(cursorUtc)
            .Should().Be(new DateTime(2026, 8, 11, 9, 30, 0),
                "imleç 30 dk geriye alınır (çakışma payı), SONRA yerele çevrilir");
    }

    /// <summary>Biçim WordPress'in beklediği gibi olmalı: saat dilimi eki <b>yok</b>.</summary>
    [Fact]
    public void Format_HasNoTimezoneSuffix()
    {
        var formatted = WordPressTimeWindow.Format(new DateTime(2026, 8, 11, 10, 11, 36));

        formatted.Should().Be("2026-08-11T10:11:36");
        formatted.Should().NotContain("Z").And.NotContain("+");
    }

    [Fact]
    public void ModifiedAfterParameter_CombinesOverlapAndConversion()
    {
        var cursorUtc = new DateTime(2026, 8, 11, 7, 11, 36, DateTimeKind.Utc);

        WordPressTimeWindow.ModifiedAfterParameter(cursorUtc).Should().Be("2026-08-11T09:41:36");
    }

    /// <summary>
    /// <c>_fields</c>'tan <c>*_gmt</c> düşerse damgalar 3 saat ileri kayar ve haberler
    /// <b>gelecekten</b> görünür — yedek yol o hatayı yumuşatır ama sessizce değil:
    /// bu test iki yolun aynı sonucu verdiğini kilitler.
    /// </summary>
    [Fact]
    public void NormalizeToUtc_PrefersGmt_ButFallsBackToLocal()
    {
        var gmt = new DateTime(2026, 8, 11, 7, 11, 36);
        var local = new DateTime(2026, 8, 11, 10, 11, 36);

        WordPressTimeWindow.NormalizeToUtc(gmt, local).Should().Be(DateTime.SpecifyKind(gmt, DateTimeKind.Utc));
        WordPressTimeWindow.NormalizeToUtc(null, local).Should().Be(DateTime.SpecifyKind(gmt, DateTimeKind.Utc));
    }
}
