using KadirliApp.Application.Features.PowerOutages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Infrastructure.Persistence;

/// <param name="Scanned">FK'sı boş olup taranan kayıt sayısı.</param>
/// <param name="Matched">Sözlüğe bağlanan kayıt sayısı.</param>
/// <param name="Unmatched">Bağlanamayan kayıtların serbest metinleri (tekilleştirilmiş, sıralı).</param>
public sealed record PowerOutageBackfillReport(int Scanned, int Matched, IReadOnlyList<string> Unmatched);

/// <summary>
/// Faz 12.3 — kesintilerin serbest metin mahallesini sözlüğe bağlayan <b>geri doldurma</b>.
/// </summary>
/// <remarks>
/// 🔴 <b>Migration içinde kör SQL ile yapılmadı</b> ve bu bilinçli. Üç sebebi var:
/// <list type="number">
///   <item>Eşleştirme kuralı <see cref="KadirliApp.Application.Common.Utils.SlugHelper"/>'a
///         dayanıyor — SQL'e kopyalansaydı Türkçe <c>'İ'</c> kuralının <b>ikinci bir
///         gerçeklemesi</b> doğardı (görünmez sözleşme #21'in tam olarak yasakladığı şey).</item>
///   <item>Migration bir kez koşar; sözlüğe <b>sonradan</b> eklenen bir mahalle eski kayıtları
///         asla kurtaramazdı. Bu adım her açılışta koşar ve yalnız FK'sı boş satırlara bakar.</item>
///   <item>🔑 <b>Eşleşmeyen kayıtları raporlaması gerekiyor.</b> Sessiz bir UPDATE, "kaç kesinti
///         hâlâ hedeflenemiyor?" sorusunu cevapsız bırakırdı; panel o sayıyı şerit olarak
///         gösteriyor.</item>
/// </list>
///
/// ⚠️ <b>Yalnız <c>neighborhood_id IS NULL</c> satırlara dokunur.</b> Yöneticinin panelden
/// bilerek kurduğu bir bağ, açılışta bir eşleştirme tahminiyle ezilemez.
/// </remarks>
public static class PowerOutageNeighborhoodBackfill
{
    public static async Task<PowerOutageBackfillReport> RunAsync(
        AppDbContext db, ILogger? logger = null, CancellationToken ct = default)
    {
        var pending = await db.PowerOutages
            .Where(o => o.NeighborhoodId == null && o.Neighborhood != null && o.Neighborhood != "")
            .ToListAsync(ct);

        if (pending.Count == 0)
            return new PowerOutageBackfillReport(0, 0, []);

        var dictionary = await db.Neighborhoods
            .Select(n => new NeighborhoodRef(n.Id, n.Name, n.Slug))
            .ToListAsync(ct);

        var matched = 0;
        var unmatched = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var outage in pending)
        {
            var hit = PowerOutageNeighborhoodMatcher.Match(outage.Neighborhood, dictionary);
            if (hit is not { } neighborhood)
            {
                unmatched.Add(outage.Neighborhood!);
                continue;
            }

            outage.NeighborhoodId = neighborhood.Id;
            // 🔑 Ad da sözlükten yeniden yazılır: bu andan itibaren alan TÜRETİLMİŞTİR.
            // "Cengiz Topel Mahallesi" → "Cengiz Topel". Mobilin ad üzerinden yaptığı
            // eşleşme (`matchesNeighborhood`) böylece kullanıcının profilindeki sözlük
            // adıyla birebir tutar — 12.3 öncesinde yazım farkı yüzünden tutmuyordu.
            outage.Neighborhood = neighborhood.Name;
            matched++;
        }

        if (matched > 0)
            await db.SaveChangesAsync(ct);

        var report = new PowerOutageBackfillReport(pending.Count, matched, [.. unmatched]);

        if (report.Matched > 0 || report.Unmatched.Count > 0)
        {
            logger?.LogInformation(
                "Kesinti mahalle geri doldurma: {Scanned} tarandı, {Matched} eşleşti, {UnmatchedCount} farklı ad eşleşmedi ({Unmatched}).",
                report.Scanned, report.Matched, report.Unmatched.Count, string.Join(" · ", report.Unmatched));
        }

        return report;
    }
}
