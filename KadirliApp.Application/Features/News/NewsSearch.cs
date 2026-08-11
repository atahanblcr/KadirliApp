using System.Text;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.13 — haber aramasının <b>tek sahibi</b>: serbest metni <c>LIKE</c> desenine çevirir.
/// </summary>
/// <remarks>
/// 🔬 <b>ÖLÇÜLDÜ — ve ölçüm ilk gerekçeyi ÇÜRÜTTÜ (dürüst not).</b> 12.12 sonrası denetimin
/// 4. bulgusu <i>"<c>Contains</c> sağlayıcıda <c>strpos</c>'a çevrilir, hiçbir indeks onu
/// karşılayamaz"</i> diyordu. <c>ToQueryString()</c> ile bakıldığında bunun <b>doğru
/// olmadığı</b> görüldü: Npgsql 8, <c>x.SourceTitle.ToLower().Contains(term)</c>'i
/// <c>lower(...) LIKE @p ESCAPE '\'</c> olarak çeviriyor ve parametreyi kaçırarak yazıyor.
/// Yani ne "strpos" vardı ne de joker açığı.
/// <para>
/// 🔑 <b>Bulgunun SONUCU yine de doğruydu, sebebi başkaydı:</b> <c>lower(kolon) LIKE '%x%'</c>
/// bir <b>btree</b> indeksiyle karşılanamaz. 12.12'de "aramanın çıpası" diye konan
/// <c>ix_news_articles_source_title</c> gerçekte yalnız <i>sıralama</i> için çalışıyordu ve
/// yapılandırmadaki yorum bunun tersini söylüyordu. Asıl düzeltme bu sınıf değil,
/// <c>AddNewsSearchIndexes</c> migration'ındaki <b>GIN/trigram ifade indeksleri</b>.
/// </para>
/// <para>
/// 📌 <b>Peki bu sınıf neden yine de duruyor?</b> İki somut sebep, ikisi de mütevazı:
/// (a) <b>en az uzunluk</b> kuralının bir sahibi olması gerekiyordu — tek harflik bir arama
/// 27k satırda trigram indeksini zaten devre dışı bırakır; (b) desenin şekli artık
/// <b>sağlayıcının çeviri davranışına</b> değil bize ait ve testle kilitli — bir Npgsql
/// yükseltmesi çeviriyi değiştirirse bunu <c>PanelNewsTests</c> söyler, canlıdaki yavaşlık
/// değil.
/// </para>
///
/// ⚠️ Küçültme <b>burada değil sorguda</b> yapılır (<c>kolon.ToLower()</c>) — ifade indeksi de
/// <c>lower(kolon)</c> üzerinde. İkisi ayrışırsa sorgu derlenir, çalışır ve indeksi
/// <b>sessizce</b> kullanmaz: hata yok, yalnız 27k satırda tam tarama.
/// </remarks>
public static class NewsSearch
{
    /// <summary>Arama teriminin en az bu kadar karakter olması beklenir; altı süzmez.</summary>
    /// <remarks>
    /// Trigram indeksi üç karakterden kısa desende zaten devreye giremez; tek harfli bir arama
    /// 27k satırı tarayıp neredeyse her şeyi döndürürdü. Sınırın altındaki terim <b>süzgeci hiç
    /// uygulamaz</b> (400 değil) — §5: bilinmeyen/eksik değer listeyi boşaltmaz.
    /// </remarks>
    public const int MinimumLength = 2;

    /// <summary>
    /// Serbest metinden <c>%…%</c> desenini üretir. Süzülmeyecekse <c>null</c> döner.
    /// </summary>
    public static string? Pattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;

        var term = search.Trim();
        if (term.Length < MinimumLength) return null;

        var escaped = new StringBuilder(term.Length + 2).Append('%');

        foreach (var ch in term.ToLowerInvariant())
        {
            // Postgres'te LIKE'ın varsayılan kaçış karakteri ters bölüdür.
            // 📌 Npgsql'in `Contains` çevirisi de aynısını yapıyordu (ölçüldü) — burada
            // farklı bir şey değil, GÖRÜNÜR ve testli bir şey yapıyoruz.
            if (ch is '%' or '_' or '\\') escaped.Append('\\');
            escaped.Append(ch);
        }

        return escaped.Append('%').ToString();
    }
}
