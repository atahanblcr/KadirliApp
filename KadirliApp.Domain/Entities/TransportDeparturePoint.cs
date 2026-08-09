using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.5 — <b>kalkış noktası sözlüğü.</b> "Otobüs nereden kalkıyor?" sorusunun tek
/// doğruluk kaynağı.
/// </summary>
/// <remarks>
/// 🔑 Neden serbest metin değil: 12.3'te kesinti mahallesi, 12.4'te etkinlik ilçesi aynı
/// gerekçeyle sözlüğe bağlandı (görünmez sözleşme #40 ve #45). Serbest metin filtrelenemez,
/// gruplanamaz ve <b>koordinat taşıyamaz</b> — oysa bu tablonun asıl amacı koordinat:
/// mobildeki "Yol tarifi" butonu (12.6) buradan besleniyor. "Kadirli Otogar" on hatta ayrı
/// ayrı yazılsaydı, koordinatı düzeltmek on kaydı elle düzeltmek olurdu.
///
/// ⚠️ Silme <b>yoktur</b>, <see cref="IsActive"/> ile pasifleştirilir: hatlar bu satıra FK ile
/// bağlı (lookup deseninin 10.9(d) kararı, <see cref="District"/> ile aynı).
/// </remarks>
public class TransportDeparturePoint : BaseEntity
{
    /// <summary>Görünen ad: "Kadirli Otogarı", "Minibüs Garajı".</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 🔴 Addan türetilir (<c>SlugHelper</c> — görünmez sözleşme #21), elle yazılmaz.
    /// Benzersizlik bunun üzerinden kurulur: "Kadirli Otogarı" ile "kadirli otogarı"
    /// iki ayrı satır olamaz.
    /// </summary>
    public string Slug { get; set; } = default!;

    /// <summary>Açık adres — koordinat yoksa harita araması bununla yapılır (12.6).</summary>
    public string? Address { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<IntercityRoute> Routes { get; set; } = new List<IntercityRoute>();
}
