using System;
using System.Collections.Generic;
using KadirliApp.Application.Common.Utils;

namespace KadirliApp.Application.Features.PowerOutages;

/// <summary>Sözlükteki bir mahallenin eşleştirmeye yeten en küçük hâli.</summary>
/// <param name="Id">Kesintiye yazılacak FK.</param>
/// <param name="Name">Kesintinin <c>Neighborhood</c> metnine yazılacak <b>kanonik</b> ad.</param>
/// <param name="Slug">Sözlükteki hazır slug — <see cref="SlugHelper"/> ile üretilmiş olmalı.</param>
public readonly record struct NeighborhoodRef(Guid Id, string Name, string Slug);

/// <summary>
/// Faz 12.3 — <b>serbest metin mahalle adını sözlükteki mahalleye eşler.</b> Saf sınıf:
/// veritabanı görmez, birim testlidir, geri doldurma da panel de aynı kuralı kullanır.
/// </summary>
/// <remarks>
/// 🔴 <b>Normalleştirmenin tek sahibi <see cref="SlugHelper"/>'dır</b> (görünmez sözleşme #21).
/// Burada ikinci bir küçültme/karakter eşlemesi yazılsaydı Türkçe <c>'İ'</c> yüzünden
/// "İstasyon" ile "istasyon" farklı eşleşirdi — ve sonuç bir hata değil, <b>yanlış mahalleye
/// giden bildirim</b> olurdu.
///
/// 🔑 Slug'ın üstüne eklenen <b>tek</b> adım, sonundaki "mahallesi/mahalle/mah/mh" ekini
/// atmaktır. Bu ikinci bir normalleştirme değil: girdisi zaten ASCII slug, çıktısı yine slug,
/// Türkçe karakter kararı hâlâ tek yerde. Gerekçesi somut — sözlükte ad <c>"Cengiz Topel"</c>
/// biçiminde durur, kesinti kaydına ise yıllardır <c>"Cengiz Topel Mahallesi"</c> yazılıyor;
/// ek atılmasaydı <b>tek satır bile eşleşmezdi</b> ve geri doldurma sessizce sıfır sonuç verirdi.
/// </remarks>
public static class PowerOutageNeighborhoodMatcher
{
    /// <summary>
    /// Slug sonundaki mahalle ekleri. Uzundan kısaya sıralı: <c>"-mah"</c> önce denenirse
    /// <c>"-mahallesi"</c> hiçbir zaman tam eşleşmez ve geriye <c>"allesi"</c> kalırdı.
    /// </summary>
    private static readonly string[] Suffixes =
    [
        "-mahallesi", "-mahalle", "-mah", "-mh"
    ];

    /// <summary>Serbest metni sözlük anahtarına indirger. Boş girdi <c>null</c> döner.</summary>
    public static string? Normalize(string? freeText)
    {
        if (string.IsNullOrWhiteSpace(freeText)) return null;

        var slug = SlugHelper.Slugify(freeText);
        if (slug.Length == 0) return null;

        foreach (var suffix in Suffixes)
        {
            if (slug.EndsWith(suffix, StringComparison.Ordinal))
            {
                var trimmed = slug[..^suffix.Length].Trim('-');
                // ⚠️ Ek atıldığında geriye bir şey kalmıyorsa (kayıt yalnız "Mahalle" yazıyorsa)
                // slug'ı OLDUĞU GİBİ bırak: boş anahtar her mahalleyle eşleşme riski taşır.
                if (trimmed.Length > 0) return trimmed;
                break;
            }
        }

        return slug;
    }

    /// <summary>
    /// Serbest metne karşılık gelen mahalleyi bulur; bulamazsa <c>null</c>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Eşleşme <b>tam</b>dır. "İçeren" eşleşme denenmiyor çünkü bir kesintiyi <i>yanlış</i>
    /// mahalleye bağlamak, hiç bağlamamaktan kötüdür: eşleşmeyen kayıt panelde uyarı şeridiyle
    /// görünür ve bildirim gönderemez, yanlış eşleşen kayıt ise <b>başka bir mahallenin
    /// sakinlerine</b> bildirim yollar ve kimse hata almaz.
    /// </remarks>
    public static NeighborhoodRef? Match(string? freeText, IEnumerable<NeighborhoodRef> dictionary)
    {
        var key = Normalize(freeText);
        if (key is null) return null;

        foreach (var candidate in dictionary)
        {
            if (string.Equals(candidate.Slug, key, StringComparison.Ordinal))
                return candidate;

            // Sözlükteki adın kendisi de ek taşıyabilir ("Yenimahalle" değil ama
            // panelden "X Mahallesi" diye girilmiş bir lookup satırı olabilir).
            if (string.Equals(Normalize(candidate.Name), key, StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }
}
