using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.12 — <b>iki imlecin tek satırlık evi</b> (<c>news_sync_state</c>, her zaman bir satır).
///
/// 🔴 <b>Neden ayrı bir tablo, neden verinin kendisinden türetilmiyor?</b>
/// İleri imleç <c>MAX(source_modified_at)</c>'ten türetilebilirdi; ama geri imlecin
/// "kaynak tükendi mi?" bilgisinin (<see cref="ArchiveCompleted"/>) veride hiçbir izi yok
/// ve türetilmiş bir imleç, kayıtların silinmesi/arşivlenmesiyle sessizce geri kayardı.
/// </summary>
public class NewsSyncState : BaseEntity
{
    /// <summary>
    /// 🐛 <b>Tek satır garantisinin veritabanı ayağı</b> (12.12 sonrası denetim bulgusu).
    /// Değeri her zaman <c>1</c>; üzerindeki <b>unique indeks</b> ikinci bir satırın
    /// açılmasını imkânsız kılar.
    /// </summary>
    /// <remarks>
    /// Neden gerekti: <c>SyncNewsJob</c> (15 dakikada bir, yani 03:00'te de koşar) ile
    /// <c>ReconcileNewsJob</c> (03:00) <b>boş durumda aynı anda</b> başlarsa ikisi de satırı
    /// bulamayıp kendi satırını açardı. <c>DisableConcurrentExecution</c> yalnız <b>aynı</b>
    /// işi korur, iki farklı işi değil. O andan sonra <c>FirstOrDefault</c> rastgele birini
    /// seçer ve ileri imleç koşular arasında <b>ileri-geri zıplar</b>: aradaki haberler
    /// atlanır, hiçbir hata oluşmaz, panelde hiçbir belirti olmaz.
    /// </remarks>
    public int Singleton { get; init; } = 1;

    /// <summary>
    /// İleri imleç: bu ana kadar olan değişiklikleri aldık (UTC, <c>modified_gmt</c>'den).
    /// <c>null</c> = hiç koşmadık → artımlı iş önce <b>arşiv derinleştirmesine</b> düşer.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>UTC saklanır, sorguya site-yerel gider</b> (<c>WordPressTimeWindow</c>).
    /// Ham UTC damgasını <c>modified_after</c>'a yazmak pencereyi 3 saat <b>geriye</b> kaydırır
    /// (zararsız: upsert idempotent); ters yön her koşuda <b>3 saatlik haberi sessizce atlar</b>.
    /// </remarks>
    public DateTime? ForwardCursorUtc { get; set; }

    /// <summary>
    /// Geri imleç: arşivde <b>bu tarihe kadar</b> indik (elimizdeki en eski haberin yayın anı, UTC).
    /// Bir sonraki derinleştirme <c>before=&lt;bu tarih&gt;</c> ile devam eder.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Neden sayfa numarası değil tarih:</b> <c>orderby=date&amp;order=desc</c> akışında
    /// sayfa numarasıyla ilerlemek, koşular arasında <b>yeni bir haber yayınlandığı anda</b>
    /// bütün sayfaları bir kaydırır — o kayma yüzünden tam sınırdaki haber hiçbir sayfada
    /// görünmez ve <b>sonsuza kadar atlanır</b>. Hiçbir hata oluşmaz; yalnız bir haber eksiktir.
    /// Tarih imleci bu sınıfa kapalıdır ve <c>News:Backfill:MaxPosts</c> büyütülünce iş yine
    /// <b>kaldığı yerden</b> devam eder.
    /// <para>
    /// ⚠️ Aynı alan <b>mutabakat penceresinin tabanıdır</b>: iş yalnız derinliğimiz kadarını
    /// tarar. Pencere derinlikle aynı olmazsa "bizde yok" ile "kaynakta yok" karışır ve
    /// <b>her eski haber <c>gone</c> işaretlenir</b>.
    /// </para>
    /// </remarks>
    public DateTime? ArchiveCursorUtc { get; set; }

    /// <summary>Kaynak tükendi (derinleştirme daha eski haber bulamadı) — boşuna istek atılmasın.</summary>
    public bool ArchiveCompleted { get; set; }

    /// <summary>Son <b>başarılı</b> koşunun bitiş anı — bayatlık göstergesinin tek kaynağı (<c>NewsSyncHealth</c>).</summary>
    public DateTime? LastSuccessfulRunAt { get; set; }

    /// <summary>Son koşunun kimliği (panelde "son koşu" bağlantısı — 12.13).</summary>
    public Guid? LastRunId { get; set; }
}
