using System;

namespace KadirliApp.Application.Features.Legal;

/// <summary>
/// Faz 12.17 — panelden gelen <b>yürürlük tarihinin</b> UTC damgasına çevrilmesinin
/// <b>tek sahibi</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🐛 <b>Bu sınıf 12.17'nin CANLI DOĞRULAMASINDA bulunan gerçek bir hatadan doğdu.</b>
/// Panelin <c>&lt;input type="date"&gt;</c> alanı MVC'de <c>DateTime</c> olarak bağlanıyor ve
/// <see cref="DateTimeKind.Unspecified"/> taşıyor; Npgsql ise <c>timestamp with time zone</c>
/// kolonuna yalnız <b>UTC</b> yazıyor. Sonuç: panelden <b>yeni sürüm açmak hiç
/// çalışmıyordu</b> — <c>ArgumentException: Cannot write DateTime with Kind=Unspecified…</c>
/// ile 500. Yani 12.16'nın "metni değiştirmenin tek yolu yeni sürümdür" kuralının
/// <b>tek yolu kapalıydı</b>.
/// </para>
/// <para>
/// 🔑 <b>Testler neden görmedi:</b> 12.16'nın testlerinin hepsi <c>DateTime.UtcNow</c>
/// veriyordu, yani <c>Kind=Utc</c>. Kural doğru ölçülüyordu ama <b>panelin ürettiği değerle
/// değil</b> — 12.7'nin *"iki bağımsız sebep koruyorsa testin hangisini tuttuğunu ölç"*
/// dersinin kardeşi: <i>bir alanı test ederken, o alana GERÇEKTE ne geldiğini ölç.</i>
/// Kilit artık <c>Kind=Unspecified</c> bir değerle kuruluyor
/// (<c>PanelLegalTests.CreateVersion_AcceptsADateComingFromTheForm…</c>).
/// </para>
/// <para>
/// ⚠️ Projede bu dönüşümün deseni zaten vardı (<c>PowerOutage</c>, <c>Announcement</c>,
/// <c>DeathNotice</c> komutları <c>DateTime.SpecifyKind</c> çağırıyor); 12.16 onu
/// <b>atlamıştı</b>. Burada tek sahibe alınmasının sebebi iki çağıran olması
/// (<c>Create</c> + <c>Update</c>): ikisi ayrı yazılsaydı biri düzeltilip diğeri
/// unutulduğunda <b>taslağı düzenlemek</b> yine 500 verirdi.
/// </para>
/// </remarks>
public static class LegalDates
{
    /// <summary>
    /// Panel formundan gelen tarihi UTC damgasına çevirir.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Saat kaydırılmaz, yalnız etiketlenir.</b> Yönetici "15.08.2026" yazdığında
    /// kastettiği o takvim günüdür; değeri TR saatinden UTC'ye çevirmek metnin yürürlük
    /// gününü <b>bir geri kaydırabilirdi</b> — §7 madde 6'nın (*"TR günü, 00:00 UTC"*) bu
    /// projede dört kez tekrarlamış tuzağı.
    /// </remarks>
    public static DateTime FromPanel(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>Boş gelirse <paramref name="fallback"/> kullanılır (o da UTC'ye etiketlenir).</summary>
    public static DateTime FromPanel(DateTime? value, DateTime fallback) =>
        FromPanel(value ?? fallback);
}
