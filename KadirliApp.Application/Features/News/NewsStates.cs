using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.13 — <b>bir haberin panelde görünen durumu</b>, tek yerde.
/// </summary>
/// <remarks>
/// 🔑 Bu modülde moderasyon <b>yok</b>: durum bir iş akışının adımı değil, iki bağımsız
/// olgudan <b>türetilmiş</b> bir cevaptır — "vatandaş bunu neden görmüyor?".
/// <list type="bullet">
///   <item><b>gone</b> — kaynakta artık yok (<c>ReconcileNewsJob</c>).</item>
///   <item><b>archived</b> — yönetici gizledi (geri alınabilir).</item>
///   <item><b>published</b> — yayında.</item>
/// </list>
///
/// 🔴 <b>Sıra önemli ve <c>gone</c> önce gelir.</b> Hem arşivlenmiş hem kaynaktan kalkmış bir
/// kayıtta "Yayından kaldırıldı" yazmak yöneticiyi <b>yanlış işe</b> yönlendirir: "Geri al"a
/// basar, kayıt yine görünmez ve sebebi ekranda hiçbir yerde yazmaz
/// (<c>UnarchiveNewsArticleCommand</c> bunu zaten söylüyor: geri alma kaynağın durumuna
/// dokunmaz). Görünen sebep, <b>ortadan kalkması daha zor</b> olan sebep olmalı.
///
/// ⚠️ Durum <b>kolonda saklanmaz</b>. Saklansaydı üçüncü bir yazma yolu doğardı ve 12.10'un
/// bütün dersi bunun üzerineydi; ayrıca kategori dışlaması gibi <i>kayıt dışı</i> bir sebeple
/// görünmeyen haberde kolon yalan söylerdi.
/// </remarks>
public static class NewsStates
{
    public const string Published = "published";
    public const string Archived = "archived";
    public const string Gone = "gone";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Published, Archived, Gone };

    public static string Of(bool isArchived, string sourceState) =>
        sourceState == NewsSourceStates.Gone ? Gone
        : isArchived ? Archived
        : Published;
}
