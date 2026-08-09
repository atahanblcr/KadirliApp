using KadirliApp.Application.Features.Lookups;

namespace KadirliApp.Web.Models;

/// <summary>
/// Faz 12.4 — il başlıklarına göre gruplu ilçe <c>&lt;select&gt;</c>'i.
/// </summary>
/// <remarks>
/// 🔑 Tek partial, üç yer: etkinlik <b>Create</b>, <b>Edit</b> ve Index'in <b>süzgeci</b>.
/// Üç ayrı <c>&lt;select&gt;</c> yazılsaydı "pasif ilçe listelenmez ama seçili olan kalır"
/// kuralı birinde unutulur ve düzenleme ekranı kaydın ilçesini <b>sessizce başka bir ilçeye
/// çevirirdi</b> — form açılıp kaydedildiğinde konum değişir, kimse fark etmez.
/// </remarks>
public class DistrictSelectViewModel
{
    public IReadOnlyList<DistrictAdminDto> Districts { get; init; } = [];

    /// <summary>Form alanı adı (<c>DistrictId</c>) ya da süzgeç adı (<c>districtId</c>).</summary>
    public string Name { get; init; } = "DistrictId";

    public Guid? Selected { get; init; }

    /// <summary>Boş seçenek metni; <c>null</c> ise boş seçenek hiç çizilmez (zorunlu alan).</summary>
    public string? EmptyLabel { get; init; }

    public bool Required { get; init; }

    public string CssClass { get; init; } =
        "w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500";

    /// <summary>
    /// Listelenecek satırlar: pasif ilçeler gizlenir <b>ama seçili olan her zaman kalır</b> —
    /// aksi hâlde kaydın ilçesi form kaydedildiğinde sessizce değişirdi.
    /// </summary>
    public IEnumerable<IGrouping<string, DistrictAdminDto>> Groups() => Districts
        .Where(d => d.IsActive || d.Id == Selected)
        .GroupBy(d => d.ProvinceName);
}
