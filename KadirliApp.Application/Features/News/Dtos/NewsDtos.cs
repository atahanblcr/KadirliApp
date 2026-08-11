using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.News.Dtos;

/// <summary>Haberin kategorisi — listede rozet, mobilde süzgeç şeridi.</summary>
public class NewsCategoryRefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
}

/// <summary>
/// Faz 12.12 — public haber DTO'su.
/// </summary>
/// <remarks>
/// 🔑 <b>Başlık/özet/görsel "etkin" değerdir</b>: yönetici override yazdıysa o, yazmadıysa
/// kaynağınki. İstemci iki alanı birleştirmez — birleştirseydi mağazadaki eski sürümler
/// override'ı görmezdi ve panel düzeltmesi <b>yalnız yeni sürümlerde</b> görünürdü.
/// </remarks>
public class NewsArticleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Excerpt { get; set; }

    /// <summary>
    /// Temizlenmiş gövde HTML'i. ⚠️ <b>Yalnız detay ucunda dolu</b> — listede <c>null</c>.
    /// 27k kayıtlık bir modülde sayfa başına 20 gövde taşımak, hiç okunmayacak ~40 KB demekti.
    /// </summary>
    public string? ContentHtml { get; set; }

    /// <summary>Aynalanmış kapak görseli — <b>göreli</b> URL (§7 madde 9); origin'i istemci ekler.</summary>
    public string? ImageUrl { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }

    /// <summary>Kaynaktaki özgün adres ("Haberin kaynağı" bağlantısı).</summary>
    public string SourceUrl { get; set; } = default!;

    public DateTime PublishedAt { get; set; }
    public DateTime ModifiedAt { get; set; }

    /// <summary>Türetilmiş (12.12, plan dışı): tahmini okuma süresi — sunucuda tek yerde üretilir.</summary>
    public int ReadingMinutes { get; set; }

    public bool IsFeatured { get; set; }

    public List<NewsCategoryRefDto> Categories { get; set; } = new();
}

/// <summary>Public kategori listesi (<c>GET /v1/news/categories</c>) — dışlanmışlar hiç dönmez.</summary>
public class NewsCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>Uygulamadaki haber sayısı (kaynağınki değil) — dışlama/arşivleme sonrası gerçek sayı.</summary>
    public int ArticleCount { get; set; }

    /// <summary>Mobil süzgeç şeridinde görünsün mü (15 kategori bir şeride sığmaz).</summary>
    public bool ShowInFilterStrip { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>Liste süzgeci.</summary>
/// <remarks>
/// ⚠️ Arama parametresinin adı <b><c>search</c></b> — <c>searchTerm</c> yalnız taksi + ulaşımda
/// (§7 madde 4). Yanlış ad 400 vermez, <b>sessizce yok sayılır</b>.
/// </remarks>
public class QueryNewsDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// <c>true</c> → yalnız öne çıkarılmış (ve süresi geçmemiş) haberler;
    /// <c>false</c> → yalnız öne çıkmayanlar; <c>null</c> → süzme yok.
    /// </summary>
    /// <remarks>
    /// ⚠️ <c>false</c> 12.13'e kadar <b>sessizce yok sayılıyordu</b> (denetim bulgusu 9):
    /// süzdüğünü sanan çağıran tüm listeyi alıyordu. Üçlü anlam artık iki uçlu bir eksen.
    /// </remarks>
    public bool? Featured { get; set; }
}
