namespace KadirliApp.Application.Common.Security;

/// <summary>
/// Faz 12.2 — giriş denemesinde denenen kimliği **maskeler**.
///
/// 🔴 Neden zorunlu: <c>login_attempts</c> tablosu bir <b>güvenlik</b> tablosu ve tam da bu
/// yüzden ham telefon numarası biriktirmeye en uygun yer. Ham saklansaydı tablo kendisi
/// bir sızıntı hedefine dönerdi — kayıtlar panelde görülüyor, <b>CSV olarak dışa
/// aktarılıyor</b> ve başarısız denemeler 180 gün duruyor
/// (<c>CODE_REVIEW_CHECKLIST</c> §7 "hassas veri loglanmıyor mu").
///
/// 🔑 Maskeleme <b>tanılamayı öldürmemeli</b>: yönetici "hangi numara denendi" sorusuna
/// yaklaşık da olsa cevap verebilmeli, aksi hâlde tablo bir sayaçtan ibaret kalır.
/// Bu yüzden baş ve son korunur, orta gizlenir. Kesin kimlik zaten <c>UserId</c>'de.
///
/// ⚠️ 12.1'in <c>SensitiveDataMasker</c>'ından ayrı bir sınıf: o <b>sorgu dizesindeki
/// anahtar=değer</b> çiftlerini maskeler (yolun şeklini korur), bu ise <b>tek bir kimlik
/// değerini</b> maskeler. Aynı sınıfa sıkıştırılsalardı ikisinden birini değiştiren
/// diğerini sessizce bozardı.
/// </summary>
public static class LoginIdentifierMasker
{
    public const string Mask = "***";

    /// <summary>Tamamen gizlenen değerlerin yerine yazılan sabit (boş/anlamsız giriş).</summary>
    private const string Empty = "(boş)";

    /// <summary>Telefon numarasında baştan korunacak karakter sayısı (ülke kodu + operatör).</summary>
    private const int PhoneHeadLength = 6;

    /// <summary>Telefon numarasında sondan korunacak karakter sayısı.</summary>
    private const int PhoneTailLength = 4;

    /// <summary>Kullanıcı adında baştan korunacak karakter sayısı.</summary>
    private const int UsernameHeadLength = 3;

    /// <summary>
    /// <c>+905001112233</c> → <c>+90500***2233</c> · <c>admin</c> → <c>adm***</c> ·
    /// <c>ab</c> → <c>***</c> (kısa değer tamamen gizlenir; ilk iki harf zaten bir ipucu).
    /// </summary>
    public static string MaskIdentifier(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value))
            return Empty;

        return LooksLikePhone(value)
            ? MaskPhone(value)
            : MaskUsername(value);
    }

    /// <summary>
    /// Telefon mu? Rakam ve <c>+</c> dışında karakter içermiyorsa ve en az 7 rakamı varsa evet.
    /// (Kullanıcı adı da rakamdan oluşabilir; ama 7 haneli sayısal bir kullanıcı adı
    /// maskelenirken telefon gibi davranırsa kaybedilen bir şey yok — ikisi de gizlenmiş olur.)
    /// </summary>
    private static bool LooksLikePhone(string value)
    {
        var digits = 0;
        foreach (var ch in value)
        {
            if (char.IsDigit(ch)) { digits++; continue; }
            if (ch is '+' or ' ' or '-' or '(' or ')') continue;
            return false;
        }

        return digits >= 7;
    }

    private static string MaskPhone(string value)
    {
        // Ortada gizlenecek en az 1 karakter kalmıyorsa tamamını gizle — aksi hâlde
        // "maskelenmiş" görünen ama aslında tam olan bir değer saklardık.
        if (value.Length <= PhoneHeadLength + PhoneTailLength)
            return Mask;

        return string.Concat(value.AsSpan(0, PhoneHeadLength), Mask, value.AsSpan(value.Length - PhoneTailLength));
    }

    private static string MaskUsername(string value) =>
        value.Length <= UsernameHeadLength
            ? Mask
            : string.Concat(value.AsSpan(0, UsernameHeadLength), Mask);
}
