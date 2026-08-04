using KadirliApp.Application.Common.Exceptions;

namespace KadirliApp.Application.Common.Sorting;

/// <summary>
/// Faz 11.18 — **liste sıralamasının ortak çekirdeği** (11.15c B grubu: "sütun sıralaması
/// yalnız İlanlar'da, o da bir açılır liste").
///
/// Her modülün handler'ı kendi <c>switch</c>'ini yazsaydı üç şey modülden modüle ayrışırdı:
/// anahtar adları (<c>newest</c> mi <c>date_desc</c> mi), bilinmeyen değerde ne olacağı
/// ve **ikincil sıra**. Üçüncüsü en sinsisi: eşit değerli satırlarda ikincil sıra yoksa
/// Postgres satırları kararsız döndürür ve <b>aynı kayıt iki farklı sayfada görünebilir,
/// bir başkası hiç görünmez</b> — sayfalı listede sessiz veri kaybı. Bu yüzden her anahtar
/// tanımı ikincil sırayı da içerir ve tanım tek bir yerde toplanır.
///
/// ⚠️ <b>Varsayılan sıra asla değişmez.</b> <c>Sort</c> boş geldiğinde uygulanan sıra,
/// modülün 11.18 öncesindeki sırasının birebir aynısıdır — varsayılanı değiştirmek
/// mobil listeyi sessizce ters çevirirdi (CODE_REVIEW_CHECKLIST §1).
/// </summary>
public sealed class SortMap<T>
{
    private readonly Dictionary<string, Func<IQueryable<T>, IOrderedQueryable<T>>> _entries;
    private readonly string _defaultKey;
    private readonly bool _rejectUnknown;

    /// <param name="defaultKey">
    /// <c>Sort</c> boş/null geldiğinde uygulanacak anahtar. Modülün mevcut varsayılan
    /// sırasıyla **birebir aynı** olmalı.
    /// </param>
    /// <param name="rejectUnknown">
    /// ⚠️ Bilinmeyen anahtarda ne olacağı **modülün mevcut sözleşmesidir, tercih değil**:
    /// İlanlar 10.8'den beri 400 döndürüyor, Etkinlikler ise DTO'sunda açıkça
    /// "bilinmeyen değer varsayılana düşer (istemci hatası liste bozmaz)" yazıyor.
    /// Bu ayrımı tekleştirmek, iki modülden birinin yayındaki istemcilerini kırardı.
    /// </param>
    public SortMap(
        string defaultKey,
        IEnumerable<(string Key, Func<IQueryable<T>, IOrderedQueryable<T>> Apply)> entries,
        bool rejectUnknown = false)
    {
        _entries = entries.ToDictionary(e => e.Key, e => e.Apply, StringComparer.OrdinalIgnoreCase);
        _defaultKey = defaultKey;
        _rejectUnknown = rejectUnknown;

        if (!_entries.ContainsKey(defaultKey))
            throw new ArgumentException(
                $"Varsayılan sıralama anahtarı '{defaultKey}' tanımlı değil.", nameof(defaultKey));
    }

    /// <summary>Panelin sütun başlıklarını çizerken bildiği anahtarlar.</summary>
    public IReadOnlyCollection<string> Keys => _entries.Keys;

    public string DefaultKey => _defaultKey;

    public bool Knows(string? key) => !string.IsNullOrWhiteSpace(key) && _entries.ContainsKey(key);

    /// <summary>Sıralamayı uygular. Boş anahtar varsayılana düşer.</summary>
    public IOrderedQueryable<T> Apply(IQueryable<T> query, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return _entries[_defaultKey](query);

        if (_entries.TryGetValue(key, out var apply))
            return apply(query);

        if (_rejectUnknown)
            throw new AppException(
                $"Geçersiz sort değeri. Kullanılabilir: {string.Join(", ", _entries.Keys)}.",
                "VALIDATION_ERROR");

        return _entries[_defaultKey](query);
    }
}
