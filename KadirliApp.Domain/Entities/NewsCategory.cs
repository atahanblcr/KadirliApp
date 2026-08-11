using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.12 — WordPress kategorisinin bizdeki sözlük karşılığı (15 satır).
///
/// <see cref="NewsArticle"/>'daki <b>iki sahip</b> ayrımı burada da geçerli ve yine
/// derleyiciyle korunuyor: <see cref="Name"/>/<see cref="Slug"/>/<see cref="ArticleCount"/>
/// kaynağın, <see cref="IsExcluded"/>/<see cref="ShowInFilterStrip"/>/<see cref="DisplayOrder"/>
/// yöneticinin. Senkron ikinciye yazmaya kalkarsa <c>CS8852</c> alır.
///
/// 📌 <b>Silme yok</b> — <c>LookupsAdmin</c>'in mevcut kuralı (FK'lı sözlük verisi yalnız
/// bayrakla pasifleşir). Kaynakta bir kategori kaldırılsa bile satır kalır: ona bağlı
/// haberlerin geçmişi kaybolmasın.
/// </summary>
public class NewsCategory : BaseEntity
{
    private string _name = default!;
    private string _slug = default!;
    private int _articleCount;
    private bool _isExcluded;
    private bool _showInFilterStrip = true;
    private int _displayOrder;

    /// <summary>WordPress kategori kimliği — eşleştirmenin tek anahtarı (unique).</summary>
    public int WpId { get; init; }

    public string Name { get => _name; init => _name = value; }
    public string Slug { get => _slug; init => _slug = value; }

    /// <summary>Kaynaktaki haber sayısı — panelde "hangi kategori ne kadar üretiyor" bilgisi.</summary>
    public int ArticleCount { get => _articleCount; init => _articleCount = value; }

    /// <summary>
    /// 🔴 <b>Dışlanmış kategorideki haber public uçlarda HİÇ görünmez.</b>
    /// </summary>
    /// <remarks>
    /// Semantik çoklu kategoriden doğuyor: bir haber <c>[Gündem, E-Gazete]</c> olabilir.
    /// "Dışlanmış <b>bir</b> kategorisi varsa gizle" seçildi; "hepsi dışlanmışsa gizle"
    /// olsaydı E-Gazete'yi kapatan yönetici, o haberlerin çoğunu yine listede görürdü ve
    /// dışlama işe yaramaz görünürdü — hata da vermezdi.
    /// </remarks>
    public bool IsExcluded { get => _isExcluded; init => _isExcluded = value; }

    /// <summary>Mobildeki kategori şeridinde görünsün mü (15 kategori bir şeride sığmaz).</summary>
    public bool ShowInFilterStrip { get => _showInFilterStrip; init => _showInFilterStrip = value; }

    public int DisplayOrder { get => _displayOrder; init => _displayOrder = value; }

    /// <summary>Senkronun kategoriye dokunabildiği tek yol — görünürlük alanlarına <b>dokunmaz</b>.</summary>
    public void ApplySourceSnapshot(string name, string slug, int articleCount)
    {
        _name = name;
        _slug = slug;
        _articleCount = articleCount;
    }

    /// <summary>Yöneticinin görünürlük kararları — kaynağın alanlarına <b>dokunmaz</b>.</summary>
    public void SetVisibility(bool isExcluded, bool showInFilterStrip, int displayOrder)
    {
        _isExcluded = isExcluded;
        _showInFilterStrip = showInFilterStrip;
        _displayOrder = displayOrder;
    }
}
