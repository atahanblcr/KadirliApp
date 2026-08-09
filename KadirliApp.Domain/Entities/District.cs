using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.4 — <b>il / ilçe sözlüğü.</b> Etkinliğin "nerede?" sorusunun tek doğruluk kaynağı.
/// </summary>
/// <remarks>
/// 🔑 Neden ayrı bir sözlük: <see cref="Event.City"/> serbest metindi, panelde formu bile yoktu
/// ve her kayıtta <c>null</c> kaldı. Serbest metin bir daha filtrelenemez, gruplanamaz ve
/// "çevre il etkinliği" gibi bir soruya cevap veremez — 12.3'te kesinti mahallesinde birebir
/// aynı sorun yaşandı (görünmez sözleşme #40).
///
/// ⚠️ Silme <b>yoktur</b>, <see cref="IsActive"/> ile pasifleştirilir: etkinlikler bu satıra
/// FK ile bağlı ve geçmiş bir etkinliğin ilçesi kaybolursa kayıt "nerede olduğu bilinmeyen"
/// hâle düşer (lookup deseninin 10.9(d)'deki kararı).
/// </remarks>
public class District : BaseEntity
{
    /// <summary>İlçe adı — il merkezleri için <c>"Merkez"</c>.</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 🔴 <b>İl + ilçe adından türetilir</b> (<c>DistrictDefaults.SlugFor</c>), elle yazılmaz.
    /// Yalnız ilçe adından üretilseydi her ilin <c>"Merkez"</c>'i çakışırdı.
    /// </summary>
    public string Slug { get; set; } = default!;

    /// <summary>İl adı ("Osmaniye", "Adana"). Ayrı bir <c>provinces</c> tablosu bilinçli olarak yok —
    /// bkz. <c>Progress.md</c> Faz 12.4.</summary>
    public string ProvinceName { get; set; } = default!;

    /// <summary>İlin merkez ilçesi mi — konum etiketi bunu kullanır ("Adana / Merkez" değil "Adana").</summary>
    public bool IsCenter { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
