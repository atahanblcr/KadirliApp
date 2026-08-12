using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.News;

/// <summary>Bildirim gönderilebilir mi — gönderilemiyorsa <b>neden</b>.</summary>
public enum NewsNotifyEligibility
{
    /// <summary>Gönderilebilir.</summary>
    Sendable,

    /// <summary>Zaten gönderilmiş — <b>terminal</b> (§7 madde 37).</summary>
    AlreadySent,

    /// <summary>Yönetici yayından kaldırmış.</summary>
    Archived,

    /// <summary>Kaynakta bulunmuyor (<c>gone</c>).</summary>
    SourceGone,

    /// <summary>
    /// Kayıt yayında ama <b>dışlanmış bir kategorisi var</b> → uygulamada görünmüyor.
    /// </summary>
    CategoryExcluded
}

/// <summary>
/// Faz 12.15 — <b>"bu haberin bildirimi gönderilebilir mi?" sorusunun tek sahibi.</b>
/// </summary>
/// <remarks>
/// 🔴 <b>Neden ayrı bir sınıf:</b> aynı cevabı iki yer birden vermek zorunda — panelin
/// önizlemesi (butonu çizen/kapatan) ve komutun kendisi (gönderimi reddeden). İkisi ayrı
/// yazılsaydı 12.2b'nin dersi tekrarlanırdı: <i>görünüm kendi koşulunu yazarsa, komutun
/// reddedeceği bir buton çizilir</i> — ya da tersi, komutun kabul edeceği bir gönderim için
/// buton sebepsiz kapalı kalır ve yönetici sebebini hiçbir yerde bulamaz.
///
/// 🔑 <b>Dördüncü koşul (<see cref="NewsNotifyEligibility.CategoryExcluded"/>) planın
/// "arşivlenmemiş + kaynağı yayında" listesinde YOKTU ve eklenmesi zorunluydu:</b> haberin
/// görünmezliğinin <b>üç</b> ekseni var (§7 madde 58/59), plan yalnız ikisini sayıyor.
/// Dışlanmış kategorideki bir haber panelde "Yayında" görünür ama uygulamada <b>yoktur</b>;
/// bildirimi gönderilseydi vatandaş bildirimi alır, dokunur ve <b>boş sayfaya</b> düşerdi —
/// 11.15c'de duyurularda birebir yaşanan hasar (§7 madde 24).
/// ⚠️ Bu yüzden kural <c>NewsVisibility</c>'nin <b>kendisiyle</b> ölçülür (çağıran o sorguyu
/// koşar), burada yeniden yazılmaz: ikinci bir görünürlük tanımı §7 madde 23'ün sınıfı olurdu.
/// </remarks>
public static class NewsNotificationRules
{
    /// <param name="isVisibleToCitizens">
    /// <c>NewsVisibility.Published</c> sorgusunun bu kayıt için verdiği cevap — <b>çağıran
    /// ölçer</b>, burada yeniden tanımlanmaz.
    /// </param>
    public static NewsNotifyEligibility Evaluate(NewsArticle article, bool isVisibleToCitizens)
    {
        // Sıra anlamlıdır ve NewsStates.Of ile aynı mantığı izler: en "ortadan kalkması zor"
        // sebep önce söylenir. "Zaten gönderildi" hepsinden önce gelir çünkü o, kaydın
        // bugünkü durumundan bağımsız bir TARİHTİR — arşivlenmiş bir haberde "yayından
        // kaldırılmış" demek, yöneticiyi geri alıp tekrar denemeye iter ve o deneme de
        // reddedilirdi.
        if (article.NotificationSent) return NewsNotifyEligibility.AlreadySent;

        if (article.SourceState == NewsSourceStates.Gone) return NewsNotifyEligibility.SourceGone;
        if (article.IsArchived) return NewsNotifyEligibility.Archived;

        // Üçüncü görünmezlik ekseni: kayıt yayında, kaynağı yerinde, ama kategorisi dışlanmış.
        if (!isVisibleToCitizens) return NewsNotifyEligibility.CategoryExcluded;

        return NewsNotifyEligibility.Sendable;
    }

    /// <summary>
    /// Sebebin <b>yöneticiye söylenecek</b> hâli. Panel bunu buton yerine basar.
    /// </summary>
    /// <remarks>
    /// ⚠️ Her cümle <b>ne yapılacağını</b> da söylüyor. "Gönderilemez" demek, yöneticiyi
    /// sebebi tahmin etmeye bırakmaktır — bu bloğun savaştığı hasar sınıfı tam olarak
    /// "kimse sebebini bilmiyor".
    /// </remarks>
    public static string? Reason(NewsNotifyEligibility eligibility) => eligibility switch
    {
        NewsNotifyEligibility.Sendable => null,
        NewsNotifyEligibility.AlreadySent =>
            "Bu haberin bildirimi zaten gönderildi. Gönderilmiş bir bildirim geri alınamaz ve ikinci kez gönderilemez.",
        NewsNotifyEligibility.Archived =>
            "Haber yayından kaldırılmış. Bildirim göndermek için önce \"Yayına al\" deyin.",
        NewsNotifyEligibility.SourceGone =>
            "Haber kaynakta bulunmuyor; uygulamada görünmediği için bildirime dokunan kullanıcı boş sayfaya düşerdi.",
        NewsNotifyEligibility.CategoryExcluded =>
            "Haberin kategorilerinden biri uygulamada gizlenmiş; haber şu anda uygulamada görünmüyor. " +
            "Kategori görünürlüğünü Haber Kategorileri ekranından açabilirsiniz.",
        _ => "Bildirim gönderilemez."
    };
}
