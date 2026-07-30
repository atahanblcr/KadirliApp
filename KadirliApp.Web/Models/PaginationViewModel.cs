using KadirliApp.Application.Common.Models;

namespace KadirliApp.Web.Models;

/// <summary>
/// Panel listelerinin ortak sayfalama kontrolü (<c>Views/Shared/_Pagination.cshtml</c>) için model.
/// Sayfa bağlantıları mevcut query string'i (arama/filtre) KORUYARAK üretilir — yalnız
/// <c>page</c> parametresi değiştirilir. Tüm panel Index action'ları sayfayı ya
/// <c>[FromQuery] int page</c> ya da Query DTO'sunun <c>Page</c> alanıyla alır; query string
/// bağlama büyük/küçük harf duyarsız olduğundan tek bir <c>page</c> adı hepsinde çalışır.
/// </summary>
public sealed class PaginationViewModel
{
    /// <summary>Sayfa bağlantılarında değiştirilen query string parametresi.</summary>
    public const string PageParameter = "page";

    public PaginationViewModel(int currentPage, int totalPages, int totalCount, int pageSize, string itemLabel)
    {
        CurrentPage = currentPage;
        TotalPages = totalPages;
        TotalCount = totalCount;
        PageSize = pageSize;
        ItemLabel = itemLabel;
    }

    /// <summary>
    /// Sayfalı sonuçtan doğrudan kurar — view'larda kullanılan kısa yol. Sonuç null ise ya da
    /// sayfa boyutu geçersizse (<c>PagedResult.TotalPages</c> sıfıra bölerdi) kontrol hiç çizilmez.
    /// </summary>
    public static PaginationViewModel From<T>(PagedResult<T>? result, string itemLabel) =>
        result is null || result.PageSize <= 0
            ? new(1, 0, 0, 1, itemLabel)
            : new(result.CurrentPage, result.TotalPages, result.TotalCount, result.PageSize, itemLabel);

    public int CurrentPage { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public int PageSize { get; }

    /// <summary>Özet metnindeki kayıt adı — örn. "şikayet", "eczane", "ilan".</summary>
    public string ItemLabel { get; }

    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>Bu sayfadaki ilk kaydın 1 tabanlı sırası.</summary>
    public int FirstItemOnPage => TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

    /// <summary>Bu sayfadaki son kaydın 1 tabanlı sırası.</summary>
    public int LastItemOnPage => Math.Min(CurrentPage * PageSize, TotalCount);

    /// <summary>
    /// Gösterilecek sayfa numaraları — geçerli sayfanın çevresinde en fazla <paramref name="window"/>
    /// numara. Çok sayfalı listelerde 1..N'i olduğu gibi basmak kontrolü kullanılamaz hâle getiriyor.
    /// </summary>
    public IEnumerable<int> PageWindow(int window = 7)
    {
        if (TotalPages <= window)
            return Enumerable.Range(1, Math.Max(TotalPages, 1));

        var half = window / 2;
        var start = Math.Max(1, CurrentPage - half);
        var end = Math.Min(TotalPages, start + window - 1);
        start = Math.Max(1, end - window + 1);

        return Enumerable.Range(start, end - start + 1);
    }
}
