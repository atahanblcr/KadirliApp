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
    /// Faz 12.1 — <b>izin matrisinde olmayan ama denetim izinde geçen</b> modül anahtarları.
    ///
    /// Yalnız-admin ekranların menü satırında <c>Module</c> <c>null</c>'dır (izin matrisinde
    /// karşılığı olmayan yetki belirmesin diye), ama komutları <c>AuditModule</c> yazmak
    /// zorunda. O anahtar burada karşılanmazsa denetim izi ekranı <b>ham İngilizce</b> basar
    /// (Değişmez Kural #6) — durum rozetleriyle birebir aynı hata sınıfı.
    /// </summary>
    private static readonly Dictionary<string, string> NonMatrixModules = new(StringComparer.Ordinal)
    {
        ["error-logs"] = "Hata Kayıtları"
    };

    /// <summary>
    /// İzin matrisi anahtarını (<c>deaths</c>) Türkçe modül adına çevirir.
    /// Kaynak <see cref="PanelMenu.Items"/> — ikinci bir liste tutulmaz, ayrışamaz.
    /// (11.15c: StaffAdmin izin rozetleri ham anahtar basıyordu.)
    /// Matris dışı anahtarlar için <see cref="NonMatrixModules"/>'a düşer.
    /// </summary>
    public static string ModuleLabel(string moduleKey) =>
        PanelMenu.Items.FirstOrDefault(i => i.Module == moduleKey)?.Label
        ?? (NonMatrixModules.TryGetValue(moduleKey, out var label) ? label : moduleKey);

    /// <summary>
    /// İzin modülü anahtarını panel controller adına çevirir (<c>deaths</c> → <c>DeathsAdmin</c>).
    /// Faz 11.16b: global arama sonucundan kaydın düzenleme ekranına bağlantı kurmak için.
    /// Kaynak yine <see cref="PanelMenu.Items"/> — eşleme ikinci bir yerde tutulmaz.
    /// </summary>
    public static string? ModuleController(string moduleKey) =>
        PanelMenu.Items.FirstOrDefault(i => i.Module == moduleKey)?.Controller;

    /// <summary>Modülün menüdeki ikonu (arama sonuçlarını gruplarken kullanılıyor).</summary>
    public static string ModuleIcon(string moduleKey) =>
        PanelMenu.Items.FirstOrDefault(i => i.Module == moduleKey)?.Icon ?? "fa-circle";

    // ── Denetim izi eylemleri (Faz 11.17) ───────────────────────────────────────
    //
    // audit_logs.action ham değerleri komutların IAuditableCommand.AuditAction'ından gelir
    // ("approve", "delete-schedule", "set-permissions"…). Ekrana ham basılırsa panel yine
    // İngilizce konuşur (Değişmez Kural #6) — durum rozetleriyle aynı sebep, aynı çözüm.
    //
    // ⚠️ Bu sözlük komutlardaki değerlerle **birebir** olmalı; yeni bir IAuditableCommand
    // eklendiğinde buraya satır atılmazsa PanelDisplayTests kırmızıya döner (kaynak taranır).

    private static readonly Dictionary<string, PanelBadge> AuditActions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Moderasyon
        ["approve"] = new("Onayladı", "bg-green-100 text-green-800", "fa-check-circle"),
        ["reject"] = new("Reddetti", "bg-red-100 text-red-800", "fa-times-circle"),
        ["resolve"] = new("Sonuçlandırdı", "bg-green-100 text-green-800", "fa-circle-check"),
        ["verify"] = new("Doğruladı", "bg-blue-100 text-blue-800", "fa-badge-check"),
        ["unverify"] = new("Doğrulamayı kaldırdı", "bg-gray-200 text-gray-700", "fa-ban"),
        ["ban"] = new("Yasakladı", "bg-red-100 text-red-800", "fa-user-slash"),
        ["unban"] = new("Yasağı kaldırdı", "bg-green-100 text-green-800", "fa-user-check"),

        // Genel CRUD
        ["create"] = new("Oluşturdu", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update"] = new("Güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["delete"] = new("Sildi", "bg-red-100 text-red-800", "fa-trash"),
        ["restore"] = new("Geri getirdi", "bg-green-100 text-green-800", "fa-trash-arrow-up"),

        // Personel / güvenlik
        ["set-permissions"] = new("İzinleri değiştirdi", "bg-purple-100 text-purple-800", "fa-user-shield"),
        ["change-password"] = new("Parola değiştirdi", "bg-purple-100 text-purple-800", "fa-key"),
        ["reset-password"] = new("Parola sıfırladı", "bg-purple-100 text-purple-800", "fa-key"),

        // Eczane nöbeti
        ["assign-duty"] = new("Nöbet atadı", "bg-indigo-100 text-indigo-800", "fa-calendar-plus"),
        ["delete-duty"] = new("Nöbet sildi", "bg-red-100 text-red-800", "fa-calendar-xmark"),

        // Alt kayıtlar (kategori / özellik / seçenek / sözlükler / ulaşım)
        ["create-category"] = new("Kategori ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update-category"] = new("Kategori güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["delete-category"] = new("Kategori sildi", "bg-red-100 text-red-800", "fa-trash"),
        ["create-property"] = new("Özellik ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update-property"] = new("Özellik güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["delete-property"] = new("Özellik sildi", "bg-red-100 text-red-800", "fa-trash"),
        ["create-option"] = new("Seçenek ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["delete-option"] = new("Seçenek sildi", "bg-red-100 text-red-800", "fa-trash"),
        ["create-neighborhood"] = new("Mahalle ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update-neighborhood"] = new("Mahalle güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["create-cemetery"] = new("Mezarlık ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update-cemetery"] = new("Mezarlık güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["create-mosque"] = new("Cami ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update-mosque"] = new("Cami güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["create-event-category"] = new("Etkinlik kategorisi ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update-event-category"] = new("Etkinlik kategorisi güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["create-place-category"] = new("Mekan kategorisi ekledi", "bg-indigo-100 text-indigo-800", "fa-plus"),
        ["update-place-category"] = new("Mekan kategorisi güncelledi", "bg-amber-100 text-amber-800", "fa-pen"),
        ["delete-schedule"] = new("Kalkış saati sildi", "bg-red-100 text-red-800", "fa-trash"),
        ["delete-stop"] = new("Durak sildi", "bg-red-100 text-red-800", "fa-trash"),

        // Hata kayıtları (Faz 12.1)
        ["resolve-error"] = new("Hatayı çözdü", "bg-green-100 text-green-800", "fa-circle-check"),
        ["reopen-error"] = new("Hatayı yeniden açtı", "bg-amber-100 text-amber-800", "fa-rotate-left"),
    };

    // ── Hata kayıtları (Faz 12.1) ───────────────────────────────────────────────
    //
    // Kaynak ve seviye DB'de İngilizce sabit (api/web/mobile · error/fatal) — ekrana ham
    // basılamaz. Durum rozetleriyle aynı kural, aynı gerekçe: panel Türkçe konuşur.

    private static readonly Dictionary<string, PanelBadge> ErrorSources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["api"] = new("Sunucu (API)", "bg-indigo-100 text-indigo-800", "fa-server"),
        ["web"] = new("Panel", "bg-purple-100 text-purple-800", "fa-desktop"),
        ["mobile"] = new("Mobil", "bg-teal-100 text-teal-800", "fa-mobile-screen")
    };

    private static readonly Dictionary<string, PanelBadge> ErrorLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["error"] = new("Hata", "bg-red-100 text-red-800", "fa-circle-exclamation"),
        ["fatal"] = new("Çökme", "bg-red-200 text-red-900", "fa-skull-crossbones")
    };

    /// <summary>Hata kaynağını Türkçe rozete çevirir. Bilinmeyen değer <b>ham geçmez</b>.</summary>
    public static PanelBadge ErrorSource(string? raw) => Lookup(ErrorSources, raw, "kaynak");

    /// <summary>Hata seviyesini Türkçe rozete çevirir. Bilinmeyen değer <b>ham geçmez</b>.</summary>
    public static PanelBadge ErrorLevel(string? raw) => Lookup(ErrorLevels, raw, "seviye");

    public static IReadOnlyCollection<string> KnownErrorSources => ErrorSources.Keys;
    public static IReadOnlyCollection<string> KnownErrorLevels => ErrorLevels.Keys;

    private static PanelBadge Lookup(Dictionary<string, PanelBadge> map, string? raw, string what)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new PanelBadge("—", "bg-gray-100 text-gray-500", "fa-minus");

        return map.TryGetValue(raw, out var badge)
            ? badge
            // Sorun gizlenmez ama İngilizce de sızmaz — AuditAction'daki aynı karar.
            : new PanelBadge($"Bilinmeyen {what} ({raw})", "bg-red-50 text-red-700", "fa-triangle-exclamation");
    }

    /// <summary>
    /// Denetim izi eylemini Türkçe rozete çevirir. Bilinmeyen eylem <b>ham geçmez</b> —
    /// durum rozetiyle aynı karar: sorun gizlenmez ama İngilizce de sızmaz.
    /// </summary>
    public static PanelBadge AuditAction(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new PanelBadge("—", "bg-gray-100 text-gray-500", "fa-minus");

        return AuditActions.TryGetValue(raw, out var badge)
            ? badge
            : new PanelBadge($"Bilinmeyen işlem ({raw})", "bg-red-50 text-red-700", "fa-triangle-exclamation");
    }

    /// <summary>Testlerin "kodun ürettiği her eylem eşlenmiş mi" sorusunu sorabilmesi için.</summary>
    public static IReadOnlyCollection<string> KnownAuditActions => AuditActions.Keys;

    /// <summary>
    /// Denetim izinde geçen entity tipini (<c>Ad</c>, <c>IntercitySchedule</c>) Türkçeleştirir.
    /// Eşleşmeyen tip ham geçer — bu bilinçli: yeni bir entity izi kaybolmasın, sadece
    /// İngilizce görünsün (rozet değil, yardımcı bilgi).
    /// </summary>
    public static string AffectedTypeLabel(string? raw) => raw switch
    {
        null or "" => "—",
        "Ad" => "İlan",
        "AdCategory" => "İlan kategorisi",
        "AdCategoryProperty" => "Kategori özelliği",
        "Announcement" => "Duyuru",
        "Business" => "İşletme",
        "Campaign" => "Kampanya",
        "Complaint" => "Şikayet",
        "Death" => "Vefat ilanı",
        "Event" => "Etkinlik",
        "EventCategory" => "Etkinlik kategorisi",
        "GuideItem" => "Rehber kaydı",
        "GuideCategory" => "Rehber kategorisi",
        "Pharmacy" => "Eczane",
        "PharmacyDuty" => "Eczane nöbeti",
        "Place" => "Mekan",
        "PlaceCategory" => "Mekan kategorisi",
        "PowerOutage" => "Elektrik kesintisi",
        "TaxiDriver" => "Taksici",
        "Neighborhood" => "Mahalle",
        "Cemetery" => "Mezarlık",
        "Mosque" => "Cami",
        "User" => "Kullanıcı",
        "ErrorLog" => "Hata kaydı",
        "IntercityRoute" => "Şehirlerarası hat",
        "IntercitySchedule" => "Kalkış saati",
        "IntracityRoute" => "Şehir içi hat",
        "IntracityStop" => "Durak",
        _ => raw
    };
}
