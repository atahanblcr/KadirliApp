namespace KadirliApp.Web.Common;

/// <summary>Kesintinin zamana göre üç hâli — mobildeki <c>PowerOutageStatus</c>'ün karşılığı.</summary>
public enum OutagePhase
{
    /// <summary>Şu an sürüyor.</summary>
    Ongoing,
    /// <summary>Henüz başlamadı (planlı).</summary>
    Planned,
    /// <summary>Bitti.</summary>
    Past
}

/// <summary>
/// Faz 11.17 — kesinti süzgecinin zaman tanımı.
///
/// ⚠️ <b>Bu tanım mobildeki <c>PowerOutage.isActive/isUpcoming/isPast</c> ile birebir
/// aynı olmak zorunda</b> (<c>mobile/lib/features/power_outages/data/models/power_outage.dart</c>).
/// <c>GET /v1/power-outages</c> bilinçli olarak sayfalamıyor ve tarih süzmüyor; süren/planlı
/// ayrımını **istemci** yapıyor. Panel kendi tanımını yazarsa yönetici "süren" derken
/// vatandaş "planlı" görür ve <b>kimse hata almaz</b> — görünmez sözleşme #23'ün aynı sınıfı.
///
/// Sınır kuralları (mobilden birebir): başlangıç anı <b>dâhil</b>, bitiş anı <b>hariç</b>.
/// </summary>
public static class PowerOutagePhaseRules
{
    public static OutagePhase Phase(DateTime startTime, DateTime endTime, DateTime nowUtc)
    {
        var start = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(endTime, DateTimeKind.Utc);

        if (start <= nowUtc && end > nowUtc) return OutagePhase.Ongoing;
        if (start > nowUtc) return OutagePhase.Planned;
        return OutagePhase.Past;
    }

    /// <summary>Süzgeç anahtarı ("ongoing"/"planned"/"past") → hâl. Tanınmayan değer null (süzme yok).</summary>
    public static OutagePhase? Parse(string? raw) => raw?.ToLowerInvariant() switch
    {
        "ongoing" => OutagePhase.Ongoing,
        "planned" => OutagePhase.Planned,
        "past" => OutagePhase.Past,
        _ => null
    };

    public static PanelBadge Badge(OutagePhase phase) => phase switch
    {
        OutagePhase.Ongoing => new PanelBadge("Sürüyor", "bg-red-100 text-red-800", "fa-bolt"),
        OutagePhase.Planned => new PanelBadge("Planlandı", "bg-amber-100 text-amber-800", "fa-clock"),
        _ => new PanelBadge("Bitti", "bg-gray-100 text-gray-700", "fa-check")
    };
}
