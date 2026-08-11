using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Ganss.Xss;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News;

namespace KadirliApp.Infrastructure.News;

/// <summary>
/// Faz 12.12 — <see cref="INewsHtmlSanitizer"/>'ın <c>Ganss.Xss</c> (HtmlSanitizer) gerçeklemesi.
///
/// 🔑 <b>Beyaz liste buradan gelmiyor</b> — <see cref="NewsHtmlPolicy"/>'den geliyor.
/// Kütüphanenin varsayılan listesi bize göre <b>çok geniş</b> (tablo, span, style…) ve
/// bir gün paket güncellendiğinde sessizce değişebilirdi: politikayı kendi kodumuzda
/// tutmak, o değişimin bizim kararımız olmasını sağlıyor.
/// </summary>
public class NewsHtmlSanitizer : INewsHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public NewsHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        foreach (var tag in NewsHtmlPolicy.AllowedTags) _sanitizer.AllowedTags.Add(tag);

        _sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in NewsHtmlPolicy.AllowedAttributes) _sanitizer.AllowedAttributes.Add(attribute);

        _sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in NewsHtmlPolicy.AllowedSchemes) _sanitizer.AllowedSchemes.Add(scheme);

        // ⚠️ `style` özniteliği ve CSS özellikleri tamamen kapalı: kaynağın tema stilleri
        // uygulamanın tipografisini bozuyor ve CSS içinden çalışan saldırı biçimleri var.
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowDataAttributes = false;

        // 🔑 script/style/iframe/object/embed/form/video İÇERİĞİYLE birlikte atılır.
        // Yalnız etiketi atmak, sayfaya JS/CSS metnini DÜZ YAZI olarak basardı —
        // kullanıcı "function(){…}" okur, kimse hata almaz.
        _sanitizer.KeepChildNodes = false;
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // İçeriğiyle birlikte atılacaklar önce kaba bir süzgeçten geçer: HtmlSanitizer
        // bilinmeyen etiketin İÇERİĞİNİ koruduğu için (KeepChildNodes=false yalnız izinli
        // olmayanları düşürür), <form>/<object> gövdesindeki metin aksi hâlde kalırdı.
        var pruned = RemoveElementsWithContent(html!);

        return _sanitizer.Sanitize(pruned).Trim();
    }

    public string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var text = RemoveElementsWithContent(html!);
        text = Regex.Replace(text, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);

        // Çoklu boşluk/satır → tek boşluk: düz metin hem aramada hem okuma süresi
        // hesabında kullanılıyor, gürültü ikisini de bozar.
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static string RemoveElementsWithContent(string html)
    {
        var result = new StringBuilder(html);

        foreach (var tag in NewsHtmlPolicy.DroppedWithContent)
        {
            var current = result.ToString();
            var cleaned = Regex.Replace(
                current,
                $@"<{Regex.Escape(tag)}\b[^>]*>.*?</{Regex.Escape(tag)}\s*>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Kapanışı olmayan (kendi kendine kapanan ya da bozuk) hâli de düşür.
            cleaned = Regex.Replace(
                cleaned,
                $@"<{Regex.Escape(tag)}\b[^>]*/?>",
                string.Empty,
                RegexOptions.IgnoreCase);

            result.Clear();
            result.Append(cleaned);
        }

        return result.ToString();
    }
}
