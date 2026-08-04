namespace KadirliApp.Application.Features.Search.Dtos;

/// <summary>Tek bir arama sonucu — hangi modülden geldiği ve nereye gideceği.</summary>
/// <param name="Module">Panel modül anahtarı (<c>ads</c>, <c>deaths</c>…). Menü/izin anahtarıyla aynı.</param>
/// <param name="Id">Kaydın kimliği — panel bağlantısı bundan kurulur.</param>
/// <param name="Title">Listede gösterilecek ana metin (başlık, ad).</param>
/// <param name="Subtitle">İkincil satır (kategori, işletme, tarih…). Boş olabilir.</param>
/// <param name="Status">Ham durum değeri; panel <c>PanelDisplay.Status()</c> ile Türkçeleştirir.</param>
/// <param name="CreatedAt">Sıralama ve "ne zaman" bilgisi için.</param>
public record GlobalSearchHit(
    string Module,
    Guid Id,
    string Title,
    string? Subtitle,
    string? Status,
    DateTime CreatedAt);

/// <summary>Bir modülün sonuçları — kaç tane bulundu, kaçı gösteriliyor.</summary>
/// <remarks>
/// <paramref name="TotalCount"/> ile <c>Hits.Count</c> ayrı: modül başına yalnız ilk birkaç
/// sonuç gösteriliyor. Toplamı da söylemezsek yönetici "3 sonuç var" sanır ve modülün kendi
/// listesine gitmeyi düşünmez — 11.15c'nin "kullanıcı boş görüp veri yok sanıyor" hatasının
/// tersi biçimi.
/// </remarks>
public record GlobalSearchGroup(string Module, int TotalCount, IReadOnlyList<GlobalSearchHit> Hits);

/// <summary>Aramanın tüm sonucu.</summary>
public record GlobalSearchResult(string Term, IReadOnlyList<GlobalSearchGroup> Groups)
{
    public int TotalCount => Groups.Sum(g => g.TotalCount);
    public bool IsEmpty => TotalCount == 0;
}
