namespace KadirliApp.Web.Models;

/// <summary>
/// Faz 12.10 — <c>_ModerationStatusField.cshtml</c>'in modeli: Düzenle formundaki
/// <b>salt-okunur</b> durum bloğu.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden ortak bir bileşen:</b> dört modülün Düzenle formunda dört ayrı durum açılır
/// menüsü vardı ve dördü de farklı seçenek listesi sunuyordu. Her görünüm kendi bloğunu
/// yazsaydı aynı ayrışma <i>salt-okunur</i> hâliyle geri gelirdi — panelin 11.15c'de yedi
/// listede yaşadığı sorun (bkz. <c>PanelDisplay</c>, <c>BulkToolbarViewModel</c>).
/// </para>
/// <para>
/// 🔑 <b>Butonlar neden <c>formaction</c> kullanıyor:</b> HTML'de form iç içe olamaz.
/// Blok, Düzenle formunun <i>içinde</i> çizildiği için buraya ayrı bir
/// <c>&lt;form asp-action="Approve"&gt;</c> konulamaz — tarayıcı onu sessizce atar ve
/// buton hiçbir şey yapmayan bir butona dönerdi (§7 madde 51'in savaştığı sınıf).
/// Bunun yerine gönderim hedefi buton üzerinde <c>formaction</c> ile değiştiriliyor;
/// antiforgery token'ı ve <c>id</c> alanı zaten formun içinde.
/// </para>
/// <para>
/// ⚠️ <c>formenctype</c> bilinçli olarak <c>application/x-www-form-urlencoded</c>:
/// Düzenle formları <c>multipart/form-data</c> ve içlerinde dosya seçici var. Override
/// olmasaydı "Onayla"ya basmak, yöneticinin henüz kaydetmediği fotoğrafı da yükler ve
/// hiçbir yere bağlamadan çöpe atardı.
/// </para>
/// </remarks>
public sealed class ModerationStatusFieldViewModel
{
    /// <summary>Kaydın <b>şu anki</b> durumu — rozet bundan üretilir.</summary>
    public required string? Status { get; init; }

    /// <summary>Onayla/Reddet aksiyonlarının bulunduğu panel controller'ı (ör. <c>AdsAdmin</c>).</summary>
    public required string Controller { get; init; }

    /// <summary>Onay diyaloğunda geçen Türkçe kayıt adı (ör. "ilan", "kampanya").</summary>
    public required string ItemLabel { get; init; }

    /// <summary>
    /// Red gerekçesi bu modülde saklanıyor mu? Etkinlikte <c>RejectedReason</c> kolonu
    /// <b>yok</b> (12.10 şema değiştirmiyor) — sormak, girilen metnin hiçbir yere
    /// yazılmadığı bir kutu üretirdi.
    /// </summary>
    public bool SupportsRejectReason { get; init; } = true;

    /// <summary>Yalnız vefat: yayındaki kaydı erken arşivleme yolu (<c>ArchiveDeathsJob</c>'ın elle hâli).</summary>
    public bool SupportsArchive { get; init; }

    private string Normalized => (Status ?? string.Empty).Trim().ToLowerInvariant();

    public bool IsApproved => Normalized == "approved";
    public bool IsRejected => Normalized == "rejected";
}
