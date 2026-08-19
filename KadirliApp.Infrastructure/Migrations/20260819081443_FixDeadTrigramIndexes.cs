using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <summary>
    /// Faz 12.22b — <b>ölçümün gösterdiği tek gerçek indeks hatası:</b> iki trigram indeksi
    /// vardı, yer kaplıyordu, her yazmada güncelleniyordu ve <b>hiçbir sorgu tarafından
    /// kullanılmıyordu.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Sebep 12.13'ün bulduğu hatanın birebir aynısı, yalnız bir katman ötede.</b>
    /// 12.13 <i>"btree <c>LIKE '%x%'</c>'i karşılayamaz"</i> demişti ve çözümü GIN/trigram
    /// olmuştu. Ama Haziran 2026'da (<c>AddTrgmExtension</c>) konan iki GIN indeksi
    /// <b>ham kolon</b> üzerineydi:
    /// <code>
    /// CREATE INDEX ix_ads_title_trgm ON ads USING GIN (title gin_trgm_ops);
    /// </code>
    /// Oysa sorgu <c>x.Title.ToLower().Contains(...)</c> yazıyor, yani Postgres'e
    /// <c>lower(title) LIKE '%…%'</c> gidiyor. İfade indeksinde ifade <b>birebir</b>
    /// eşleşmek zorundadır: <c>title</c> ≠ <c>lower(title)</c> → indeks <b>sessizce</b>
    /// kullanılmaz. Ne hata, ne uyarı, ne log.
    /// </para>
    /// <para>
    /// 🔬 <b>Ölçüldü (12.22c bozma turu, 20.005 satırlık <c>ads</c>):</b> arama sorgusu
    /// <c>Seq Scan</c> yapıyordu — <i>Rows Removed by Filter: 19.994</i>, 29 ms. Yani indeks
    /// yalnız işe yaramıyordu; <b>işe yaradığı sanılıyordu</b>, ki bu daha kötüdür — arama
    /// yavaşladığında ilk bakılacak yer "indeks var mı?" olur ve cevap "var" çıkar.
    /// </para>
    /// <para>
    /// 🔑 <b>İlan aramasında İKİ indeks birden şart, biri yetmez.</b> Sorgu
    /// <c>lower(title) LIKE … OR lower(description) LIKE …</c> biçiminde; Postgres bir
    /// <c>BitmapOr</c> kurabilmek için <b>OR'un her iki tarafında</b> indeks ister. Yalnız
    /// başlık indekslenseydi planlayıcı yine tam tarama seçerdi ve bu migration hiçbir şey
    /// değiştirmemiş olurdu — ölçülmeseydi "düzelttik" denip geçilecek tam olarak bu.
    /// (12.13'ün <c>source_title</c> + <c>source_plain_text</c> çiftini yazma gerekçesinin
    /// aynısı.)
    /// </para>
    /// <para>
    /// 📌 <b>Kapsam bilinçli olarak dar.</b> Projede 14 sorgu daha <c>lower(…) LIKE</c>
    /// yapıyor (rehber · vefat · taksi · ulaşım · işletme · global arama · hata kayıtları)
    /// ve <b>hiçbirinde trigram indeksi yok</b>. Onlara indeks eklemek bu migration'ın işi
    /// değil: burada düzeltilen şey <i>"var olan ama ölü"</i> bir yapı: bedeli zaten
    /// ödeniyor, karşılığı alınmıyor. Yeni indeks eklemek ise ölçülmemiş bir karardır ve bu
    /// faz ölçülmemiş kararı reddediyor (bkz. <c>Memory_Bank/Performance_Baseline.md</c>).
    /// </para>
    /// <para>
    /// ⚠️ EF <c>gin_trgm_ops</c>'lu ifade indeksini modelleyemez → ham SQL, snapshot'ta
    /// <b>görünmez</b> (12.13'ün uyarısı burada da geçerli).
    /// </para>
    /// </remarks>
    public partial class FixDeadTrigramIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // ── İlanlar ────────────────────────────────────────────────────────
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ads_title_trgm;");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_ads_title_trgm
                ON ads USING GIN (lower(title) gin_trgm_ops);");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_ads_description_trgm
                ON ads USING GIN (lower(description) gin_trgm_ops);");

            // ── Mekanlar ───────────────────────────────────────────────────────
            // Tek kolon aranıyor (`x.Name`), OR yok → tek indeks yeterli.
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_places_name_trgm;");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_places_name_trgm
                ON places USING GIN (lower(name) gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 📌 Geri alma, indeksleri Haziran 2026'daki (ÖLÜ) hâllerine döndürür —
            // "geri al" demek "düzeltmeden önceki duruma dön" demektir, "sil" demek değil.
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ads_description_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ads_title_trgm;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_ads_title_trgm ON ads USING GIN (title gin_trgm_ops);");

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_places_name_trgm;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_places_name_trgm ON places USING GIN (name gin_trgm_ops);");
        }
    }
}
