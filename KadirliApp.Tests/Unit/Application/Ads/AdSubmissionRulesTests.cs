using System.Globalization;
using FluentAssertions;
using FluentValidation;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Ads;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Moq;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Ads;

/// <summary>
/// Faz 11.14 — İlan gönderim kurallarının **birim** testleri. Bu kurallar `CreateAd` ve
/// `UpdateMyAd`'in ortak kalbi: bozulursa mobilin ilan verme sihirbazı (11.9) sessizce
/// yanlış veri yazar ya da geçerli ilanı reddeder. Entegrasyon testleri (AdsMobileTests)
/// yalnız birkaç örneği uçtan geçiriyordu; burada kuralın **kendisi** kilitleniyor.
///
/// ⚠️ Sınıf `internal` — test projesine `InternalsVisibleTo` ile açıldı (Application.csproj).
/// </summary>
public class AdSubmissionRulesTests
{
    // ---------------------------------------------------------------- ValidateContent

    [Theory]
    [InlineData("", "geçerli açıklama")]                 // boş başlık
    [InlineData("   ", "geçerli açıklama")]              // yalnız boşluk
    [InlineData("ab", "geçerli açıklama")]               // 3 karakterden kısa
    public void ValidateContent_RejectsBadTitle(string title, string description)
    {
        var act = () => AdSubmissionRules.ValidateContent(title, description, 100, "+905331112233", isUserSubmission: true);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ValidateContent_RejectsTitleLongerThan200()
    {
        var act = () => AdSubmissionRules.ValidateContent(new string('a', 201), "açıklama", 1, "05331112233", true);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ValidateContent_AcceptsTitleOfExactly200()
    {
        var act = () => AdSubmissionRules.ValidateContent(new string('a', 200), "açıklama", 1, "05331112233", true);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateContent_RequiresDescription_AndCapsAt5000()
    {
        var empty = () => AdSubmissionRules.ValidateContent("Başlık", "  ", 1, "05331112233", true);
        empty.Should().Throw<ValidationException>();

        var tooLong = () => AdSubmissionRules.ValidateContent("Başlık", new string('a', 5001), 1, "05331112233", true);
        tooLong.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ValidateContent_RejectsNegativePrice_ButAllowsZeroAndNull()
    {
        var negative = () => AdSubmissionRules.ValidateContent("Başlık", "açıklama", -1, "05331112233", true);
        negative.Should().Throw<ValidationException>();

        // 0 ve null geçerli: "fiyat belirtilmemiş" ilan mobilde "Fiyat belirtilmemiş" olarak gösteriliyor.
        var zero = () => AdSubmissionRules.ValidateContent("Başlık", "açıklama", 0, "05331112233", true);
        zero.Should().NotThrow();
        var missing = () => AdSubmissionRules.ValidateContent("Başlık", "açıklama", null, "05331112233", true);
        missing.Should().NotThrow();
    }

    [Theory]
    [InlineData("05331112233")]
    [InlineData("+905331112233")]
    [InlineData("5331112233")]
    [InlineData("  05331112233  ")] // baştaki/sondaki boşluk kırpılır
    public void ValidateContent_AcceptsValidMobileFormats(string phone)
    {
        var act = () => AdSubmissionRules.ValidateContent("Başlık", "açıklama", 1, phone, isUserSubmission: true);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("03281112233")]     // sabit hat (5 ile başlamıyor)
    [InlineData("0533111223")]      // eksik hane
    [InlineData("053311122334")]    // fazla hane
    [InlineData("+15551112233")]    // yabancı ülke kodu
    public void ValidateContent_RejectsNonMobilePhones_ForUserSubmissions(string phone)
    {
        var act = () => AdSubmissionRules.ValidateContent("Başlık", "açıklama", 1, phone, isUserSubmission: true);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void ValidateContent_AdminSubmission_SkipsMobileFormatCheck_ButStillRequiresPhone()
    {
        // Panelden girilen ilanda sabit hat yazılabilmeli (esnaf ilanı) —
        // ama telefon alanı yine de boş bırakılamaz.
        var landline = () => AdSubmissionRules.ValidateContent("Başlık", "açıklama", 1, "03281112233", isUserSubmission: false);
        landline.Should().NotThrow();

        var empty = () => AdSubmissionRules.ValidateContent("Başlık", "açıklama", 1, "  ", isUserSubmission: false);
        empty.Should().Throw<ValidationException>();
    }

    // ------------------------------------------------- ValidatePropertyValuesAsync

    private static readonly Guid CategoryId = Guid.NewGuid();

    private static CategoryProperty Prop(string name, PropertyType type, bool required, params string[] options)
    {
        var id = Guid.NewGuid();
        return new CategoryProperty
        {
            Id = id,
            CategoryId = CategoryId,
            PropertyName = name,
            PropertyType = type,
            IsRequired = required,
            Options = options.Select(o => new PropertyOption { Id = Guid.NewGuid(), PropertyId = id, OptionValue = o }).ToList()
        };
    }

    private static IUnitOfWork UowWith(params CategoryProperty[] properties)
    {
        var repo = new Mock<IRepository<CategoryProperty>>();
        repo.Setup(r => r.Query(It.IsAny<bool>())).Returns(properties.AsAsyncQueryable());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<CategoryProperty>()).Returns(repo.Object);
        return uow.Object;
    }

    private static Task<List<(Guid PropertyId, string Value)>> Validate(
        IUnitOfWork uow, Dictionary<Guid, string>? values, bool isUserSubmission = true) =>
        AdSubmissionRules.ValidatePropertyValuesAsync(uow, CategoryId, values, isUserSubmission, CancellationToken.None);

    [Fact]
    public async Task PropertyValues_MissingRequired_ThrowsForUserSubmission_ButNotForAdmin()
    {
        var required = Prop("Model Yılı", PropertyType.Number, required: true);
        var uow = UowWith(required);

        var user = async () => await Validate(uow, values: null, isUserSubmission: true);
        (await user.Should().ThrowAsync<ValidationException>())
            .WithMessage("*Model Yılı*", "eksik alanın adı kullanıcıya söylenmeli");

        // Panelin property arayüzü yok → admin akışı zorunlu alan denetimine takılmamalı.
        var admin = async () => await Validate(uow, values: null, isUserSubmission: false);
        await admin.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PropertyValues_WhitespaceOnlyValue_CountsAsMissing()
    {
        var required = Prop("Model Yılı", PropertyType.Number, required: true);
        var uow = UowWith(required);

        var act = async () => await Validate(uow, new Dictionary<Guid, string> { [required.Id] = "   " });
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PropertyValues_PropertyFromAnotherCategory_IsRejected()
    {
        var own = Prop("Renk", PropertyType.Text, required: false);
        var uow = UowWith(own);

        var act = async () => await Validate(uow, new Dictionary<Guid, string> { [Guid.NewGuid()] = "Kırmızı" });
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*bu kategoriye ait değil*");
    }

    [Theory]
    [InlineData("2020")]
    [InlineData("2020.5")]   // InvariantCulture: ondalık AYIRICI NOKTA
    [InlineData("-3")]
    public async Task PropertyValues_Number_AcceptsInvariantCultureValues(string value)
    {
        var number = Prop("Kilometre", PropertyType.Number, required: false);
        var uow = UowWith(number);

        var act = async () => await Validate(uow, new Dictionary<Guid, string> { [number.Id] = value });
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// 🐛 Faz 11.14'te bu test yazılırken bulunan gerçek hata: kural `NumberStyles.Number`
    /// kullanıyordu; o stil `AllowThousands` içerir ve .NET grup boyutlarını denetlemez →
    /// Türkçe ondalık gösterimi olan <c>"2020,5"</c> doğrulamadan GEÇİYOR, sayı olarak
    /// okunduğunda <c>20205</c> çıkıyordu (10 kat sapma, hiçbir uyarı yok).
    /// Kural sıkılaştırıldı: binlik ayracı hiç kabul edilmiyor, ondalık ayracı yalnız nokta.
    /// </summary>
    [Theory]
    [InlineData("2020,5")]   // Türkçe ondalık virgülü → 20205 olarak sızıyordu
    [InlineData("1,000")]    // binlik ayracı: Türk kullanıcı için 1,0 mı 1000 mi belirsiz
    [InlineData("1 000")]    // boşluklu binlik
    public async Task PropertyValues_Number_RejectsAmbiguousThousandSeparators(string value)
    {
        var number = Prop("Kilometre", PropertyType.Number, required: false);
        var uow = UowWith(number);

        var act = async () => await Validate(uow, new Dictionary<Guid, string> { [number.Id] = value });
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*sayısal*");
    }

    [Fact]
    public async Task PropertyValues_Number_IsCultureIndependent()
    {
        // Makinenin kültürü tr-TR olsa bile ayraç yorumu değişmemeli (sunucu Türkiye'de koşuyor).
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var number = Prop("Kilometre", PropertyType.Number, required: false);
            var uow = UowWith(number);

            var dot = async () => await Validate(uow, new Dictionary<Guid, string> { [number.Id] = "2020.5" });
            await dot.Should().NotThrowAsync();

            var comma = async () => await Validate(uow, new Dictionary<Guid, string> { [number.Id] = "2020,5" });
            await comma.Should().ThrowAsync<ValidationException>();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task PropertyValues_Number_RejectsNonNumericText()
    {
        var number = Prop("Kilometre", PropertyType.Number, required: false);
        var uow = UowWith(number);

        var act = async () => await Validate(uow, new Dictionary<Guid, string> { [number.Id] = "çok" });
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("True")]
    public async Task PropertyValues_Boolean_AcceptsBoolLiterals(string value)
    {
        var boolean = Prop("Hasarlı mı?", PropertyType.Boolean, required: false);
        var uow = UowWith(boolean);

        var act = async () => await Validate(uow, new Dictionary<Guid, string> { [boolean.Id] = value });
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("evet")]
    [InlineData("1")]
    [InlineData("Hayır")]
    public async Task PropertyValues_Boolean_RejectsTurkishAndNumericAnswers(string value)
    {
        // 📌 Görünmez sözleşme: boolean değerler metin olarak taşınıyor ama
        // yalnız "true"/"false" kabul ediliyor — "evet"/"1" DEĞİL.
        var boolean = Prop("Hasarlı mı?", PropertyType.Boolean, required: false);
        var uow = UowWith(boolean);

        var act = async () => await Validate(uow, new Dictionary<Guid, string> { [boolean.Id] = value });
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*true/false*");
    }

    [Fact]
    public async Task PropertyValues_Select_IsValidatedAgainstOptionText_CaseSensitively()
    {
        // 📌 Görünmez sözleşme: select değeri seçenek **kimliğiyle** değil, seçenek
        // **metniyle** doğrulanıyor ve karşılaştırma harf duyarlı.
        var select = Prop("Yakıt Tipi", PropertyType.Select, required: false, "Benzin", "Dizel", "LPG");
        var uow = UowWith(select);

        var valid = async () => await Validate(uow, new Dictionary<Guid, string> { [select.Id] = "Dizel" });
        await valid.Should().NotThrowAsync();

        var wrongCase = async () => await Validate(uow, new Dictionary<Guid, string> { [select.Id] = "dizel" });
        await wrongCase.Should().ThrowAsync<ValidationException>();

        var unknown = async () => await Validate(uow, new Dictionary<Guid, string> { [select.Id] = "Nükleer" });
        (await unknown.Should().ThrowAsync<ValidationException>()).WithMessage("*geçersiz seçenek*");
    }

    [Fact]
    public async Task PropertyValues_MultiSelect_AcceptsCsv_TrimsEntries_AndRejectsAnyUnknownMember()
    {
        var multi = Prop("Donanım", PropertyType.MultiSelect, required: false, "ABS", "Klima", "Sunroof");
        var uow = UowWith(multi);

        var valid = async () => await Validate(uow, new Dictionary<Guid, string> { [multi.Id] = "ABS, Klima" });
        await valid.Should().NotThrowAsync("virgül sonrası boşluklar kırpılmalı");

        // Tek geçersiz üye tüm değeri reddeder (sessizce elenmez).
        var partly = async () => await Validate(uow, new Dictionary<Guid, string> { [multi.Id] = "ABS,Turbo" });
        await partly.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PropertyValues_Text_IsCappedAt500Characters()
    {
        var text = Prop("Not", PropertyType.Text, required: false);
        var uow = UowWith(text);

        var ok = async () => await Validate(uow, new Dictionary<Guid, string> { [text.Id] = new string('a', 500) });
        await ok.Should().NotThrowAsync();

        var tooLong = async () => await Validate(uow, new Dictionary<Guid, string> { [text.Id] = new string('a', 501) });
        await tooLong.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task PropertyValues_ReturnsTrimmedValues_ForPersistence()
    {
        var text = Prop("Renk", PropertyType.Text, required: false);
        var uow = UowWith(text);

        var result = await Validate(uow, new Dictionary<Guid, string> { [text.Id] = "  Kırmızı  " });

        result.Should().ContainSingle();
        result[0].PropertyId.Should().Be(text.Id);
        result[0].Value.Should().Be("Kırmızı", "değer kırpılmadan kaydedilirse mobilde başında boşlukla görünür");
    }

    [Fact]
    public async Task PropertyValues_OptionalPropertiesMayBeOmitted()
    {
        var uow = UowWith(
            Prop("Renk", PropertyType.Text, required: false),
            Prop("Model Yılı", PropertyType.Number, required: true));

        var only = await Validate(uow, new Dictionary<Guid, string>
        {
            [uow.Repository<CategoryProperty>().Query().First(p => p.PropertyName == "Model Yılı").Id] = "2020"
        });

        only.Should().ContainSingle("gönderilmeyen opsiyonel alan sonuçta yer almamalı");
    }

    // -------------------------------------------- ValidateImageOwnershipAsync

    private static IUnitOfWork UowWithFiles(params Domain.Entities.File[] files)
    {
        var repo = new Mock<IRepository<Domain.Entities.File>>();
        repo.Setup(r => r.Query(It.IsAny<bool>())).Returns(files.AsAsyncQueryable());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Domain.Entities.File>()).Returns(repo.Object);
        return uow.Object;
    }

    [Fact]
    public async Task ImageOwnership_EmptyList_SkipsQueryEntirely()
    {
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict).Object; // hiçbir çağrı beklenmiyor
        var act = async () => await AdSubmissionRules.ValidateImageOwnershipAsync(uow, Array.Empty<Guid>(), Guid.NewGuid(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ImageOwnership_AcceptsOwnFiles_AndRejectsForeignOrDeletedOnes()
    {
        var owner = Guid.NewGuid();
        var mine = new Domain.Entities.File { Id = Guid.NewGuid(), UploadedBy = owner };
        var foreign = new Domain.Entities.File { Id = Guid.NewGuid(), UploadedBy = Guid.NewGuid() };
        var deleted = new Domain.Entities.File { Id = Guid.NewGuid(), UploadedBy = owner, DeletedAt = DateTime.UtcNow };
        var uow = UowWithFiles(mine, foreign, deleted);

        var ok = async () => await AdSubmissionRules.ValidateImageOwnershipAsync(uow, new[] { mine.Id }, owner, CancellationToken.None);
        await ok.Should().NotThrowAsync();

        // Başkasının dosyası → 400 (kimliği bilinen dosya başkasının ilanına bağlanamaz).
        var stolen = async () => await AdSubmissionRules.ValidateImageOwnershipAsync(uow, new[] { foreign.Id }, owner, CancellationToken.None);
        await stolen.Should().ThrowAsync<ValidationException>();

        // Silinmiş dosya → 400.
        var gone = async () => await AdSubmissionRules.ValidateImageOwnershipAsync(uow, new[] { deleted.Id }, owner, CancellationToken.None);
        await gone.Should().ThrowAsync<ValidationException>();

        // Karışık liste: biri bile geçersizse tamamı reddedilir.
        var mixed = async () => await AdSubmissionRules.ValidateImageOwnershipAsync(uow, new[] { mine.Id, foreign.Id }, owner, CancellationToken.None);
        await mixed.Should().ThrowAsync<ValidationException>();
    }
}
