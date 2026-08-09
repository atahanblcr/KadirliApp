using KadirliApp.Application.Features.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Persistence;

/// <param name="Scanned">İlçesi boş olup taranan etkinlik sayısı.</param>
/// <param name="Matched">Ev ilçesine bağlanan etkinlik sayısı.</param>
/// <param name="HomeDistrictMissing">Sözlükte ev ilçesi bulunamadı — hiçbir şey yazılmadı.</param>
public sealed record EventDistrictBackfillReport(int Scanned, int Matched, bool HomeDistrictMissing);

/// <summary>
/// Faz 12.4 — ilçesi olmayan etkinlikleri <b>Kadirli</b>'ye bağlayan geri doldurma.
/// </summary>
/// <remarks>
/// 🔑 <b>Varsayım neden doğru:</b> panelde 12.4'e kadar ilçe alanı <i>hiç yoktu</i> — form
/// <c>Event.City</c>'yi bile göstermiyordu. Yani var olan her etkinlik Kadirli'de yapılmış
/// bir etkinliktir; başka bir şey girilmesi mümkün değildi.
///
/// ⚠️ <b>Bu varsayımın 12.4'ten SONRA geçerli kalmasının şartı:</b> ilçesi boş yeni bir kayıt
/// doğmamalı. İki kapı bunu tutuyor: komut ilçeyi <b>zorunlu</b> doğrular
/// (<c>EventDistrictResolver</c>) ve sözlükte <b>silme yoktur</b> (10.9(d) lookup kararı) —
/// yani FK'nin <c>SetNull</c> davranışı pratikte hiç tetiklenmez. Bu iki kapı olmasaydı,
/// yöneticinin bilerek boş bıraktığı bir kayıt her açılışta sessizce "Kadirli" olurdu.
///
/// 🔴 <b>Migration içinde kör SQL ile yapılmadı</b> (12.3 dersi): ev ilçesinin kimliği
/// <c>DistrictDefaults.HomeSlug</c>'dan çözülüyor ve <c>IsLocal</c> türetmesi uygulama kodunda;
/// SQL'e kopyalansaydı ikinci bir gerçekleme doğardı.
/// </remarks>
public static class EventDistrictBackfill
{
    public static async Task<EventDistrictBackfillReport> RunAsync(
        AppDbContext db, ILogger? logger = null, CancellationToken ct = default)
    {
        // ⚠️ Global query filter (deleted_at IS NULL) bilerek AÇIK bırakıldı: silinmiş bir
        // etkinlik çöp kutusundan geri getirilebilir ve geri geldiğinde ilçesiz olur.
        // Bu yüzden silinmişler de taranır.
        var pending = await db.Events
            .IgnoreQueryFilters()
            .Where(e => e.DistrictId == null)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return new EventDistrictBackfillReport(0, 0, false);

        var home = await db.Districts
            .FirstOrDefaultAsync(d => d.Slug == DistrictDefaults.HomeSlug, ct);

        if (home is null)
        {
            // Sözlük henüz kurulmamış — sessiz kalmak yanlış olurdu: "kaç etkinlik hâlâ
            // konumsuz?" sorusu cevapsız kalır ve panel boş bir ilçe sütunu gösterir.
            logger?.LogWarning(
                "Etkinlik ilçe geri doldurma atlandı: sözlükte ev ilçesi ({Slug}) yok, {Count} etkinlik konumsuz kaldı.",
                DistrictDefaults.HomeSlug, pending.Count);
            return new EventDistrictBackfillReport(pending.Count, 0, true);
        }

        foreach (var ev in pending)
        {
            ev.DistrictId = home.Id;
            // IsLocal türetilmiş alandır ve ev ilçesi tanım gereği yereldir.
            ev.IsLocal = true;
        }

        await db.SaveChangesAsync(ct);

        logger?.LogInformation(
            "Etkinlik ilçe geri doldurma: {Scanned} tarandı, {Matched} kayıt {District} ilçesine bağlandı.",
            pending.Count, pending.Count, home.Name);

        return new EventDistrictBackfillReport(pending.Count, pending.Count, false);
    }
}
