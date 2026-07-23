namespace KadirliApp.Application.Common.Caching;

/// <summary>
/// Faz 9.4: Bu arayüzü uygulayan MediatR query'leri CachingBehavior tarafından cache'lenir.
/// Yalnızca kullanıcıya göre DEĞİŞMEYEN (public/lookup) sonuçlar cache'lenmelidir.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Sorgu parametrelerini içermeli — farklı sayfa/filtre kombinasyonları ayrı anahtar üretir.</summary>
    string CacheKey { get; }

    /// <summary>Kaydın bağlanacağı invalidation grubu (<see cref="CacheGroups"/>).</summary>
    string CacheGroup { get; }

    TimeSpan CacheDuration { get; }
}

/// <summary>
/// Faz 9.4: Bu arayüzü uygulayan command'ler başarıyla tamamlandığında
/// CacheInvalidationBehavior ilgili grupların tüm anahtarlarını siler.
/// </summary>
public interface ICacheInvalidator
{
    IReadOnlyCollection<string> CacheGroupsToInvalidate { get; }
}

/// <summary>
/// Grup adları tek yerde — query'nin CacheGroup'u ile command'in invalidate listesi
/// aynı sabiti kullanmalı, aksi hâlde invalidation sessizce çalışmaz.
/// </summary>
public static class CacheGroups
{
    public const string Guide = "guide";
    public const string Pharmacies = "pharmacies";
    public const string Dashboard = "dashboard";
    /// <summary>Faz 10.4: mahalle/mezarlık/cami/etkinlik-kategorisi lookup'ları. 10.9(d)'den beri CRUD'ları var — LookupCommands invalidate eder.</summary>
    public const string Lookups = "lookups";
    /// <summary>Faz 10.5: ilan kategori ağacı + kategori özellikleri. 10.9(c)'den beri CRUD'ları var — AdCategoryCommands invalidate eder.</summary>
    public const string AdsLookup = "ads-lookup";
}
