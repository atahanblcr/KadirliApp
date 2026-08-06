using FluentAssertions;
using KadirliApp.Application.Common.Security;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Security;

/// <summary>
/// Faz 12.2 — **maskeleme bir güvenlik sınırıdır, kozmetik değil.**
///
/// <c>login_attempts</c> bir güvenlik tablosu ve tam da bu yüzden ham telefon biriktirmeye
/// en uygun yer. Ham saklansaydı tablo kendisi bir sızıntı hedefi olurdu: kayıtlar panelde
/// görülüyor, <b>CSV olarak dışa aktarılıyor</b> ve başarısız denemeler 180 gün duruyor.
///
/// Testlerin iki yönü var ve ikisi de gerekli: (1) hassas kısım <b>gerçekten</b> gizleniyor
/// mu, (2) tanılama <b>hâlâ mümkün</b> mü. İkincisi olmadan maskeleme, tabloyu bir sayaca
/// indirir ve ekranın varlık sebebini yok eder.
/// </summary>
public class LoginIdentifierMaskerTests
{
    [Theory]
    [InlineData("+905001112233")]
    [InlineData("05001112233")]
    [InlineData("+90 500 111 22 33")]
    public void Phone_HidesTheSubscriberPart(string phone)
    {
        var masked = LoginIdentifierMasker.MaskIdentifier(phone);

        masked.Should().Contain(LoginIdentifierMasker.Mask);
        masked.Should().NotBe(phone, "ham numara saklanamaz");
        // Abonenin ayırt edici orta hanesi görünmemeli.
        masked.Should().NotContain("111");
    }

    /// <summary>
    /// 🔑 Maskeleme tanılamayı öldürmemeli: yönetici "hangi numara denendi" sorusuna
    /// yaklaşık da olsa cevap verebilmeli, aksi hâlde tablo bir sayaçtan ibaret kalır.
    /// </summary>
    [Fact]
    public void Phone_KeepsEnoughContextToBeUseful()
    {
        var masked = LoginIdentifierMasker.MaskIdentifier("+905001112233");

        masked.Should().StartWith("+90500", "ülke kodu + operatör tanılama için gerekli");
        masked.Should().EndWith("2233", "son haneler kaydı ayırt etmeye yeter");
    }

    [Fact]
    public void Username_KeepsOnlyTheFirstFewCharacters()
    {
        LoginIdentifierMasker.MaskIdentifier("adminuser").Should().Be("adm" + LoginIdentifierMasker.Mask);
    }

    /// <summary>
    /// Kısa değerde baş harfleri korumak hiçbir şey gizlemez ("ab" → "ab***" ham değerin
    /// tamamıdır). Bu durumda tamamı gizlenir.
    /// </summary>
    [Theory]
    [InlineData("ab")]
    [InlineData("abc")]
    public void ShortValues_AreFullyHidden(string value)
    {
        LoginIdentifierMasker.MaskIdentifier(value).Should().Be(LoginIdentifierMasker.Mask);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyValues_ProduceAStablePlaceholder(string? value)
    {
        // Boş kalırsa kolon NOT NULL olduğu için yazma patlar — ve o patlama giriş
        // akışının içinde, yani en kötü yerde olurdu.
        LoginIdentifierMasker.MaskIdentifier(value).Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 🔴 <b>Determinizm sözleşmesi.</b> Aynı telefon her zaman aynı maskeli değeri
    /// üretmeli: kullanıcı ekranındaki "son giriş denemeleri" kutusu, hatalı OTP
    /// satırlarını (UserId boş) <b>yalnız bu değer üzerinden</b> hesaba bağlıyor.
    /// Maskeleme rastgele/zaman bağımlı olsaydı o satırlar hiçbir hesapla eşleşmezdi.
    /// </summary>
    [Fact]
    public void Masking_IsDeterministic()
    {
        var first = LoginIdentifierMasker.MaskIdentifier("+905001112233");
        var second = LoginIdentifierMasker.MaskIdentifier("+905001112233");

        first.Should().Be(second);
    }

    [Fact]
    public void DifferentPhones_ProduceDifferentMasks()
    {
        LoginIdentifierMasker.MaskIdentifier("+905001112233")
            .Should().NotBe(LoginIdentifierMasker.MaskIdentifier("+905001119999"));
    }

    /// <summary>Baştaki/sondaki boşluk aynı numarayı iki ayrı kimliğe bölmemeli.</summary>
    [Fact]
    public void SurroundingWhitespace_DoesNotChangeTheResult()
    {
        LoginIdentifierMasker.MaskIdentifier("  adminuser  ")
            .Should().Be(LoginIdentifierMasker.MaskIdentifier("adminuser"));
    }
}
