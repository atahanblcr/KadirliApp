using KadirliApp.Application.Features.Lookups;

namespace KadirliApp.Web.Models;

/// <summary>
/// Faz 12.5 — hattın kalkış noktası <c>&lt;select&gt;</c>'i (12.4'ün <see cref="DistrictSelectViewModel"/>
/// deseni).
/// </summary>
/// <remarks>
/// 🔑 Tek partial, iki yer: hat <b>Create</b> ve <b>Edit</b>. İki ayrı <c>&lt;select&gt;</c>
/// yazılsaydı "pasif nokta listelenmez ama seçili olan kalır" kuralı birinde unutulur ve
/// düzenleme ekranı hattın kalkış noktasını <b>sessizce boşaltırdı</b>.
///
/// ⚠️ İlçeden farkı: kalkış noktası <b>zorunlu değil</b> — 12.5 öncesi hatların kalkış noktası
/// gerçekten bilinmiyor ve bir tahmin vatandaşı yanlış yere götürürdü.
/// </remarks>
public class DeparturePointSelectViewModel
{
    public IReadOnlyList<DeparturePointAdminDto> Points { get; init; } = [];

    public string Name { get; init; } = "DeparturePointId";

    public Guid? Selected { get; init; }

    public string EmptyLabel { get; init; } = "Seçilmedi";

    public string CssClass { get; init; } =
        "w-full px-4 py-3 rounded-xl border border-gray-300 focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all shadow-sm outline-none";
}
