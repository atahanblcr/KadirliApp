using System.Reflection;
using FluentAssertions;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Features.Ads.Queries;
using KadirliApp.Application.Features.Dashboard.Queries;
using KadirliApp.Application.Features.Guide.Dtos;
using KadirliApp.Application.Features.Guide.Queries;
using KadirliApp.Application.Features.Lookups;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Application.Features.Pharmacies.Queries;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Caching;

/// <summary>
/// Faz 11.15b — **önbellek sözleşmesi: klasik sessiz hata alanı.**
///
/// Önbellek yanlış yapılandırıldığında kimse hata almaz. Panelde eczane güncellenir,
/// yönetici "kaydedildi" mesajını görür, veritabanında satır değişmiştir — ama mobil
/// uygulama 15 dakika boyunca **eski nöbetçi eczaneyi** göstermeye devam eder. Ne log
/// düşer ne istisna atılır. Nöbetçi eczane söz konusuysa bu, gece yarısı yanlış adrese
/// giden bir insan demektir.
///
/// Buradaki denetimler <b>yapısal</b>dır: yeni bir cache'lenen sorgu ya da yeni bir
/// mutasyon eklendiğinde kendiliğinden kapsanırlar.
/// </summary>
public class CacheContractTests
{
    private static readonly Assembly ApplicationAssembly = typeof(ICacheableQuery).Assembly;

    private static IReadOnlyList<Type> CacheableQueryTypes() => ApplicationAssembly.GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(ICacheableQuery).IsAssignableFrom(t))
        .OrderBy(t => t.Name, StringComparer.Ordinal)
        .ToList();

    private static IReadOnlyList<Type> InvalidatorTypes() => ApplicationAssembly.GetTypes()
        .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(ICacheInvalidator).IsAssignableFrom(t))
        .OrderBy(t => t.Name, StringComparer.Ordinal)
        .ToList();

    private static IReadOnlySet<string> DeclaredGroups() => typeof(CacheGroups)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Süpürmenin bir şey bulduğundan emin ol — tip taraması sessizce boşalırsa
    /// aşağıdaki testlerin hepsi "hiçbir ihlal yok" diyerek yeşil kalır.
    /// </summary>
    [Fact]
    public void CachingSurface_IsDiscoverable()
    {
        CacheableQueryTypes().Should().HaveCountGreaterThan(8, "projede 10+ cache'lenen sorgu var");
        InvalidatorTypes().Should().HaveCountGreaterThan(10, "projede çok sayıda invalidate eden komut var");
        DeclaredGroups().Should().HaveCountGreaterThan(3);
    }

    /// <summary>
    /// 🔑 Grup adı serbest metin olsaydı, sorguda <c>"pharmacies"</c> yazıp komutta
    /// <c>"pharmacy"</c> yazmak **hiçbir hata üretmezdi** ve invalidation sonsuza dek
    /// çalışmazdı. Bu test grup adlarını <see cref="CacheGroups"/> sabitlerine bağlar.
    /// </summary>
    [Fact]
    public void EveryCacheGroupUsed_IsADeclaredConstant()
    {
        var declared = DeclaredGroups();
        var offenders = new List<string>();

        foreach (var type in CacheableQueryTypes())
        {
            var group = ((ICacheableQuery)CreateUninitialized(type)).CacheGroup;
            if (!declared.Contains(group)) offenders.Add($"{type.Name} → '{group}'");
        }

        foreach (var type in InvalidatorTypes())
        {
            foreach (var group in ((ICacheInvalidator)CreateUninitialized(type)).CacheGroupsToInvalidate)
                if (!declared.Contains(group)) offenders.Add($"{type.Name} → '{group}'");
        }

        offenders.Should().BeEmpty(
            "CacheGroups'ta tanımlı olmayan grup adları: {0}. " +
            "Serbest metin grup adı invalidation'ı SESSİZCE devre dışı bırakır.",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// 🔑 Bir grubu **hiçbir komut temizlemiyorsa** o veri TTL dolana kadar bayat kalır.
    /// Panelden yapılan değişikliğin mobile yansımaması demektir.
    /// </summary>
    [Fact]
    public void EveryCachedGroup_HasAtLeastOneInvalidatingCommand()
    {
        var cachedGroups = CacheableQueryTypes()
            .Select(t => ((ICacheableQuery)CreateUninitialized(t)).CacheGroup)
            .ToHashSet(StringComparer.Ordinal);

        var invalidatedGroups = InvalidatorTypes()
            .SelectMany(t => ((ICacheInvalidator)CreateUninitialized(t)).CacheGroupsToInvalidate)
            .ToHashSet(StringComparer.Ordinal);

        var orphans = cachedGroups.Except(invalidatedGroups).ToList();

        orphans.Should().BeEquivalentTo(new[] { CacheGroups.Dashboard },
            "invalidate edilmeyen tek grup 'dashboard' olmalı — o BİLİNÇLİ olarak yalnız " +
            "TTL'e (60 sn) dayanır, çünkü panel istatistiklerini neredeyse her mutasyon " +
            "etkiler ve hepsine invalidate eklemek sayaçları her yazmada sıfırlardı. " +
            "Listeye başka grup düşerse o modülde panel değişikliği mobile YANSIMIYOR demektir. " +
            "Bulunanlar: {0}", string.Join(", ", orphans));
    }

    /// <summary>
    /// Cache'lenen bir sorgunun TTL'i sıfır ya da negatif olursa Redis anahtarı hiç
    /// yazılmaz (ya da anında ölür) — önbellek sessizce kapanır, kimse fark etmez.
    /// Üst sınır da önemli: bayat veri en geç bu süre sonunda düzelmeli.
    /// </summary>
    [Fact]
    public void EveryCacheDuration_IsWithinSaneBounds()
    {
        foreach (var type in CacheableQueryTypes())
        {
            var ttl = ((ICacheableQuery)CreateUninitialized(type)).CacheDuration;

            ttl.Should().BeGreaterThan(TimeSpan.Zero, "{0} TTL'i pozitif olmalı", type.Name);
            ttl.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(1),
                "{0} TTL'i 1 saati aşıyor — invalidation kaçarsa bayat veri çok uzun yaşar", type.Name);
        }
    }

    // ───────────────── Anahtar üretimi: filtre değişince anahtar da değişmeli ─────────────────

    /// <summary>
    /// 🔑 Anahtar bir filtreyi taşımıyorsa, o filtrenin **farklı sonuçları aynı anahtarı
    /// paylaşır**: 2. sayfayı isteyen kullanıcı 1. sayfayı görür, arama yapan kullanıcı
    /// aramasız listeyi görür. Hata mesajı yoktur — yalnız yanlış veri.
    /// </summary>
    [Fact]
    public void PharmacyQueryKey_ChangesWithEveryFilter()
    {
        var baseline = new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 1, 20)).CacheKey;

        new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 2, 20)).CacheKey
            .Should().NotBe(baseline, "sayfa numarası anahtarda olmalı");
        new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 1, 50)).CacheKey
            .Should().NotBe(baseline, "sayfa boyutu anahtarda olmalı");
        new GetPharmaciesQuery(new QueryPharmacyDto("merkez", null, 1, 20)).CacheKey
            .Should().NotBe(baseline, "arama metni anahtarda olmalı");
        new GetPharmaciesQuery(new QueryPharmacyDto(null, true, 1, 20)).CacheKey
            .Should().NotBe(baseline, "aktiflik filtresi anahtarda olmalı");

        // ⚠️ En kritik ayrım: public uç yalnız aktif eczaneleri döner, panel hepsini.
        // Aynı anahtarı paylaşsalardı panelde pasif eczane gören yönetici cache'i doldurur,
        // mobil kullanıcı da PASİF eczaneyi nöbetçi sanırdı.
        new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 1, 20), OnlyActive: true).CacheKey
            .Should().NotBe(baseline, "public/panel ayrımı anahtarda olmalı");
    }

    [Fact]
    public void GuideQueryKeys_ChangeWithEveryFilter()
    {
        var dto = new QueryGuideItemDto();
        var baseline = new GetGuideItemsQuery(dto).CacheKey;

        new GetGuideItemsQuery(new QueryGuideItemDto { Page = 2 })
            .CacheKey.Should().NotBe(baseline);
        new GetGuideItemsQuery(new QueryGuideItemDto { Search = "eczane" })
            .CacheKey.Should().NotBe(baseline);
        new GetGuideItemsQuery(new QueryGuideItemDto { CategoryId = Guid.NewGuid() })
            .CacheKey.Should().NotBe(baseline);
    }

    [Fact]
    public void AdCategoryKeys_SeparateRootFromSubtree()
    {
        var root = new GetAdCategoriesQuery(null).CacheKey;
        var child = new GetAdCategoriesQuery(Guid.NewGuid()).CacheKey;

        child.Should().NotBe(root, "kök ve alt kategori listeleri aynı anahtarı paylaşamaz");

        var a = new GetCategoryPropertiesQuery(Guid.NewGuid()).CacheKey;
        var b = new GetCategoryPropertiesQuery(Guid.NewGuid()).CacheKey;
        a.Should().NotBe(b, "her kategorinin özellik listesi kendi anahtarında olmalı");
    }

    [Fact]
    public void PharmacyScheduleKey_ChangesWithMonth()
    {
        new GetPharmacyScheduleQuery(2026, 8).CacheKey
            .Should().NotBe(new GetPharmacyScheduleQuery(2026, 9).CacheKey, "ay anahtarda olmalı");
        new GetPharmacyScheduleQuery(2026, 8).CacheKey
            .Should().NotBe(new GetPharmacyScheduleQuery(2025, 8).CacheKey, "yıl anahtarda olmalı");
    }

    /// <summary>
    /// ⚠️ Nöbetçi eczane anahtarı **güne** bağlı. Gün düşerse gece yarısından sonra
    /// bir önceki günün nöbetçisi 15 dakika daha gösterilir.
    /// </summary>
    [Fact]
    public void OnDutyKey_IsScopedToTheDay()
    {
        var today = new DateOnly(2026, 8, 3);

        new GetOnDutyPharmaciesQuery(today).CacheKey
            .Should().NotBe(new GetOnDutyPharmaciesQuery(today.AddDays(1)).CacheKey, "gün anahtarda olmalı");
        new GetOnDutyPharmaciesQuery(today).CacheKey
            .Should().Be(new GetOnDutyPharmaciesQuery(new DateOnly(2026, 8, 3)).CacheKey,
                "aynı gün için anahtar sabit olmalı — yoksa cache hiç isabet etmez");
    }

    /// <summary>
    /// İki farklı sorgu tipi aynı anahtarı üretirse biri diğerinin sonucunu okur:
    /// tip uyuşmazlığı ya çözümleme hatasına ya da **sessizce yanlış veriye** yol açar.
    /// </summary>
    [Fact]
    public void DifferentQueryTypes_DoNotShareAKey()
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        var clashes = new List<string>();

        foreach (var (name, key) in new (string, string)[]
                 {
                     (nameof(GetDashboardStatsQuery), new GetDashboardStatsQuery().CacheKey),
                     (nameof(GetRecentActivitiesQuery), new GetRecentActivitiesQuery(8).CacheKey),
                     (nameof(GetNeighborhoodsQuery), new GetNeighborhoodsQuery().CacheKey),
                     (nameof(GetCemeteriesQuery), new GetCemeteriesQuery().CacheKey),
                     (nameof(GetMosquesQuery), new GetMosquesQuery().CacheKey),
                     (nameof(GetEventCategoriesQuery), new GetEventCategoriesQuery().CacheKey),
                     (nameof(GetPlaceCategoriesQuery), new GetPlaceCategoriesQuery().CacheKey),
                     (nameof(GetAdCategoriesQuery), new GetAdCategoriesQuery(null).CacheKey),
                     (nameof(GetPharmaciesQuery), new GetPharmaciesQuery(new QueryPharmacyDto(null, null, 1, 20)).CacheKey),
                     (nameof(GetPharmacyScheduleQuery), new GetPharmacyScheduleQuery(2026, 8).CacheKey),
                     (nameof(GetGuideItemsQuery), new GetGuideItemsQuery(new QueryGuideItemDto()).CacheKey)
                 })
        {
            if (keys.TryGetValue(key, out var owner)) clashes.Add($"{name} ile {owner}: '{key}'");
            else keys[key] = name;
        }

        clashes.Should().BeEmpty("aynı cache anahtarını üreten sorgular: {0}", string.Join(" | ", clashes));
    }

    /// <summary>
    /// Kurucusu parametre isteyen record'ları da tarayabilmek için: yalnız
    /// <c>CacheGroup</c>/<c>CacheDuration</c> gibi sabit dönen üyeleri okuyoruz,
    /// alanlara dokunulmuyor.
    /// </summary>
    private static object CreateUninitialized(Type type)
        => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
}
