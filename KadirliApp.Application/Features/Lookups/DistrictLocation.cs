using System;
using KadirliApp.Application.Common.Utils;

namespace KadirliApp.Application.Features.Lookups;

/// <summary>
/// Faz 12.4 — uygulamanın <b>"burası neresi"</b> sabitleri. Tek yerde durur çünkü hem
/// <c>IsLocal</c> türetmesi, hem konum etiketi, hem de "çevre iller" süzgeci aynı iki
/// bilgiye dayanıyor: ev ilçesi ve ev ili.
/// </summary>
/// <remarks>
/// ⚠️ Ev ilçesi bir <b>slug sabiti</b>dir, veritabanındaki bir bayrak değil. Bayrak olsaydı
/// panelden yanlışlıkla başka bir ilçeye taşınabilir ve o an bütün etkinlikler sessizce
/// "yerel değil" hâline düşerdi — kimse hata almazdı.
/// </remarks>
public static class DistrictDefaults
{
    /// <summary>Uygulamanın ili. "Çevre iller" tanımı bunun <b>dışı</b> demektir.</summary>
    public const string HomeProvince = "Osmaniye";

    /// <summary>Uygulamanın ilçesi. <c>Event.IsLocal</c> tam olarak "bu ilçe mi" demektir.</summary>
    public const string HomeDistrictName = "Kadirli";

    /// <summary>Ev ilçesinin slug'ı — sözlükte aranan anahtar.</summary>
    public static string HomeSlug { get; } = SlugFor(HomeProvince, HomeDistrictName);

    /// <summary>
    /// İl + ilçe adından slug üretir. 🔴 Normalleştirmenin tek sahibi
    /// <see cref="SlugHelper"/> (görünmez sözleşme #21) — burada ikinci bir küçültme yok.
    /// </summary>
    public static string SlugFor(string provinceName, string name)
        => SlugHelper.Slugify($"{provinceName} {name}");
}

/// <summary>
/// Faz 12.4 — bir ilçenin <b>kullanıcıya gösterilen konum etiketi</b>. Saf sınıf, birim testli.
/// </summary>
/// <remarks>
/// 🔴 <b>Etiket sunucuda üretilir ve tek sahibi burasıdır.</b> Panel de mobil de aynı
/// metni <c>locationLabel</c> alanından okur. İstemcide üretilseydi panel "Osmaniye / Merkez",
/// mobil "Merkez" yazardı ve <b>kimse hata almazdı</b> (görünmez sözleşme #23'ün sınıfı).
///
/// Kural üç satır:
/// <list type="bullet">
///   <item>ev ilçesi → yalnız adı: <c>"Kadirli"</c> (kendi şehrinde il adı gürültüdür);</item>
///   <item>başka bir ilin <b>merkezi</b> → yalnız il adı: <c>"Adana"</c>
///         ("Adana / Merkez" kullanıcı için bilgi taşımaz);</item>
///   <item>geri kalan her şey → <c>"İl / İlçe"</c>: <c>"Osmaniye / Merkez"</c>, <c>"Adana / Ceyhan"</c>.</item>
/// </list>
/// </remarks>
public static class DistrictLabel
{
    public static string? For(string? name, string? provinceName, bool isCenter)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(provinceName))
            return null;

        var district = name.Trim();
        var province = provinceName.Trim();

        var isHomeProvince = string.Equals(
            SlugHelper.Slugify(province), SlugHelper.Slugify(DistrictDefaults.HomeProvince), StringComparison.Ordinal);

        if (isHomeProvince && string.Equals(
                SlugHelper.Slugify(district), SlugHelper.Slugify(DistrictDefaults.HomeDistrictName), StringComparison.Ordinal))
            return district;

        if (isCenter && !isHomeProvince)
            return province;

        return $"{province} / {district}";
    }
}
