extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Application.Features.Events.Queries;
using KadirliApp.Application.Features.Lookups;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.4 — <b>etkinlik konumu: il / ilçe.</b>
///
/// İddia "form açılıyor" değil; üç yeni görünmez sözleşmenin gerçekten tutması:
/// <list type="number">
///   <item><b>#43</b> — <c>locationLabel</c> <b>sunucuda tek yerde</b> üretilir ve liste ile
///         detay <b>aynı</b> projeksiyondan geçer. Ayrışsalardı detay ekranı sessizce
///         konumsuz kalırdı.</item>
///   <item><b>#44</b> — <c>Event.IsLocal</c> <b>türetilmiştir</b>: yazma anında
///         <c>DistrictId</c>'den hesaplanır, formdan gelen değere güvenilmez. Ayrışsaydı
///         kayıt "ilçesi Kadirli ama IsLocal=false" hâline düşer ve mobilin "Kadirli"
///         süzgeci onu <b>hiç göstermezdi</b>.</item>
///   <item><b>#45</b> — ilçe <b>zorunludur</b>; boş ilçe geri doldurmanın "ilçesi yoksa
///         12.4 öncesinden kalmadır" varsayımını çürütürdü.</item>
/// </list>
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelEventDistrictTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    private const string Marker = "CLAUDE-EVENT-DISTRICT";

    private Guid _kadirli;
    private Guid _osmaniyeMerkez;
    private Guid _adana;
    private Guid _categoryId;

    public PanelEventDistrictTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();

            // Sözlük seed'den gelir; testin kendi satırını kurması gerekmiyor çünkü
            // il/ilçe listesi uygulamanın SABİT verisi (mahalleden farkı bu).
            _kadirli = (await db.Districts.FirstAsync(d => d.Slug == DistrictDefaults.HomeSlug)).Id;
            _osmaniyeMerkez = (await db.Districts.FirstAsync(d => d.Slug == "osmaniye-merkez")).Id;
            _adana = (await db.Districts.FirstAsync(d => d.Slug == "adana-merkez")).Id;

            _categoryId = (await db.EventCategories.OrderBy(c => c.Name).FirstAsync()).Id;
        });

        await CleanAsync();
    }

    public Task DisposeAsync() => CleanAsync();

    private Task CleanAsync() => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Events.IgnoreQueryFilters().Where(e => e.Title.Contains(Marker)).ExecuteDeleteAsync();
    });

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await action(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp => result = await sp.GetRequiredService<ISender>().Send(request));
        return result;
    }

    /// <summary>Paneli gerçekten POST'lar — komut yolu uçtan uca koşsun diye.</summary>
    private static Dictionary<string, string> Form(string title, Guid categoryId, Guid? districtId) =>
        new()
        {
            ["Title"] = title,
            ["Description"] = "Faz 12.4 testi",
            ["CategoryId"] = categoryId.ToString(),
            ["EventDate"] = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            ["EventTime"] = "19:00",
            ["VenueName"] = "Kültür Merkezi",
            ["DistrictId"] = districtId?.ToString() ?? string.Empty,
            // ⚠️ Form BİLEREK IsLocal=true gönderiyor: türetmenin formdan gelen değeri
            // yok saydığını ancak böyle kanıtlayabiliriz.
            ["IsLocal"] = "true"
        };

    private Task<Event> EventAsync(string title) =>
        InDbAsync(db => db.Events.AsNoTracking().FirstAsync(e => e.Title == title));

    // ────────────────────────────────────────────────────────────────────────
    // #44 — IsLocal TÜRETİLMİŞTİR
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Form "yerelim" dese bile, ilçe Adana ise kayıt <b>yerel değildir</b>.
    /// Formdan gelen değere güvenilseydi mobilin "Kadirli" süzgeci Adana etkinliğini
    /// listeler ve kimse hata almazdı.
    /// </summary>
    [Fact]
    public async Task IsLocal_IsDerivedFromTheDistrict_NotFromTheForm()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} Adana";

        var response = await admin.PostFormAsync("/EventsAdmin/Create", Form(title, _categoryId, _adana));
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var created = await EventAsync(title);
        created.DistrictId.Should().Be(_adana);
        created.IsLocal.Should().BeFalse("ilçe Adana — formdaki IsLocal=true yok sayılmalı");
    }

    [Fact]
    public async Task IsLocal_IsTrue_ForTheHomeDistrict()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} Kadirli";

        await admin.PostFormAsync("/EventsAdmin/Create", Form(title, _categoryId, _kadirli));

        (await EventAsync(title)).IsLocal.Should().BeTrue();
    }

    /// <summary>
    /// 🔴 Güncelleme <b>aynı kuraldan</b> geçer. İkinci bir gerçekleme yazılsaydı, ilçesi
    /// Kadirli'den Adana'ya çekilen bir etkinlik <c>IsLocal=true</c> kalır ve iki taraf
    /// farklı gerçeklik görürdü (görünmez sözleşme #23'ün sınıfı).
    /// </summary>
    [Fact]
    public async Task Update_RederivesIsLocal_WhenTheDistrictChanges()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} Tasindi";

        await admin.PostFormAsync("/EventsAdmin/Create", Form(title, _categoryId, _kadirli));
        var created = await EventAsync(title);
        created.IsLocal.Should().BeTrue();

        var edit = Form(title, _categoryId, _adana);
        edit["Id"] = created.Id.ToString();
        edit["Status"] = created.Status;
        await admin.PostFormAsync("/EventsAdmin/Edit", edit, tokenFromPath: $"/EventsAdmin/Edit/{created.Id}");

        var updated = await EventAsync(title);
        updated.DistrictId.Should().Be(_adana);
        updated.IsLocal.Should().BeFalse("ilçe değişti — türetilmiş alan da değişmeli");
    }

    // ────────────────────────────────────────────────────────────────────────
    // #45 — ilçe ZORUNLUDUR
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// İlçesiz kayıt kabul edilseydi <c>EventDistrictBackfill</c>'in "ilçesi boş kayıt =
    /// 12.4 öncesinden kalma" varsayımı çürür ve yöneticinin bilerek boş bıraktığı kayıt
    /// bir sonraki açılışta <b>sessizce Kadirli</b> olurdu.
    /// </summary>
    [Fact]
    public async Task Create_IsRejected_WhenTheDistrictIsMissing()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} Ilcesiz";

        var response = await admin.PostFormAsync("/EventsAdmin/Create", Form(title, _categoryId, null));

        // Redirect DEĞİL: form hatasıyla geri dönmeli.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await InDbAsync(db => db.Events.IgnoreQueryFilters().AnyAsync(e => e.Title == title)))
            .Should().BeFalse("ilçesiz etkinlik yazılmamalı");
    }

    /// <summary>Sözlükte olmayan bir kimlik de reddedilir — FK hatasıyla 500 değil, Türkçe mesajla.</summary>
    [Fact]
    public async Task Create_IsRejected_WhenTheDistrictDoesNotExist()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} Yok";

        var response = await admin.PostFormAsync("/EventsAdmin/Create", Form(title, _categoryId, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadDecodedBodyAsync()).Should().Contain("İlçe");
        (await InDbAsync(db => db.Events.IgnoreQueryFilters().AnyAsync(e => e.Title == title)))
            .Should().BeFalse();
    }

    // ────────────────────────────────────────────────────────────────────────
    // #43 — locationLabel TEK SAHİPLİ, liste ve detay AYNI projeksiyondan geçer
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LocationLabel_IsProducedByTheServer_ForEveryScope()
    {
        var admin = await _factory.SuperAdminAsync();

        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} L1", _categoryId, _kadirli));
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} L2", _categoryId, _osmaniyeMerkez));
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} L3", _categoryId, _adana));

        var page = await SendAsync(new GetEventsQuery(new QueryEventDto { Search = Marker, Limit = 50 }));
        var labels = page.Items.ToDictionary(x => x.Title, x => x.LocationLabel);

        labels[$"{Marker} L1"].Should().Be("Kadirli");
        labels[$"{Marker} L2"].Should().Be("Osmaniye / Merkez");
        labels[$"{Marker} L3"].Should().Be("Adana", "başka ilin merkezi yalnız il adıyla yazılır");
    }

    /// <summary>
    /// 🔴 Liste ve detay <b>aynı</b> projeksiyondan geçmek zorunda. 12.4 öncesinde iki ayrı
    /// <c>Select</c> bloğu vardı; yeni alanlar yalnız birine eklenseydi detay ekranı
    /// sessizce konumsuz kalırdı ve ne derleyici ne test bunu yakalardı.
    /// </summary>
    [Fact]
    public async Task DetailAndList_ReturnTheSameLocationFields()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} Parite";

        await admin.PostFormAsync("/EventsAdmin/Create", Form(title, _categoryId, _osmaniyeMerkez));
        var created = await EventAsync(title);

        var fromList = (await SendAsync(new GetEventsQuery(new QueryEventDto { Search = Marker, Limit = 50 })))
            .Items.Single(x => x.Title == title);
        var fromDetail = await SendAsync(new GetEventByIdQuery(created.Id));

        fromDetail.Should().NotBeNull();
        fromDetail!.DistrictId.Should().Be(fromList.DistrictId);
        fromDetail.DistrictName.Should().Be(fromList.DistrictName);
        fromDetail.ProvinceName.Should().Be(fromList.ProvinceName);
        fromDetail.LocationLabel.Should().Be(fromList.LocationLabel).And.NotBeNull();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Süzgeçler
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LocationScope_FiltersTheList()
    {
        var admin = await _factory.SuperAdminAsync();
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} S1", _categoryId, _kadirli));
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} S2", _categoryId, _osmaniyeMerkez));
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} S3", _categoryId, _adana));

        async Task<string[]> TitlesAsync(string? scope)
        {
            var page = await SendAsync(new GetEventsQuery(
                new QueryEventDto { Search = Marker, Limit = 50, LocationScope = scope }));
            return page.Items.Select(x => x.Title).OrderBy(x => x).ToArray();
        }

        (await TitlesAsync("local")).Should().Equal($"{Marker} S1");
        (await TitlesAsync("province")).Should().Equal($"{Marker} S1", $"{Marker} S2");
        (await TitlesAsync("nearby")).Should().Equal($"{Marker} S3");
        (await TitlesAsync(null)).Should().HaveCount(3);
    }

    /// <summary>
    /// 🔴 <c>ARCHITECTURE.md</c> §5: <b>bilinmeyen değer varsayılana düşer.</b>
    /// 400 dönseydi ya da liste boşalsaydı, mağazadaki bir istemcinin yazım hatası
    /// etkinlik ekranını tamamen kullanılamaz hâle getirirdi.
    /// </summary>
    [Fact]
    public async Task UnknownLocationScope_FallsBackToTheDefault_InsteadOfEmptyingTheList()
    {
        var admin = await _factory.SuperAdminAsync();
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} U1", _categoryId, _kadirli));

        var page = await SendAsync(new GetEventsQuery(
            new QueryEventDto { Search = Marker, Limit = 50, LocationScope = "kadirli-merkez" }));

        page.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task DistrictId_FiltersTheList()
    {
        var admin = await _factory.SuperAdminAsync();
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} D1", _categoryId, _kadirli));
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} D2", _categoryId, _adana));

        var page = await SendAsync(new GetEventsQuery(
            new QueryEventDto { Search = Marker, Limit = 50, DistrictId = _adana }));

        page.Items.Should().ContainSingle().Which.Title.Should().Be($"{Marker} D2");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Panel yüzeyi
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Listede konum sütunu ve şerit görünüyor mu — <b>ham</b> kapsam değeri (<c>nearby</c>)
    /// ekrana basılmamalı (Değişmez Kural #6).
    /// </summary>
    [Fact]
    public async Task Index_ShowsTheLocationColumnAndTurkishScopeChips()
    {
        var admin = await _factory.SuperAdminAsync();
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} Panel", _categoryId, _adana));

        var body = await (await admin.GetAsync($"/EventsAdmin?Search={Marker}")).ReadDecodedBodyAsync();

        body.Should().Contain("Konum");
        body.Should().Contain("Adana");
        body.Should().Contain("Çevre iller");
        body.Should().NotContain(">nearby<", "ham kapsam değeri ekrana basılmaz");
    }

    /// <summary>
    /// CSV, ekrandaki etiketin <b>aynısını</b> yazmalı: ayrı bir biçimlendirme yazılsaydı
    /// dosya ile ekran farklı konum gösterirdi.
    /// </summary>
    [Fact]
    public async Task ExportCsv_CarriesTheSameLocationLabel()
    {
        var admin = await _factory.SuperAdminAsync();
        await admin.PostFormAsync("/EventsAdmin/Create", Form($"{Marker} Csv", _categoryId, _osmaniyeMerkez));

        var text = await (await admin.GetAsync($"/EventsAdmin/ExportCsv?Search={Marker}")).Content.ReadAsStringAsync();

        text.Should().Contain("Konum");
        text.Should().Contain("Osmaniye / Merkez");
    }

    /// <summary>
    /// 🐛 <b>Canlı denetimde bulundu.</b> Konum şeridinin bağlantıları ilk yazımda
    /// <c>asp-route-*</c> ile <b>elle sayılıyordu</b> (<c>Search</c> + <c>DistrictId</c>) ve
    /// <c>sort</c> sayılmamıştı: başlığa göre sıralanmış bir listede "Çevre iller"e tıklamak
    /// sıralamayı <b>sessizce</b> varsayılana döndürüyordu — hiçbir test kırılmaz, hiçbir log
    /// düşmez. Kural artık <c>PanelQuery.With</c>'te ve mevcut sorgunun <b>tamamını</b> korur.
    /// </summary>
    [Fact]
    public async Task LocationChips_PreserveEveryOtherFilter_IncludingSort()
    {
        var admin = await _factory.SuperAdminAsync();

        var body = await (await admin.GetAsync(
            $"/EventsAdmin?Search={Marker}&sort=title_asc&DistrictId={_adana}")).ReadDecodedBodyAsync();

        var hrefs = System.Text.RegularExpressions.Regex
            .Matches(body, "href=\"(/EventsAdmin\\?[^\"]*LocationScope=[^\"]*)\"")
            .Select(m => m.Groups[1].Value)
            .ToList();

        hrefs.Should().NotBeEmpty("konum şeridi çizilmeli");
        hrefs.Should().OnlyContain(h => h.Contains("sort=title_asc"), "sıralama korunmalı");
        hrefs.Should().OnlyContain(h => h.Contains($"Search={Marker}"), "arama korunmalı");
        hrefs.Should().OnlyContain(h => h.Contains($"DistrictId={_adana}"), "ilçe süzgeci korunmalı");
    }

    /// <summary>
    /// Sayfa numarası <b>düşmeli</b>: süzgeç değiştikten sonra 7. sayfa artık bambaşka
    /// kayıtların sayfasıdır (<c>_SortableHeader</c>'daki aynı karar).
    /// </summary>
    [Fact]
    public async Task LocationChips_DropThePageNumber()
    {
        var admin = await _factory.SuperAdminAsync();

        var body = await (await admin.GetAsync("/EventsAdmin?page=3")).ReadDecodedBodyAsync();

        System.Text.RegularExpressions.Regex
            .Matches(body, "href=\"(/EventsAdmin\\?[^\"]*LocationScope=[^\"]*)\"")
            .Select(m => m.Groups[1].Value)
            .Should().NotBeEmpty()
            .And.OnlyContain(h => !h.Contains("page="));
    }

    /// <summary>Etkinlik formu ilçeyi <c>&lt;optgroup&gt;</c> ile il başlıklarına göre gruplamalı.</summary>
    [Fact]
    public async Task CreateForm_GroupsDistrictsByProvince()
    {
        var admin = await _factory.SuperAdminAsync();
        var body = await (await admin.GetAsync("/EventsAdmin/Create")).ReadDecodedBodyAsync();

        body.Should().Contain("optgroup");
        body.Should().Contain("label=\"Osmaniye\"");
        body.Should().Contain("label=\"Adana\"");
    }

    /// <summary>
    /// 🐛 <b>12.5 canlı denetiminde bulunan hata (12.4 kodunda).</b> İlçesi sonradan
    /// pasifleştirilen bir etkinlik, <b>başka hiçbir alanı</b> değiştirilemez hâle gelmişti:
    /// yönetici yalnız başlıktaki bir yazım hatasını düzeltmek istese bile
    /// "Seçilen ilçe bulunamadı veya pasif durumda" alıyordu — üstelik <b>hiç dokunmadığı bir
    /// alan</b> için. Kaydedebilmesinin tek yolu etkinliği <b>başka bir ilçeye taşımak</b>tı,
    /// yani başlığı düzeltmek için konumu değiştirmek.
    ///
    /// 🔑 Tek tek doğru olan iki kural çarpışıyordu: form pasif ilçeyi <b>seçili tutuyor</b>
    /// (konum sessizce değişmesin diye) ve resolver pasif ilçeyi <b>reddediyordu</b> (emekli
    /// ilçe yeniden seçilmesin diye). Kural artık "pasif ilçe <b>yeni olarak</b> seçilemez".
    /// </summary>
    [Fact]
    public async Task EventInADeactivatedDistrict_CanStillBeEdited()
    {
        var admin = await _factory.SuperAdminAsync();
        var title = $"{Marker} Pasif Ilce";
        await admin.PostFormAsync("/EventsAdmin/Create", Form(title, _categoryId, _adana));

        var created = await EventAsync(title);
        await SetDistrictActiveAsync(_adana, false);
        try
        {
            var newTitle = $"{Marker} Pasif Ilce (basligi duzeltildi)";
            var form = Form(newTitle, _categoryId, _adana);
            form["Id"] = created.Id.ToString();
            form["Status"] = created.Status;

            var response = await admin.PostFormAsync("/EventsAdmin/Edit", form,
                tokenFromPath: $"/EventsAdmin/Edit/{created.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect,
                "yalnız başlığı düzeltmek, dokunulmayan bir alan yüzünden reddedilmemeli");

            var updated = await InDbAsync(db => db.Events.AsNoTracking().FirstAsync(e => e.Id == created.Id));
            updated.Title.Should().Be(newTitle);
            updated.DistrictId.Should().Be(_adana, "pasif de olsa var olan konum korunmalı");
        }
        finally
        {
            await SetDistrictActiveAsync(_adana, true);
        }
    }

    private Task SetDistrictActiveAsync(Guid id, bool isActive) => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var district = await db.Districts.FirstAsync(d => d.Id == id);
        district.IsActive = isActive;
        await db.SaveChangesAsync();
    });

    /// <summary>
    /// 🔴 Ev ilçesi ne yeniden adlandırılabilir ne de pasifleştirilebilir: slug'ı
    /// <c>IsLocal</c> türetmesinin çıpası. Değişseydi o günden sonra yazılan <b>her</b>
    /// etkinlik "yerel değil" olur ve mobilin "Kadirli" süzgeci sessizce boşalırdı.
    /// </summary>
    [Fact]
    public async Task HomeDistrict_CannotBeRenamedOrDeactivated()
    {
        var admin = await _factory.SuperAdminAsync();

        var rename = await admin.PostFormAsync("/LookupsAdmin/DistrictUpdate", new Dictionary<string, string>
        {
            ["id"] = _kadirli.ToString(),
            ["provinceName"] = "Osmaniye",
            ["name"] = "Kadirli Merkez",
            ["isCenter"] = "false",
            ["displayOrder"] = "0",
            ["isActive"] = "true"
        }, tokenFromPath: "/LookupsAdmin");

        rename.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var home = await InDbAsync(db => db.Districts.AsNoTracking().FirstAsync(d => d.Id == _kadirli));
        home.Name.Should().Be("Kadirli");
        home.Slug.Should().Be(DistrictDefaults.HomeSlug);
        home.IsActive.Should().BeTrue();
    }
}
