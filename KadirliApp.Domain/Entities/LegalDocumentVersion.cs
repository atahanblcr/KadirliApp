using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.16 — bir hukuki belgenin <b>belirli bir hâli</b>. Rıza kaydı belgeye değil
/// <b>buna</b> bağlanır (§7 madde 71).
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>YAYINLANMIŞ SÜRÜM DEĞİŞTİRİLEMEZ</b> (§7 madde 72) ve bu kural iki katmanda birden
/// yaşıyor:
/// <list type="number">
///   <item><b>Derleyici:</b> içerik alanları <c>init</c>, yani dışarıdan
///         <c>version.Body = "…"</c> yazmak <b><c>CS8852</c></b>'dir. 12.11'in dersi:
///         tek sahiplik bir <i>taramaya</i> değil derleyiciye emanet edilir.</item>
///   <item><b>Varlığın kendisi:</b> tek yazma kapısı <see cref="TryRevise"/> ve o kapı
///         yayınlanmış sürümde <c>false</c> döner. Derleyici "kim yazabilir"i, bu kapı
///         "ne zaman yazılabilir"i tutar.</item>
/// </list>
/// Değiştirilebilseydi bütün geçmiş rıza kayıtları <b>retroaktif olarak</b> başka bir metni
/// işaret ederdi ve hiçbir yerde hata görünmezdi — bu bloğun var olma sebebi tam olarak budur.
/// </para>
/// <para>
/// 🔴 <b>Aynı anda en fazla BİR yayında sürüm</b> olabilir ve bu kural asıl olarak
/// <b>veritabanında</b> yaşıyor: <c>legal_document_versions</c> üzerinde
/// <c>(document_id) WHERE published_at IS NOT NULL AND superseded_at IS NULL</c> kısmi unique
/// indeksi (§7 madde 60'ın deseni). Koddaki sıra doğru olsa bile <b>iki eşzamanlı yayınlama
/// isteği</b> yarışı kazanamaz: ikinci INSERT/UPDATE veritabanında reddedilir. Kilit kodda
/// kalsaydı iki "yayında" sürüm doğar ve kayıt ekranı hangisini soracağını bilemezdi.
/// </para>
/// </remarks>
public class LegalDocumentVersion : BaseEntity
{
    private int _versionNumber;
    private string _body = default!;
    private string? _summary;
    private bool _requiresReconsent;
    private DateTime _effectiveFrom;
    private DateTime? _publishedAt;
    private Guid? _publishedBy;
    private DateTime? _supersededAt;

    public Guid DocumentId { get; set; }

    /// <summary>1'den başlar, belge içinde <b>unique</b>. Kullanıcıya "v3" olarak gösterilir.</summary>
    public int VersionNumber { get => _versionNumber; init => _versionNumber = value; }

    /// <summary>Metnin kendisi (HTML). ⚠️ Panelden geliyor, temizliği <c>NewsHtmlPolicy</c> ile aynı sahiptedir.</summary>
    public string Body { get => _body; init => _body = value; }

    /// <summary>Kayıt ekranında kutunun yanında görünen tek cümle ("Kişisel verilerimin işlenmesini kabul ediyorum").</summary>
    public string? Summary { get => _summary; init => _summary = value; }

    /// <summary>
    /// 🔑 Bu sürüm <b>yeniden onay</b> gerektiriyor mu? Karar panelde verilir, kodda türetilmez.
    /// </summary>
    /// <remarks>
    /// Türetilemez çünkü fark <b>anlamdadır</b>: bir yazım hatası düzeltmesi ile işleme
    /// kapsamının genişlemesi metin olarak birbirine benzeyebilir. Bayrak olmasaydı iki
    /// kötü seçenekten biri kalırdı: ya her düzeltmede <b>bütün şehri</b> yeniden onay
    /// ekranına düşürürdük, ya da esaslı bir kapsam değişikliği <b>hiç kimseye</b> ulaşmazdı.
    /// </remarks>
    public bool RequiresReconsent { get => _requiresReconsent; init => _requiresReconsent = value; }

    /// <summary>Metnin yürürlük tarihi — <see cref="PublishedAt"/>'ten farklı olabilir (metnin kendi üstünde yazan tarih).</summary>
    public DateTime EffectiveFrom { get => _effectiveFrom; init => _effectiveFrom = value; }

    /// <summary><c>null</c> ise <b>taslak</b>: hiçbir public uçta görünmez, rıza bağlanamaz.</summary>
    public DateTime? PublishedAt { get => _publishedAt; init => _publishedAt = value; }

    public Guid? PublishedBy { get => _publishedBy; init => _publishedBy = value; }

    /// <summary>
    /// Yerine yeni bir sürüm yayınlandığında dolar. ⚠️ Satır <b>silinmez</b>: ona bağlı rıza
    /// kayıtları hâlâ onu işaret ediyor ve o metin kanıtın kendisidir.
    /// </summary>
    public DateTime? SupersededAt { get => _supersededAt; init => _supersededAt = value; }

    /// <summary>Şu anda yürürlükte olan sürüm mü? (Kısmi unique indeksin tuttuğu koşulun kod aynası.)</summary>
    public bool IsLive => _publishedAt is not null && _supersededAt is null;

    /// <summary>Henüz yayınlanmamış mı? Yalnız taslak düzenlenebilir.</summary>
    public bool IsDraft => _publishedAt is null;

    // Navigation
    public LegalDocument? Document { get; set; }

    /// <summary>
    /// Taslağı düzenler. <b>Yayınlanmış sürümde hiçbir şey yapmaz ve <c>false</c> döner</b>
    /// (§7 madde 72) — çağıran bunu kullanıcıya <b>söylemek</b> zorundadır.
    /// </summary>
    /// <remarks>
    /// 🔑 Sessizce yutmak yerine <c>false</c> dönmesi bilinçli: "kaydettim" deyip
    /// kaydetmemek, hiç kaydetmemekten kötüdür (§7 madde 37'nin dersi). Panelin formu
    /// yayınlanmış sürümü zaten salt-okunur gösterir; bu kapı <b>ikinci</b> savunmadır.
    /// </remarks>
    public bool TryRevise(string body, string? summary, bool requiresReconsent, DateTime effectiveFrom)
    {
        if (!IsDraft) return false;

        _body = body;
        _summary = summary;
        _requiresReconsent = requiresReconsent;
        _effectiveFrom = effectiveFrom;
        return true;
    }

    /// <summary>
    /// Taslağı yayına alır. Zaten yayınlanmışsa <c>false</c> döner ve kaydı
    /// <b>değiştirmez</b> (<c>NewsArticle.MarkNotificationSent</c> deseni — terminal geçiş).
    /// </summary>
    public bool Publish(Guid publishedBy, DateTime now)
    {
        if (!IsDraft) return false;

        _publishedAt = now;
        _publishedBy = publishedBy;
        return true;
    }

    /// <summary>
    /// Yürürlükten kaldırır (yerine yeni sürüm geldi). Yayında değilse <c>false</c> döner.
    /// </summary>
    /// <remarks>
    /// ⚠️ Çağıranın işlemine katılır — yeni sürümün <see cref="Publish"/>'i ile bu çağrı
    /// <b>aynı <c>SaveChanges</c>'te</b> olmak zorunda: ayrı yazılsalardı araya düşen bir
    /// hata iki yayında sürüm bırakırdı ve kısmi unique indeks o yazmayı reddederken
    /// yönetici sebebini hiçbir yerde göremezdi.
    /// </remarks>
    public bool Supersede(DateTime now)
    {
        if (!IsLive) return false;

        _supersededAt = now;
        return true;
    }
}
