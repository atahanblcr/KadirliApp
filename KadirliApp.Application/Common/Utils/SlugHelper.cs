using System.Text;

namespace KadirliApp.Application.Common.Utils;

/// <summary>
/// Türkçe karakter destekli slug üretimi (CreateAnnouncementType/BusinessRules emsali — Faz 10.9'da ortaklaştı).
///
/// <para>🐛 <b>Faz 11.15b düzeltmesi:</b> 10.9'daki ortaklaştırma sırasında **büyük harf Türkçe
/// eşlemesi düşmüştü** ve kural fiilen iki yere ayrılmıştı: <c>DbSeeder.Slugify</c> doğru
/// çalışıyor, çalışma zamanında panelin kullandığı bu sınıf ise yanlış üretiyordu.</para>
///
/// <para>Somut hata: <c>'İ'</c> (U+0130, Türkçe noktalı büyük I) <c>ToLowerInvariant()</c> ile
/// **küçülmez** — çok baytlı bir karşılığı olduğu için .NET onu olduğu gibi bırakır. Küçük harf
/// eşlemesine takılmaz, <c>IsLetterOrDigit</c> denetimini de geçer ve slug'a **olduğu gibi**
/// girerdi. Sonuç: "İstasyon Mahallesi" → <c>"İstasyon-mahallesi"</c>. Bu iki şeyi birden bozar:</para>
/// <list type="number">
///   <item>slug, URL'de kullanılan ASCII kimlik olmaktan çıkar;</item>
///   <item>daha kötüsü <c>"İstasyon"</c> ile <c>"istasyon"</c> **farklı** slug üretir →
///         benzersizlik denetimi ikisini de kabul eder ve mobilde aynı mahalle iki kez listelenir.</item>
/// </list>
/// <para>Kadirli'de İ ile başlayan mahalle adları yaygın (İstasyon, İnönü, İstiklal) — yani bu
/// yola pratikte er ya da geç girilecekti.</para>
/// </summary>
public static class SlugHelper
{
    /// <summary>
    /// Küçük harfler <c>ToLowerInvariant</c> sonrası, büyük harfler ise **doğrudan** eşlenir:
    /// invariant küçültme Türkçe büyük harfleri güvenilir biçimde çevirmiyor.
    /// </summary>
    private static readonly Dictionary<char, char> TurkishMap = new()
    {
        ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u',
        ['Ç'] = 'c', ['Ğ'] = 'g', ['İ'] = 'i', ['Ö'] = 'o', ['Ş'] = 's', ['Ü'] = 'u'
    };

    public static string Slugify(string value)
    {
        var lower = value.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);

        foreach (var ch in lower)
        {
            var c = TurkishMap.TryGetValue(ch, out var mapped) ? mapped : ch;

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            // Ardışık ayraçlar tek tireye iner: "A & B" → "a-b" (eskiden "a--b").
            // DbSeeder'ın davranışı buydu; iki gerçekleme artık aynı sonucu veriyor.
            else if (sb.Length > 0 && sb[^1] != '-')
            {
                sb.Append('-');
            }
        }

        return sb.ToString().Trim('-');
    }
}
