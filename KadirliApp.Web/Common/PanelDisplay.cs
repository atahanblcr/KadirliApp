using System.Globalization;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 11.15c — **panelin ortak görsel dili: durum → Türkçe etiket + renk + ikon, ve para biçimi.**
///
/// Neden var: 11.15c canlı denetiminde panelin *modül modül* iyi ama **ekranların arası**
/// zayıf olduğu görüldü. Somut kanıt: yedi ayrı Index görünümü <c>approved</c>/<c>pending</c>
/// için elle Türkçe yazıyor, **geri kalan her değeri gri rozetle HAM basıyordu** —
/// yönetici <c>expired</c>, <c>archived</c>, <c>SuperAdmin</c> görüyordu
/// (CLAUDE.md Değişmez Kural #6 ihlali: "Arayüz Türkçe").
///
/// Mobilde bunun karşılığı <c>AdStatus</c> (11.10) — panelde eşi hiç yazılmamıştı.
/// Burada tek yerde toplanınca **yeni modül unutamaz**: bilinmeyen bir durum
/// <see cref="Status"/>'a düştüğünde <c>PanelDisplayTests</c> kırmızıya döner.
///
/// ⚠️ Bu sınıf <c>KadirliApp.Web</c> içinde durur, <c>Application</c>'a taşınmaz:
/// sunum kararı (renk/ikon) iş katmanına sızmamalı. Mobil kendi karşılığını kullanır.
/// </summary>
public sealed record PanelBadge(string Label, string Css, string Icon);

public static class PanelDisplay
{
    // ── Para ────────────────────────────────────────────────────────────────────
    //
    // ⚠️ Panel Program.cs'te bilinçli olarak InvariantCulture'a sabitlenmiştir
    // (form ondalıklarının "1.5" gibi okunabilmesi için). Bunun yan etkisi:
    // decimal.ToString("C2") para birimi simgesini bilemez ve JENERİK "¤" basar
    // → canlıda "¤750,000.00" görüldü. Bu yüzden para biçimi "C" formatına
    // BIRAKILMAZ, aşağıdaki tek yardımcıdan geçer.

    /// <summary>Türk Lirası biçimi: <c>₺750.000,00</c>. Değer yoksa "Belirtilmemiş".</summary>
    public static string TL(decimal? value, string emptyText = "Belirtilmemiş") =>
        value.HasValue ? TL(value.Value) : emptyText;

    /// <summary>Türk Lirası biçimi: <c>₺750.000,00</c> (binlik nokta, ondalık virgül).</summary>
    public static string TL(decimal value) =>
        "₺" + value.ToString("N2", TurkishNumbers);

    /// <summary>
    /// Yalnız SAYI biçimi için tr-TR; uygulamanın geri kalanı InvariantCulture'da kalır.
    /// (Simgeyi elle yazıyoruz çünkü tr-TR'de "₺" konumu/boşluğu .NET sürümüne göre değişebiliyor.)
    /// </summary>
    private static readonly CultureInfo TurkishNumbers = CultureInfo.GetCultureInfo("tr-TR");

    // ── Durum rozetleri ─────────────────────────────────────────────────────────

    private static readonly Dictionary<string, PanelBadge> Statuses = new(StringComparer.OrdinalIgnoreCase)
    {
        // Onay akışı — ilan / vefat / etkinlik / kampanya / işletme
        ["pending"] = new("Bekliyor", "bg-yellow-100 text-yellow-800", "fa-clock"),
        ["approved"] = new("Onaylandı", "bg-green-100 text-green-800", "fa-check-circle"),
        ["rejected"] = new("Reddedildi", "bg-red-100 text-red-800", "fa-times-circle"),

        // İlan yaşam döngüsü (ExpireAdsJob)
        ["expired"] = new("Süresi Doldu", "bg-orange-100 text-orange-800", "fa-hourglass-end"),

        // Vefat arşivi (ArchiveDeathsJob)
        ["archived"] = new("Arşivlendi", "bg-slate-200 text-slate-700", "fa-box-archive"),

        // Duyuru yayın durumu
        ["draft"] = new("Taslak", "bg-gray-200 text-gray-700", "fa-pen-to-square"),
        ["scheduled"] = new("Zamanlandı", "bg-amber-100 text-amber-800", "fa-clock"),
        ["active"] = new("Yayında", "bg-green-100 text-green-800", "fa-circle-check"),

        // Şikayet/istek akışı
        ["in_progress"] = new("İşlemde", "bg-blue-100 text-blue-800", "fa-spinner"),
        ["resolved"] = new("Çözüldü", "bg-green-100 text-green-800", "fa-circle-check"),
    };

    /// <summary>
    /// Ham durum değerini panelde gösterilecek rozete çevirir.
    /// Bilinmeyen değer <b>ham geçmez</b>: "Bilinmiyor (ham)" olarak işaretlenir ki
    /// yöneticiye anlamsız İngilizce sızmasın, ama sorun da gizlenmesin.
    /// </summary>
    public static PanelBadge Status(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new PanelBadge("—", "bg-gray-100 text-gray-500", "fa-minus");

        return Statuses.TryGetValue(raw, out var badge)
            ? badge
            : new PanelBadge($"Bilinmeyen durum ({raw})", "bg-red-50 text-red-700", "fa-triangle-exclamation");
    }

    /// <summary>Testlerin "her bilinen durum eşlenmiş mi" sorusunu sorabilmesi için.</summary>
    public static IReadOnlyCollection<string> KnownStatuses => Statuses.Keys;

    // ── Rol rozetleri ───────────────────────────────────────────────────────────

    private static readonly Dictionary<UserRole, PanelBadge> Roles = new()
    {
        [UserRole.User] = new("Vatandaş", "bg-gray-100 text-gray-700", "fa-user"),
        [UserRole.Moderator] = new("Moderatör", "bg-blue-100 text-blue-800", "fa-user-pen"),
        [UserRole.Admin] = new("Yönetici", "bg-indigo-100 text-indigo-800", "fa-user-gear"),
        [UserRole.SuperAdmin] = new("Süper Yönetici", "bg-purple-100 text-purple-800", "fa-user-shield"),
    };

    public static PanelBadge Role(UserRole role) =>
        Roles.TryGetValue(role, out var badge)
            ? badge
            : new PanelBadge($"Bilinmeyen rol ({role})", "bg-red-50 text-red-700", "fa-triangle-exclamation");

    /// <summary>Rolün string karşılığından (JWT/DTO biçimi: <c>super_admin</c>) rozet.</summary>
    public static PanelBadge Role(string? raw) =>
        Enum.TryParse<UserRole>((raw ?? string.Empty).Replace("_", string.Empty), ignoreCase: true, out var role)
            ? Role(role)
            : new PanelBadge($"Bilinmeyen rol ({raw})", "bg-red-50 text-red-700", "fa-triangle-exclamation");

    // ── İzin modülü etiketleri ──────────────────────────────────────────────────

    /// <summary>
    /// İzin matrisi anahtarını (<c>deaths</c>) Türkçe modül adına çevirir.
    /// Kaynak <see cref="PanelMenu.Items"/> — ikinci bir liste tutulmaz, ayrışamaz.
    /// (11.15c: StaffAdmin izin rozetleri ham anahtar basıyordu.)
    /// </summary>
    public static string ModuleLabel(string moduleKey) =>
        PanelMenu.Items.FirstOrDefault(i => i.Module == moduleKey)?.Label ?? moduleKey;
}
