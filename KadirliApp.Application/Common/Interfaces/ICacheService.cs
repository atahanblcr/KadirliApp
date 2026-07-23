namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Faz 9.4: Redis üzerinde distributed cache. Grup (tag) mantığı: her kayıt bir gruba
/// bağlanabilir; grup invalidate edildiğinde gruba ait TÜM anahtarlar silinir
/// (örn. "guide" grubu → tüm sayfa/filtre kombinasyonları tek seferde temizlenir).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, string? group = null, CancellationToken ct = default);
    Task InvalidateGroupsAsync(IReadOnlyCollection<string> groups, CancellationToken ct = default);
}
