using System;
using System.Linq;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.15 — haber bildiriminin <b>başlığını ve gövdesini üreten tek yer</b> (saf).
/// </summary>
/// <remarks>
/// 🔴 <b>Bu sınıfın var olma sebebi bir uyumluluk borcu.</b> Bildirim
/// <c>relatedType = "news"</c> ile gidiyor; mağazadaki <b>eski sürümler</b> bu türü tanımaz
/// (§7 madde 18) → bildirimi listede <b>okur</b>, dokununca <b>hiçbir yere gitmez</b> ve hata
/// da almaz. Kullanıcı bu bedeli bilerek kabul etti (alternatifi haberi Duyurular listesine
/// de düşürmekti), ama bedelin <b>zorunlu bir hafifletmesi</b> var:
/// <b>gövde kendi kendine yeterli olmak zorunda.</b>
///
/// Yani gövdesi <i>"Detay için dokunun"</i> diyen bir bildirim, o sürümlerdeki kullanıcıya
/// <b>yalan söyler</b>: dokunacak, hiçbir şey olmayacak ve elinde hiçbir bilgi kalmayacak.
/// Bu yüzden gövde her zaman haberin <b>ilk cümlesini</b> taşır — deep-link çalışmasa bile
/// kullanıcı <i>ne olduğunu</i> öğrenmiş olur.
///
/// ⚠️ Başlık/özet <b>etkin</b> değerlerdir (override varsa o): panelde başlığı düzelten
/// yönetici, düzelttiği başlığın gitmesini bekler. Kaynağınki gönderilseydi düzeltme
/// "kaydedildi" der, şehre eski hâli giderdi — ve fark hiçbir yerde görünmezdi.
/// </remarks>
public static class NewsNotificationText
{
    /// <summary>Bildirim başlığının tavanı — <c>push_campaigns.title</c> kolonu 200.</summary>
    public const int MaxTitleLength = 200;

    /// <summary>
    /// Gövdenin tavanı. <c>NotificationDispatcher.MaxBodyLength</c> (500) zaten kırpıyor;
    /// buradaki tavan daha dar çünkü bildirim <b>gölgede</b> okunuyor: 180 karakterden
    /// sonrası hiçbir platformda görünmüyor ve kırpma noktası kelime ortasına düşüyor.
    /// </summary>
    public const int MaxBodyLength = 180;

    /// <summary>Etkin başlık (override → kaynak), tavanlanmış.</summary>
    public static string Title(string? titleOverride, string sourceTitle)
        => Clamp(Collapse(Pick(titleOverride, sourceTitle)), MaxTitleLength);

    /// <summary>
    /// Kendi kendine yeterli gövde: etkin özetin <b>ilk cümlesi</b>; özet yoksa düz metnin
    /// ilk cümlesi; ikisi de yoksa <b>başlığın kendisi</b>.
    /// </summary>
    /// <remarks>
    /// 🔑 Son çare "başlık" olmak zorunda ve boş metin <b>olamaz</b>: <c>PushCampaign.Body</c>
    /// <c>IsRequired</c> ve FCM boş gövdeli mesajı kimi cihazlarda <b>hiç göstermez</b> —
    /// yani özetsiz bir haberin bildirimi sessizce buharlaşırdı.
    /// ⚠️ Başlığın tekrar edilmesi bilinçli bir kabul: bildirimde başlık zaten üstte yazar,
    /// ama "iki kez aynı cümle" ile "gövdesi boş bildirim" arasında ilki <b>okunabilir</b>.
    /// </remarks>
    public static string Body(string? excerptOverride, string? sourceExcerpt, string? plainText, string title)
    {
        var summary = Pick(excerptOverride, sourceExcerpt);

        var sentence = FirstSentence(summary) ?? FirstSentence(plainText);

        return Clamp(sentence ?? Collapse(title), MaxBodyLength);
    }

    /// <summary>
    /// İlk cümle: <c>.</c> <c>!</c> <c>?</c> ya da <c>…</c>'de biter, <b>noktalama dâhil</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Cümle sonu bulunamazsa metnin <b>tamamı</b> döner (kırpmayı <see cref="Clamp"/>
    /// yapar). "Cümle bulamadım → boş dön" yazılsaydı noktalamasız tek satırlık bir özet
    /// bildirimi <b>gövdesiz</b> bırakırdı ve sebebi hiçbir yerde görünmezdi.
    /// 📌 Kısaltmalar (<i>"Av. Ali"</i>) yanlış bölünebilir; bunun bedeli bir <b>eksik
    /// cümle</b>, kazancı ise gövdenin her zaman anlamlı bir yerde bitmesi.
    /// </remarks>
    internal static string? FirstSentence(string? text)
    {
        var value = Collapse(text);
        if (value.Length == 0) return null;

        var end = value.IndexOfAny(['.', '!', '?', '…']);
        if (end < 0) return value;

        return value[..(end + 1)].Trim();
    }

    /// <summary>Override boş/whitespace ise kaynağa düşer ("boş metin" diye bir override yok).</summary>
    private static string? Pick(string? preferred, string? fallback)
        => string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    /// <summary>
    /// Satır sonlarını ve tekrarlı boşlukları tek boşluğa indirir.
    /// </summary>
    /// <remarks>
    /// 🔑 Kaynağın düz metni <c>\n</c> taşıyor (gövde HTML'inden türetiliyor) ve bildirim
    /// tek satırda çizilir: satır sonu temizlenmezse bazı cihazlarda gövde <b>ilk satırdan
    /// sonra kesilir</b> — hata yok, yalnız yarım bilgi.
    /// </remarks>
    private static string Collapse(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Tavanı aşan metni <b>kelime sınırında</b> keser ve "…" ekler.</summary>
    private static string Clamp(string value, int max)
    {
        if (value.Length <= max) return value;

        var cut = value[..max];
        var lastSpace = cut.LastIndexOf(' ');

        // Tek kelimelik dev bir metinde boşluk yok — o zaman ham kes (yoksa boş dönerdi).
        if (lastSpace > max / 2) cut = cut[..lastSpace];

        return cut.TrimEnd() + "…";
    }
}
