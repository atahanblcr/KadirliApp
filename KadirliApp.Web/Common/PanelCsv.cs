using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 11.16b — panel listelerinin **CSV dışa aktarma çekirdeği** (11.18'den kalan madde).
///
/// <para>
/// 🔑 <b>Neden ortak bir yardımcı:</b> "CSV yazmak" ilk bakışta
/// <c>string.Join(",", …)</c> kadar basit görünür; oysa aşağıdaki dört ayrıntının her biri
/// yanlış yapıldığında **sessiz** hasar verir — dosya indirilir, açılır ve <i>yanlış</i>
/// görünür. Bu yüzden biçimlendirme tek yerde toplandı, çağıran yalnız sütunları söyler.
/// </para>
///
/// <list type="number">
///   <item><b>UTF-8 BOM zorunlu.</b> Excel, BOM'suz bir CSV'yi sistem kod sayfasıyla
///   açar; Türkçe Windows'ta "İstanbul" → "Ä°stanbul" olur. Veri doğrudur, ekran yanlıştır.</item>
///   <item><b>Ayraç noktalı virgül.</b> Türkçe yerelde ondalık ayracı virgüldür, bu yüzden
///   Excel CSV ayracı olarak <c>;</c> bekler. Virgülle yazılan dosya Excel'de
///   <b>tek sütuna</b> düşer — kullanıcı "bozuk" der, sebebini bulamaz.</item>
///   <item><b>Formül enjeksiyonu.</b> Hücre <c>=</c>, <c>+</c>, <c>-</c>, <c>@@</c> ile
///   başlıyorsa Excel onu <b>formül olarak çalıştırır</b>. İçeriğin bir kısmı
///   <i>vatandaşın yazdığı</i> ilan başlığı olduğu için bu gerçek bir saldırı yüzeyi:
///   <c>=HYPERLINK(...)</c> başlıklı bir ilan, yöneticinin Excel'inde canlı bağlantıya
///   dönüşür. Bu tür hücrelerin başına tek tırnak konur.</item>
///   <item><b>Satır tavanı — sessiz kırpma YOK.</b> Dışa aktarmanın amacı "tüm sonuçlar"
///   olduğu için sayfalama yapılmaz; ama filtresiz bir liste tüm tabloyu belleğe çeker
///   (checklist §8). Tavanı aşan istek <b>kırpılmaz, reddedilir</b> ve yöneticiye filtreyi
///   daraltması söylenir. Yarım bir dosyayı tam sanmak, dosyayı hiç alamamaktan kötüdür.</item>
/// </list>
/// </summary>
public static class PanelCsv
{
    /// <summary>
    /// Tek dosyada dışa aktarılabilecek azami satır. Panelin en kalabalık listesi bile
    /// bunun altında; tavan, filtresiz bir dışa aktarmanın paneli boğmasını önlüyor.
    /// </summary>
    public const int MaxRows = 5000;

    /// <summary>Excel'in Türkçe yerelde beklediği ayraç.</summary>
    private const char Delimiter = ';';

    /// <summary>Bir sütun: başlık + satırdan değeri okuyan seçici.</summary>
    public sealed record Column<T>(string Header, Func<T, string?> Value);

    /// <summary>
    /// Satırları CSV'ye çevirip indirilebilir dosya olarak döndürür.
    /// Dosya adına tarih damgası eklenir — aynı listenin iki dışa aktarımı birbirini ezmesin.
    /// </summary>
    public static FileContentResult File<T>(
        IReadOnlyCollection<T> rows,
        IReadOnlyList<Column<T>> columns,
        string fileNamePrefix)
    {
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(Delimiter, columns.Select(c => Escape(c.Header))));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(Delimiter, columns.Select(c => Escape(c.Value(row)))));
        }

        // ⚠️ `UTF8Encoding(true)` — BOM'u YAZAN aşırı yükleme. `Encoding.UTF8.GetBytes`
        // BOM üretmez (yaygın karışıklık); o yolla yazılan dosya Excel'de bozuk görünür.
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            .GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(builder.ToString()))
            .ToArray();

        var stamp = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd_HHmm", CultureInfo.InvariantCulture);
        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = $"{fileNamePrefix}_{stamp}.csv"
        };
    }

    /// <summary>
    /// Filtrelenmiş listenin <b>tamamını</b> sayfalayarak toplar.
    /// </summary>
    /// <param name="fetchPage">(sayfa, sayfaBoyu) alıp o sayfayı getiren delege.</param>
    /// <returns>Satırlar ve sunucunun bildirdiği toplam sayı.</returns>
    /// <remarks>
    /// 🐛 <b>Bu metot bir tuzaktan doğdu.</b> İlk yazımda dışa aktarma tek istekle
    /// <c>Limit = 5000</c> gönderiyordu. Ama <c>Pagination.Clamp</c> (Faz 10.7, DoS koruması)
    /// panel sorgularını <c>AdminMaxLimit = 200</c>'e **kırpıyor** ve bunu sessizce yapıyor:
    /// istek 200 satır döndürür, <c>TotalCount</c> yine 4.000 der, CSV indirilir ve
    /// yönetici 200 satırı "tüm liste" sanır. Hata yok, uyarı yok — dışa aktarmanın
    /// verebileceği en kötü sonuç.
    ///
    /// Çözüm clamp'i gevşetmek DEĞİL (o koruma bilinçli): sayfa boyu clamp'in izin verdiği
    /// azami değere sabitlenip sayfalar sırayla dolaşılıyor. Böylece tavan
    /// <see cref="MaxRows"/> tarafından, DoS koruması ise <c>Pagination</c> tarafından
    /// bağımsız olarak korunuyor.
    ///
    /// ⚠️ Toplam <see cref="MaxRows"/>'u aşıyorsa <b>hiç sayfa dolaşılmaz</b> — çağıran
    /// zaten reddedecek, boşuna 5.000 satır çekmenin anlamı yok.
    /// </remarks>
    public static async Task<(IReadOnlyList<T> Rows, int TotalCount)> CollectAsync<T>(
        Func<int, int, Task<Application.Common.Models.PagedResult<T>>> fetchPage)
    {
        const int pageSize = Application.Common.Models.Pagination.AdminMaxLimit;

        var first = await fetchPage(1, pageSize);
        var total = first.TotalCount;

        if (total > MaxRows) return (Array.Empty<T>(), total);

        var rows = new List<T>(first.Items);
        var totalPages = first.TotalPages;

        for (var page = 2; page <= totalPages; page++)
        {
            var next = await fetchPage(page, pageSize);
            rows.AddRange(next.Items);
        }

        return (rows, total);
    }

    /// <summary>
    /// Tavan aşıldıysa Türkçe bir açıklama döndürür; aşılmadıysa <c>null</c>.
    /// Çağıran bunu <c>TempData["Error"]</c>'a yazıp listeye geri döner.
    /// </summary>
    public static string? RejectIfTooLarge(int totalCount) =>
        totalCount > MaxRows
            ? $"Dışa aktarma en fazla {MaxRows:N0} kayıt içerebilir; bu filtre {totalCount:N0} kayıt " +
              "döndürüyor. Lütfen filtreyi daraltın (tarih aralığı, durum ya da arama ekleyin)."
            : null;

    /// <summary>Tarihleri panelin her yerindeki biçimle yazar (Kadirli saati).</summary>
    public static string Date(DateTime? value) =>
        value is null ? "" : value.Value.AddHours(3).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Sayıyı Türkçe ondalık ayracıyla yazar — Excel aksi hâlde <c>1.5</c>'i tarih sanabilir.
    /// </summary>
    public static string Number(decimal? value) =>
        value?.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR")) ?? "";

    /// <summary>
    /// Bir hücreyi CSV kurallarına ve Excel'in formül davranışına göre güvenli hâle getirir.
    /// </summary>
    internal static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var cell = value;

        // Formül enjeksiyonu: Excel bu karakterlerle başlayan hücreyi FORMÜL sayar.
        // Tek tırnak öneki hücreyi metin olarak sabitler; Excel tırnağı göstermez.
        if (cell.Length > 0 && cell[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            cell = "'" + cell;
        }

        // RFC4180: ayraç, tırnak ya da satır sonu içeren hücre tırnaklanır; içteki tırnak ikilenir.
        if (cell.Contains(Delimiter) || cell.Contains('"') || cell.Contains('\n') || cell.Contains('\r'))
        {
            cell = "\"" + cell.Replace("\"", "\"\"") + "\"";
        }

        return cell;
    }
}
