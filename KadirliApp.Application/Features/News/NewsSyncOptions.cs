namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — alım ayarları. Değerler <c>appsettings</c>'ten (<c>News:*</c>) okunur ve
/// Infrastructure'da bağlanır; Application katmanı <c>IConfiguration</c> görmez (§1).
/// </summary>
public sealed class NewsSyncOptions
{
    /// <summary>
    /// 🔑 <b>Elimizde tutmak istediğimiz TOPLAM haber sayısı</b> — "arşiv derinliği" değil.
    /// <b>Başlangıç 50</b> (kullanıcının gerekçesi: <i>"biz bunu ilk başta test edeceğiz"</i>).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Adı 12.13'te değişti ve sebebi bir denetim bulgusu</b> (bulgu 11): eski ad
    /// <c>BackfillMaxPosts</c> ("arşive kaç haber inelim?") beklentiyi <b>yanlış</b>
    /// kuruyordu. Arşiv derinleştirmesi <c>kalan = ayar − TOPLAM haber sayısı</c> hesaplıyor,
    /// yani artımlı senkron yeni haber ekledikçe <b>arşiv sessizce sığlaşıyor</b>. Kod doğru,
    /// ad yanlıştı: *"derinliği 200 yaptım, 50 haber geldi"* sürprizinin kaynağı buydu.
    /// 📌 Davranış bilinçli olarak <b>değiştirilmedi</b>: tavan gerçekten toplam kayıt üzerinde
    /// olmalı, yoksa arşiv + artımlı birlikte sınırsız büyür.
    /// <para>
    /// 🔑 Değer <b>koddan değil yapılandırmadan</b> okunur (<c>News:Backfill:MaxTotalPosts</c>,
    /// eski anahtar <c>News:Backfill:MaxPosts</c> hâlâ okunur): yarın 500 ya da 2000 istenirse
    /// tek satır ayar değişir ve geri imleç <b>kaldığı yerden</b> devam eder.
    /// ⚠️ 27.284'ün tamamı istenirse ~273 istek + (aynalama ile) ~1,6 GB görsel demektir —
    /// o karar ayrıca verilmeli.
    /// </para>
    /// ⚠️ Mutabakat penceresi de bu sayıyı kullanır (§7 madde 56: pencere derinliğimizle aynı
    /// olmak zorunda). İkisi ayrışırsa "bizde yok" ile "kaynakta yok" karışır.
    /// </remarks>
    public int MaxTotalPosts { get; init; } = 50;

    /// <summary>Sayfa boyutu — WordPress tavanı 100.</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>
    /// Bir koşuda taranacak en fazla sayfa. Kaçak bir imleç (ya da kaynağın sıralamayı
    /// değiştirmesi) yüzünden koşunun <b>saatlerce</b> dönmesini engelleyen tavan.
    /// </summary>
    public int MaxPagesPerRun { get; init; } = 20;

    /// <summary>Görsel aynalanacak mı? Kapatıldığında haber yine iner, yalnız görselsiz.</summary>
    public bool MirrorImages { get; init; } = true;
}

/// <summary>
/// Faz 12.12 — <c>news_articles</c> kolon tavanları, <b>tek yerde</b>.
/// </summary>
/// <remarks>
/// 🔑 Sayılar EF yapılandırmasıyla (<c>NewsArticleConfiguration</c>) birebir aynı olmak
/// zorunda: burası daha gevşek olursa <c>SaveChanges</c> anında <b>bütün parti</b> düşer
/// (hata kayıt başına değil, batch başına doğar) ve o koşuda hiçbir haber inmez.
/// ⚠️ Kaynak bizim ama içeriğini biz yazmıyoruz — 500 karakterlik bir başlık bir gün gelir.
/// </remarks>
public static class NewsColumnLimits
{
    public const int Title = 500;
    public const int Excerpt = 1000;
    public const int Url = 1000;
}
