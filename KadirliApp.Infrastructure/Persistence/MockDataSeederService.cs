using System.Text;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Infrastructure.Persistence;

/// <summary>
/// Faz 12.19a — <see cref="IMockDataSeeder"/> gerçeklemesi: <see cref="MockDataSeeder"/>'ı
/// sarar ve <b>ne yazdığını sayar</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Sayım neden sarmalayıcıda, seeder'ın içinde değil:</b> <c>MockDataSeeder</c>
/// 20 bloklu, 310 satırlık bir dosya ve her bloğu kendi <c>if (!await db.X.AnyAsync())</c>
/// kapısıyla korunuyor. Sayacı 20 bloğa dağıtmak, 21. blok eklendiğinde <b>sessizce
/// eksik kalan</b> bir rapor üretirdi (bu projenin "elle tutulan liste" tuzağının aynısı).
/// Önce/sonra <b>satır sayısı farkı</b> ise kapsamı EF modelinden türetir: yeni bir tablo
/// eklendiği gün rapora kendiliğinden girer.
/// </para>
/// <para>
/// ⚠️ <b>Sayım tek sorguda</b> (<c>UNION ALL</c>) yapılır, tablo başına bir sorgu ile değil:
/// modelde ~90 tablo var, iki turda 180 gidiş-dönüş demek olurdu.
/// </para>
/// <para>
/// 📌 <b>Görünüm (view) ve sahiplenilmiş (owned) tipler dışarıda:</b> <c>OwnsOne(...).ToJson()</c>
/// ile saklanan tipler kendi tablolarına sahip değil — sorguya girseler SQL geçersiz olurdu.
/// </para>
/// </remarks>
public sealed class MockDataSeederService : IMockDataSeeder
{
    private readonly AppDbContext _db;

    public MockDataSeederService(AppDbContext db) => _db = db;

    public async Task<MockDataSeedResult> SeedAsync(CancellationToken ct = default)
    {
        var before = await RowCountsAsync(ct);

        await MockDataSeeder.SeedAsync(_db);

        var after = await RowCountsAsync(ct);

        var written = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (table, count) in after)
        {
            var delta = count - before.GetValueOrDefault(table);
            if (delta > 0) written[table] = delta;
        }

        return new MockDataSeedResult(written);
    }

    /// <summary>Modeldeki her tablonun satır sayısı — <b>tek</b> sorguda.</summary>
    private async Task<Dictionary<string, int>> RowCountsAsync(CancellationToken ct)
    {
        var tables = _db.Model.GetEntityTypes()
            .Where(t => !t.IsOwned())
            .Select(t => (Schema: t.GetSchema() ?? "public", Table: t.GetTableName()))
            .Where(t => !string.IsNullOrEmpty(t.Table))
            .Distinct()
            .OrderBy(t => t.Table, StringComparer.Ordinal)
            .ToList();

        if (tables.Count == 0) return new Dictionary<string, int>(StringComparer.Ordinal);

        var sql = new StringBuilder();
        foreach (var (schema, table) in tables)
        {
            if (sql.Length > 0) sql.Append(" union all ");
            // Tablo/şema adları EF modelinden geliyor (kullanıcı girdisi değil); yine de
            // çift tırnak kaçışı yapılıyor — bir gün model adı elle yazılırsa diye.
            sql.Append($"select '{table!.Replace("'", "''")}' as table_name, count(*) as row_count from \"{schema.Replace("\"", "\"\"")}\".\"{table.Replace("\"", "\"\"")}\"");
        }

        var connection = _db.Database.GetDbConnection();
        var opened = connection.State != System.Data.ConnectionState.Open;
        if (opened) await _db.Database.OpenConnectionAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql.ToString();

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                counts[reader.GetString(0)] = checked((int)reader.GetInt64(1));

            return counts;
        }
        finally
        {
            if (opened) await _db.Database.CloseConnectionAsync();
        }
    }
}
