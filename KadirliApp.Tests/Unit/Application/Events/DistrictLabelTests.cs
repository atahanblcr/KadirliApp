using FluentAssertions;
using KadirliApp.Application.Common.Utils;
using KadirliApp.Application.Features.Lookups;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Events;

/// <summary>
/// Faz 12.4 — konum etiketinin <b>saf</b> kuralı (görünmez sözleşme #43).
/// </summary>
/// <remarks>
/// 🔑 Container gerektirmiyor ve gerektirmemeli: bu bir veri sorusu değil, bir <b>sunum
/// kuralı</b> sorusu. Yanlış cevabı hata vermez — panel ile mobil aynı etkinliği farklı
/// yazar ve kimse fark etmez (görünmez sözleşme #23'ün sınıfı).
/// </remarks>
public class DistrictLabelTests
{
    /// <summary>Ev ilçesinde il adı gürültüdür: kullanıcı zaten Kadirli uygulamasında.</summary>
    [Fact]
    public void HomeDistrict_IsLabelledWithItsNameAlone()
        => DistrictLabel.For("Kadirli", "Osmaniye", isCenter: false).Should().Be("Kadirli");

    /// <summary>Kendi ilimizin diğer ilçeleri il adıyla birlikte yazılır — "Merkez" tek başına anlamsız.</summary>
    [Fact]
    public void OtherDistrictOfHomeProvince_IsLabelledWithProvince()
        => DistrictLabel.For("Merkez", "Osmaniye", isCenter: true).Should().Be("Osmaniye / Merkez");

    /// <summary>Başka bir ilin merkezi <b>yalnız il adıyla</b> yazılır: "Adana / Merkez" bilgi taşımaz.</summary>
    [Fact]
    public void CenterOfAnotherProvince_IsLabelledWithProvinceAlone()
        => DistrictLabel.For("Merkez", "Adana", isCenter: true).Should().Be("Adana");

    /// <summary>Başka bir ilin merkez olmayan ilçesi tam yazılır.</summary>
    [Fact]
    public void NonCenterOfAnotherProvince_IsLabelledWithBoth()
        => DistrictLabel.For("Ceyhan", "Adana", isCenter: false).Should().Be("Adana / Ceyhan");

    /// <summary>
    /// 🔴 Görünmez sözleşme #21'in bu fazdaki karşılığı: ev ilçesi/ili karşılaştırması
    /// <see cref="SlugHelper"/> üzerinden yapılır. Ham <c>ToLowerInvariant</c> ile
    /// karşılaştırılsaydı Türkçe <c>'İ'</c> yüzünden yazım farkı olan bir kayıt
    /// "başka il" sayılır ve <b>Kadirli etkinliği çevre il listesine düşerdi</b>.
    /// </summary>
    [Theory]
    [InlineData("KADİRLİ", "OSMANİYE")]
    [InlineData("kadirli", "osmaniye")]
    [InlineData(" Kadirli ", " Osmaniye ")]
    public void HomeDistrict_IsRecognisedRegardlessOfTurkishCasing(string name, string province)
        => DistrictLabel.For(name, province, isCenter: false).Should().Be(name.Trim());

    [Theory]
    [InlineData(null, "Osmaniye")]
    [InlineData("Kadirli", null)]
    [InlineData("", "Osmaniye")]
    [InlineData("   ", "   ")]
    public void MissingParts_ProduceNoLabel(string? name, string? province)
        => DistrictLabel.For(name, province, isCenter: false).Should().BeNull();

    /// <summary>Ev ilçesinin slug'ı türetmenin çıpası — biçimi kazara değişmemeli.</summary>
    [Fact]
    public void HomeSlug_IsDerivedFromProvinceAndDistrict()
        => DistrictDefaults.HomeSlug.Should().Be("osmaniye-kadirli");

    /// <summary>
    /// Her ilin bir "Merkez"i var: slug yalnız ilçe adından üretilseydi ikinci il merkezi
    /// benzersiz indekse takılır ve sözlüğe <b>hiç eklenemezdi</b>.
    /// </summary>
    [Fact]
    public void SlugFor_SeparatesTheCentersOfDifferentProvinces()
        => DistrictDefaults.SlugFor("Adana", "Merkez")
            .Should().NotBe(DistrictDefaults.SlugFor("Osmaniye", "Merkez"));
}
