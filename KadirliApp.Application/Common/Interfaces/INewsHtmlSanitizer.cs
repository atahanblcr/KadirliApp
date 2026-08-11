namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Faz 12.12 — kaynaktan gelen HTML'in temizleyicisi.
///
/// 🔴 <b>Temizlik ALIM ANINDA yapılır, gösterim anında değil.</b> Gösterim anında
/// temizlemek her tüketiciye (mobil, panel, yarın bir web) aynı işi tekrar yaptırmak ve
/// birinin unutmasına açık bırakmak olurdu; veritabanında temiz olmayan bir gövde durduğu
/// sürece <b>her</b> yeni ekran yeni bir XSS yüzeyidir.
///
/// ⚠️ Yine de tek kapı değil: temizlenmiş HTML panelde <c>@Html.Raw</c> ile <b>basılmaz</b>
/// (§7 madde 33, checklist §11) ve istemci ikinci bir beyaz liste yazmaz.
///
/// Beyaz listenin kendisi Application'da (<c>NewsHtmlPolicy</c>) yaşar: hangi etiketin
/// kalacağı bir <b>ürün kararı</b>dır, kütüphanenin varsayılanı değil.
/// </summary>
public interface INewsHtmlSanitizer
{
    /// <summary>Beyaz listeye indirger. Girdi ne olursa olsun <b>fırlatmaz</b>.</summary>
    string Sanitize(string? html);

    /// <summary>
    /// Etiketsiz metin — arama ve <b>özet yedeği</b> için.
    /// WordPress <c>excerpt</c>'i HTML parçalı geldiği için özet doğrudan kullanılamıyor.
    /// </summary>
    string ToPlainText(string? html);
}
