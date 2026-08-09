using System;
using System.Linq;
using FluentAssertions;
using KadirliApp.Domain.Enums;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Transport;

/// <summary>
/// Faz 12.5 — sefer gün maskesinin saf mantığı (görünmez sözleşme #46).
/// </summary>
/// <remarks>
/// 🔴 Bu dosyanın asıl işi <b>Pazar kaymasını</b> kilitlemek: .NET <see cref="DayOfWeek"/>
/// <c>Pazar=0</c>'dan başlar, bizim maske <b>Pazartesi=1</b>'den. İki yerde ayrı yazılan bir
/// dönüşüm "Salı seferini Pazartesi gösterir" ve <b>kimse hata almaz</b> — bu yüzden dönüşümün
/// tek sahibi <see cref="OperatingDays"/> ve sınırları burada test ediliyor.
/// </remarks>
public class OperatingDaysTests
{
    // ── Bit eşlemesi: Pazar sınırı ──────────────────────────────────────────────

    [Theory]
    [InlineData(DayOfWeek.Monday, 1)]
    [InlineData(DayOfWeek.Tuesday, 2)]
    [InlineData(DayOfWeek.Wednesday, 4)]
    [InlineData(DayOfWeek.Thursday, 8)]
    [InlineData(DayOfWeek.Friday, 16)]
    [InlineData(DayOfWeek.Saturday, 32)]
    [InlineData(DayOfWeek.Sunday, 64)]
    public void BitFor_MapsMondayFirst(DayOfWeek day, int expectedBit)
        => OperatingDays.BitFor(day).Should().Be(expectedBit);

    /// <summary>
    /// 🔴 Kaymanın tam kanıtı: <c>DayOfWeek.Sunday</c> sayısal olarak <b>0</b>'dır. Maske
    /// <c>1 &lt;&lt; (int)day</c> ile üretilseydi Pazar biti 1 olur, yani <b>Pazartesi</b>
    /// ile aynı bite düşerdi — Pazar seferi Pazartesi görünürdü.
    /// </summary>
    [Fact]
    public void Sunday_DoesNotCollideWithMonday()
    {
        ((int)DayOfWeek.Sunday).Should().Be(0, ".NET'te Pazar sıfırdır — kaymanın kaynağı budur");

        OperatingDays.BitFor(DayOfWeek.Sunday)
            .Should().NotBe(OperatingDays.BitFor(DayOfWeek.Monday));

        var sundayOnly = new OperatingDays(OperatingDays.Sunday);
        sundayOnly.Runs(DayOfWeek.Sunday).Should().BeTrue();
        sundayOnly.Runs(DayOfWeek.Monday).Should().BeFalse();
    }

    // ── Geçerlilik: 0 yasak ─────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Hiçbir gün çalışmayan sefer, panelde <i>duran</i> ama mobilde <i>hiç görünmeyen</i>
    /// bir kayıttır: yönetici saati girdiğini sanır, vatandaş asla göremez, kimse hata almaz.
    /// </summary>
    [Fact]
    public void ZeroMask_IsInvalid()
    {
        new OperatingDays(0).IsValid.Should().BeFalse();
        new OperatingDays(1).IsValid.Should().BeTrue();
        new OperatingDays(OperatingDays.Daily).IsValid.Should().BeTrue();
        new OperatingDays(128).IsValid.Should().BeFalse("7 günün dışında bit yok");
        new OperatingDays(-1).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Daily_CoversEveryDayOfTheWeek()
    {
        var all = OperatingDays.All;

        all.Mask.Should().Be(127);
        all.RunsDaily.Should().BeTrue();

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            all.Runs(day).Should().BeTrue($"{day} her gün çalışan seferde olmalı");
    }

    [Fact]
    public void WeekdaysAndWeekend_AreComplementary()
    {
        (OperatingDays.Weekdays | OperatingDays.Weekend).Should().Be(OperatingDays.Daily);
        (OperatingDays.Weekdays & OperatingDays.Weekend).Should().Be(0);

        new OperatingDays(OperatingDays.Weekdays).Runs(DayOfWeek.Saturday).Should().BeFalse();
        new OperatingDays(OperatingDays.Weekend).Runs(DayOfWeek.Sunday).Should().BeTrue();
    }

    // ── DTO kodları: KONTRAT ────────────────────────────────────────────────────

    /// <summary>
    /// ⚠️ Kodlar DTO'ya çıkıyor, yani mağazadaki sürümlerle paylaşılan bir sözleşme.
    /// Değişirlerse eski sürümler günü tanımaz.
    /// </summary>
    [Fact]
    public void Codes_AreTheContractAndStartOnMonday()
    {
        OperatingDays.All.Codes()
            .Should().Equal("mon", "tue", "wed", "thu", "fri", "sat", "sun");

        new OperatingDays(OperatingDays.Monday | OperatingDays.Wednesday | OperatingDays.Friday)
            .Codes().Should().Equal("mon", "wed", "fri");
    }

    [Fact]
    public void FromCodes_RoundTripsWithCodes()
    {
        var original = new OperatingDays(OperatingDays.Tuesday | OperatingDays.Sunday);

        OperatingDays.FromCodes(original.Codes()).Mask.Should().Be(original.Mask);
    }

    /// <summary>Bilinmeyen kod <b>yok sayılır</b> — tek bir yazım hatası maskeyi patlatmamalı.</summary>
    [Fact]
    public void FromCodes_IgnoresUnknownCodesAndIsCaseInsensitive()
    {
        OperatingDays.FromCodes(new[] { "MON", " tue ", "pazartesi", "" }).Mask
            .Should().Be(OperatingDays.Monday | OperatingDays.Tuesday);

        OperatingDays.FromCodes(new[] { "pazartesi" }).IsValid
            .Should().BeFalse("hiçbir kod tanınmazsa maske 0 kalır ve komut reddeder");

        OperatingDays.FromCodes(null).Mask.Should().Be(0);
    }

    [Fact]
    public void FromBits_IgnoresValuesOutsideTheSevenDayRange()
    {
        OperatingDays.FromBits(new[] { 1, 64, 128, 0, 3 }).Mask
            .Should().Be(OperatingDays.Monday | OperatingDays.Sunday,
                "3 tek bir günün biti değil (1|2); yalnız tanımlı bitler kabul edilir");

        OperatingDays.FromBits(null).Mask.Should().Be(0);
    }

    // ── "Sıradaki sefer" (12.6'nın dayanağı) ────────────────────────────────────

    [Fact]
    public void DaysUntilNext_CountsTodayAsZero()
    {
        var weekdays = new OperatingDays(OperatingDays.Weekdays);

        weekdays.DaysUntilNext(DayOfWeek.Wednesday).Should().Be(0, "bugün çalışıyorsa bugündür");
    }

    /// <summary>
    /// Hafta içi seferi <b>Pazar günü</b> sorulduğunda "yarın" (1 gün) demeli. Kaymanın
    /// pratikteki sonucu tam burada görünür: yanlış eşleme "6 gün sonra" derdi.
    /// </summary>
    [Fact]
    public void DaysUntilNext_WrapsAroundTheWeekEnd()
    {
        var weekdays = new OperatingDays(OperatingDays.Weekdays);

        weekdays.DaysUntilNext(DayOfWeek.Saturday).Should().Be(2, "Cumartesi'den Pazartesi'ye iki gün");
        weekdays.DaysUntilNext(DayOfWeek.Sunday).Should().Be(1, "Pazar'dan Pazartesi'ye bir gün");

        new OperatingDays(OperatingDays.Sunday).DaysUntilNext(DayOfWeek.Monday)
            .Should().Be(6, "Pazartesi'den Pazar'a altı gün");
    }

    [Fact]
    public void DaysUntilNext_ReturnsNullWhenNoDayRuns()
        => new OperatingDays(0).DaysUntilNext(DayOfWeek.Monday).Should().BeNull();

    [Fact]
    public void ToDayOfWeeks_ReturnsMondayFirstOrder()
        => OperatingDays.All.ToDayOfWeeks().Should().Equal(
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday);
}
