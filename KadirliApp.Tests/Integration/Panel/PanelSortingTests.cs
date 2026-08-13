extern alias WebPanel;

using System.Collections;
using System.Reflection;
using FluentAssertions;
using KadirliApp.Application.Common.Sorting;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncements;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.18 — **sütun sıralaması** (11.15c B grubu: "sıralama yalnız İlanlar'da,
/// o da bir açılır liste; diğer 15 listede başlığa tıklayarak sıralama yok").
///
/// 🔑 Bu sınıfın en kritik testi "artan sıralama artan sıralıyor" değil,
/// <see cref="DefaultOrdering_IsUnchanged"/>: sıralama **additive** bir özellik ve
/// varsayılan sırayı bir tık kaydırmak, mobil listeyi hiçbir hata vermeden ters
/// çevirirdi (CODE_REVIEW_CHECKLIST §1 — "varsayılan sıralamayı değiştirmek listeyi
/// sessizce ters çevirir"). Bu yüzden her modülün varsayılanı ayrı ayrı kilitleniyor.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelSortingTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "SortTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelSortingTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Announcements.IgnoreQueryFilters()
                .Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    // ————————————————————————————————————————————————————————————————
    // 1. SortMap sözleşmesi (saf mantık)
    // ————————————————————————————————————————————————————————————————

    /// <summary>
    /// 🔴 Varsayılan anahtarlar 11.18 ÖNCESİNDEKİ sıraların birebir aynısı olmalı.
    /// Biri değişirse mobil liste sessizce ters döner.
    /// </summary>
    [Fact]
    public void DefaultOrdering_IsUnchanged()
    {
        PanelSorts.Announcements.DefaultKey.Should().Be("created_desc",
            "duyurular 11.18 öncesinde OrderByDescending(CreatedAt) ile geliyordu");
        PanelSorts.Campaigns.DefaultKey.Should().Be("created_desc",
            "kampanyalar 11.18 öncesinde OrderByDescending(CreatedAt) ile geliyordu");
        PanelSorts.Deaths.DefaultKey.Should().Be("funeral_desc",
            "vefat kayıtları 11.18 öncesinde OrderByDescending(FuneralDate) ile geliyordu");
        PanelSorts.Events.DefaultKey.Should().Be("date_desc",
            "etkinliklerde 11.10'dan beri varsayılan 'en ileri tarih önce'");
    }

    /// <summary>
    /// ⚠️ Bilinmeyen anahtar **varsayılana düşmeli, patlamamalı**. Etkinliklerde bu,
    /// 11.10'dan beri <c>QueryEventDto</c>'da yazılı bir sözleşme ("istemci hatası liste
    /// bozmaz"); 400'e çevirmek yayındaki mobil sürümleri kırardı.
    /// </summary>
    [Fact]
    public void UnknownSortKey_FallsBackToDefault_DoesNotThrow()
    {
        var act = () => PanelSorts.Events.Apply(Array.Empty<Event>().AsQueryable(), "boyle-bir-sey-yok");
        act.Should().NotThrow("bilinmeyen sıralama listeyi bozmamalı (11.10 sözleşmesi)");
    }

    /// <summary>
    /// 🔑 Her sıralama anahtarı **ikincil sıra** içermeli. İçermezse eşit değerli
    /// satırlarda Postgres sırayı garanti etmez ve sayfalı listede aynı kayıt iki
    /// sayfada birden görünüp bir başkası hiç görünmeyebilir — sessiz veri kaybı.
    /// Bu testi ifadeyi inceleyerek değil, davranışla doğruluyoruz: eşit birincil
    /// anahtarlı iki kayıt her zaman AYNI sırada gelmeli.
    /// </summary>
    [Fact]
    public void EveryKey_ProducesStableOrderForTiedRows()
    {
        var shared = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var a = new Announcement { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Title = "Aynı", CreatedAt = shared };
        var b = new Announcement { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Title = "Aynı", CreatedAt = shared };

        foreach (var key in PanelSorts.Announcements.Keys)
        {
            var forward = PanelSorts.Announcements.Apply(new[] { a, b }.AsQueryable(), key).Select(x => x.Id).ToList();
            var reversed = PanelSorts.Announcements.Apply(new[] { b, a }.AsQueryable(), key).Select(x => x.Id).ToList();

            forward.Should().Equal(reversed,
                $"'{key}' anahtarında eşit satırların sırası girdi sırasına göre değişmemeli " +
                "(ikincil sıra yoksa sayfalı listede kayıt kaybolur)");
        }
    }

    /// <summary>
    /// 🔴 <b>Faz A bozma turunun bulgusu (13 Ağu 2026):</b> yukarıdaki süpürme yalnız
    /// <b>Announcements</b> haritasını geziyordu. Ölçüldü: <c>Campaigns</c>'in
    /// <c>end_asc</c>/<c>end_desc</c> anahtarlarından benzersiz ayraç (<c>ThenBy(Id)</c>)
    /// kaldırıldığında <b>hiçbir test kırılmadı</b> — yani §7 madde 30 sekiz haritanın
    /// yalnız birinde kilitliydi.
    ///
    /// <para>
    /// 🔑 Çözüm testi "genişletmek" değil, <b>kapsamı türetmek</b> (12.11'in dersi):
    /// harita listesi <see cref="PanelSorts"/>'un <b>kendisinden</b> yansımayla okunur.
    /// Yarın eklenen dokuzuncu bir harita kendiliğinden kapsama girer; elle bir liste
    /// tutulmaz — çünkü *bir taramanın kapsamı da elle tutulan bir listedir.*
    /// </para>
    ///
    /// <para>
    /// ⚠️ Ölçüt yine <b>davranış</b>: iki satırın <b>bütün</b> alanları eşit (yalnız
    /// <c>Id</c>'leri farklı), dolayısıyla her anahtar için birincil sıra berabere kalır.
    /// Bellek-içi sıralama <b>kararlı</b> olduğu için, benzersiz bir ayraç yoksa sonuç
    /// <b>girdi sırasını</b> izler — ters sırayla verilen aynı iki satır ters çıkar ve
    /// test kırmızıya döner. Ayraç <i>var ama benzersiz değilse</i> de aynı şey olur
    /// (11.18'in dersi: "bir ikincil anahtar koymak" yetmez).
    /// </para>
    /// </summary>
    [Fact]
    public void EverySortMapInTheProject_ProducesStableOrderForTiedRows()
    {
        var maps = typeof(PanelSorts)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType.IsGenericType
                        && f.FieldType.GetGenericTypeDefinition() == typeof(SortMap<>))
            .ToList();

        maps.Should().HaveCountGreaterThan(5,
            "kapsam PanelSorts'tan TÜRETİLİYOR — türetme çalışmıyorsa bu test hiçbir şey denetlemiyor");

        var unstable = new List<string>();
        var unevaluated = new List<string>();

        foreach (var field in maps)
        {
            var map = field.GetValue(null)!;
            var entity = field.FieldType.GetGenericArguments()[0];
            var idProperty = entity.GetProperty("Id")!;

            object Row(Guid id)
            {
                var row = Activator.CreateInstance(entity)!;
                idProperty.SetValue(row, id);
                return row;
            }

            var first = Row(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var second = Row(Guid.Parse("22222222-2222-2222-2222-222222222222"));

            var keys = (IEnumerable<string>)map.GetType().GetProperty("Keys")!.GetValue(map)!;
            var apply = map.GetType().GetMethod("Apply")!;

            foreach (var key in keys)
            {
                try
                {
                    var forward = OrderedIds(apply, map, entity, first, second, key);
                    var reversed = OrderedIds(apply, map, entity, second, first, key);

                    if (!forward.SequenceEqual(reversed))
                        unstable.Add($"{field.Name}.{key}");
                }
                catch (TargetInvocationException ex) when (ex.InnerException is NullReferenceException)
                {
                    // Gezinme özelliğine bakan anahtar bellek-içi değerlendirilemiyor.
                    unevaluated.Add($"{field.Name}.{key}");
                }
            }
        }

        unstable.Should().BeEmpty(
            "her sıralama anahtarı BENZERSİZ bir ayraçla (ThenBy(Id)) bitmeli (§7 madde 30). " +
            "Bitmezse eşit değerli satırlarda Postgres sırayı garanti etmez ve sayfalı listede " +
            "aynı kayıt iki sayfada birden görünüp bir başkası HİÇ görünmez — hata vermeyen " +
            "veri kaybı. Kararsız anahtarlar: {0}", string.Join(", ", unstable));

        unevaluated.Should().BeEmpty(
            "bellek-içi değerlendirilemeyen anahtar, bu testin kapsamındaki bir DELİKTİR: " +
            "sessizce atlanırsa kapsam yine elle tutulmuş olur. Böyle bir anahtar doğarsa " +
            "ya satırı kurarken ilgili gezinme nesnesi doldurulmalı ya da anahtar burada " +
            "bilinçli olarak muaf tutulup gerekçesi yazılmalı. Değerlendirilemeyenler: {0}",
            string.Join(", ", unevaluated));
    }

    /// <summary>İki satırı verilen sırayla sıralayıp kimlik listesini döndürür.</summary>
    private static List<Guid> OrderedIds(
        MethodInfo apply, object map, Type entity, object a, object b, string key)
    {
        var array = Array.CreateInstance(entity, 2);
        array.SetValue(a, 0);
        array.SetValue(b, 1);

        var queryable = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == nameof(Queryable.AsQueryable) && m.IsGenericMethod)
            .MakeGenericMethod(entity)
            .Invoke(null, new object[] { array })!;

        var ordered = (IEnumerable)apply.Invoke(map, new[] { queryable, (object)key })!;
        var idProperty = entity.GetProperty("Id")!;
        return ordered.Cast<object>().Select(x => (Guid)idProperty.GetValue(x)!).ToList();
    }

    [Fact]
    public void DefaultKey_MustExistInMap()
    {
        var act = () => new SortMap<Announcement>(
            "olmayan-anahtar",
            new (string, Func<IQueryable<Announcement>, IOrderedQueryable<Announcement>>)[]
            {
                ("created_desc", q => q.OrderByDescending(x => x.CreatedAt))
            });

        act.Should().Throw<ArgumentException>("tanımsız varsayılan sessizce kabul edilmemeli");
    }

    // ————————————————————————————————————————————————————————————————
    // 2. Uçtan uca: sorgu gerçekten sıralıyor mu
    // ————————————————————————————————————————————————————————————————

    private async Task SeedAnnouncementsAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var typeId = await db.AnnouncementTypes.Select(t => t.Id).FirstAsync();

            var newer = new Announcement { Title = $"{_marker} Zeytin", TypeId = typeId, Body = "x", Status = "active" };
            var older = new Announcement { Title = $"{_marker} Ahlat", TypeId = typeId, Body = "x", Status = "active" };
            db.Announcements.AddRange(newer, older);
            await db.SaveChangesAsync();

            // ⚠️ `CreatedAt` eklemede verilemez: AppDbContext.SaveChanges, State == Added olan
            // her varlığın CreatedAt'ini UtcNow ile EZER. İlk yazımda tarihler kurucuda
            // veriliyordu ve iki kayıt da aynı ana düşüyordu — "en yeni önce" testi
            // rastgele sonuç veriyordu. Tarihler bu yüzden ikinci bir geçişte yazılıyor.
            newer.CreatedAt = DateTime.UtcNow.AddDays(-1);
            older.CreatedAt = DateTime.UtcNow.AddDays(-3);
            await db.SaveChangesAsync();
        });
    }

    private async Task<List<string>> QueryTitlesAsync(string? sort)
    {
        List<string> titles = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var result = await sp.GetRequiredService<ISender>()
                .Send(new GetAnnouncementsQuery { Page = 1, Limit = 100, Sort = sort });

            titles = result.Items
                .Where(a => a.Title.Contains(_marker))
                .Select(a => a.Title)
                .ToList();
        });
        return titles;
    }

    [Fact]
    public async Task TitleAscending_SortsAlphabetically()
    {
        await SeedAnnouncementsAsync();

        var titles = await QueryTitlesAsync("title_asc");

        titles.Should().HaveCount(2);
        titles[0].Should().Contain("Ahlat");
        titles[1].Should().Contain("Zeytin");
    }

    [Fact]
    public async Task TitleDescending_ReversesTheOrder()
    {
        await SeedAnnouncementsAsync();

        var titles = await QueryTitlesAsync("title_desc");

        titles[0].Should().Contain("Zeytin");
        titles[1].Should().Contain("Ahlat");
    }

    /// <summary>Sıralama parametresi hiç verilmediğinde eski davranış (en yeni önce) sürmeli.</summary>
    [Fact]
    public async Task NoSortParameter_KeepsNewestFirst()
    {
        await SeedAnnouncementsAsync();

        var titles = await QueryTitlesAsync(null);

        titles[0].Should().Contain("Zeytin", "Zeytin daha yeni (1 gün önce), Ahlat daha eski (3 gün önce)");
    }

    // ————————————————————————————————————————————————————————————————
    // 3. Panel arayüzü
    // ————————————————————————————————————————————————————————————————

    [Theory]
    [InlineData("/AdsAdmin/Index")]
    [InlineData("/EventsAdmin/Index")]
    [InlineData("/CampaignsAdmin/Index")]
    [InlineData("/DeathsAdmin/Index")]
    [InlineData("/AnnouncementsAdmin/Index")]
    public async Task ListPage_RendersSortableHeaders(string path)
    {
        var client = await _factory.SuperAdminAsync();
        var body = await (await client.GetAsync(path)).ReadDecodedBodyAsync();

        body.Should().Contain("data-sortable-header", $"{path} tıklanabilir başlık çizmeli");
        body.Should().Contain("aria-sort", "sıralama durumu ekran okuyucuya da bildirilmeli");
        body.Should().Contain("sort=", "başlık bağlantısı sort parametresi üretmeli");
    }

    /// <summary>
    /// 🔑 Sıralama bağlantısı mevcut filtreyi **korumalı**, sayfayı ise **sıfırlamalı**.
    /// Filtre kaybolursa yönetici her sıralamada filtreyi yeniden kurar; sayfa korunursa
    /// 7. sayfadayken sıralama değiştiren kişi bambaşka kayıtların olduğu 7. sayfaya düşer.
    /// </summary>
    [Fact]
    public async Task SortLink_PreservesFiltersButResetsPage()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await (await client.GetAsync("/AdsAdmin/Index?status=pending&page=3")).ReadDecodedBodyAsync();

        body.Should().Contain("status=pending", "sıralama bağlantısı mevcut süzgeci taşımalı");
        body.Should().MatchRegex(@"href=""/AdsAdmin/Index\?[^""]*sort=", "bağlantı sort üretmeli");

        var sortLinks = System.Text.RegularExpressions.Regex
            .Matches(body, @"href=""(/AdsAdmin/Index\?[^""]*sort=[^""]*)""")
            .Select(m => m.Groups[1].Value)
            .ToList();

        sortLinks.Should().NotBeEmpty();
        sortLinks.Should().OnlyContain(l => !l.Contains("page="),
            "sıralama değişince sayfa başa dönmeli — yoksa kayıtlar 'kaybolmuş' görünür");
    }

    /// <summary>Etkin sütun, yönünü hem ikonla hem aria-sort ile göstermeli.</summary>
    [Fact]
    public async Task ActiveColumn_ShowsDirection()
    {
        var client = await _factory.SuperAdminAsync();
        var body = await (await client.GetAsync("/AnnouncementsAdmin/Index?sort=title_asc")).ReadDecodedBodyAsync();

        body.Should().Contain("aria-sort=\"ascending\"", "etkin sütunun yönü belirtilmeli");
        body.Should().Contain("fa-sort-up", "yön görsel olarak da gösterilmeli");
    }

    /// <summary>Etkin olmayan sütuna ilk tıklama **artan** başlamalı ("A'dan Z'ye" beklentisi).</summary>
    [Fact]
    public async Task InactiveColumn_FirstClickSortsAscending()
    {
        var model = new WebPanel::KadirliApp.Web.Models.SortableHeaderViewModel(
            "Başlık", "title_asc", "title_desc", CurrentSort: null);

        model.IsActive.Should().BeFalse();
        model.NextSort.Should().Be("title_asc");
    }

    /// <summary>Etkin ve artan sütuna tıklamak azalana çevirmeli (aç/kapa davranışı).</summary>
    [Fact]
    public async Task ActiveAscendingColumn_TogglesToDescending()
    {
        var model = new WebPanel::KadirliApp.Web.Models.SortableHeaderViewModel(
            "Başlık", "title_asc", "title_desc", CurrentSort: "title_asc");

        model.IsActive.Should().BeTrue();
        model.IsAscending.Should().BeTrue();
        model.NextSort.Should().Be("title_desc");
    }
}
