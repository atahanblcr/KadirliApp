using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <summary>
    /// Faz 12.13 — haber aramasının gerçek çıpası (12.12 sonrası denetim, bulgu 4).
    ///
    /// 🔴 12.12'de <c>ix_news_articles_source_title</c> "aramanın çıpası" diye konmuştu ama
    /// öyle çalışmıyordu: sorgu <c>lower(kolon) LIKE '%x%'</c> üretiyor ve bir <b>btree</b>
    /// indeksi bunu karşılayamıyor. Yani indeks vardı, yorum onu anlatıyordu ve <b>arama
    /// yine tam tarama yapıyordu</b> — 27k kayıt × ~2 KB düz metin ≈ 55 MB, her tuş vuruşunda.
    ///
    /// 🔬 <b>Ölçüm bir ayrıntıyı düzeltti (dürüst not):</b> denetim bulgusu sebebi
    /// <i>"<c>Contains</c> → <c>strpos</c>"</i> diye yazmıştı; <c>ToQueryString()</c> ile
    /// bakıldığında Npgsql'in <c>Contains</c>'i de <c>lower(...) LIKE @p</c> ürettiği görüldü.
    /// Sebep yanlıştı, <b>sonuç doğruydu</b>: eksik olan şey sorgunun şekli değil, bu
    /// migration'daki trigram indeksleriydi.
    ///
    /// 🔑 <c>pg_trgm</c> + GIN <b>ifade</b> indeksi <c>LIKE '%…%'</c>'i karşılayabilen tek
    /// yapı. İfade (<c>lower(...)</c>) sorgudakiyle <b>birebir</b> aynı olmak zorunda:
    /// ayrışırsa Postgres indeksi sessizce kullanmaz, hata da vermez.
    ///
    /// ⚠️ EF <c>gin_trgm_ops</c>'lu bir ifade indeksini modelleyemiyor → ham SQL. Bu yüzden
    /// koruma <c>AppDbContextModelSnapshot</c>'ta <b>görünmez</b>; bulgu 3'ün dersi burada da
    /// geçerli: bu indeksler yalnız migration'da yaşıyor.
    /// 📌 <c>pg_trgm</c> uzantısı 10.x'ten beri kurulu (<c>AddTrgmExtension</c>); yine de
    /// <c>IF NOT EXISTS</c> ile idempotent bırakıldı — boş bir veritabanına bu migration'ın
    /// tek başına uygulanabilmesi test kurulumlarında işe yarıyor.
    /// </summary>
    public partial class AddNewsSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_news_articles_source_title_trgm
                ON news_articles USING GIN (lower(source_title) gin_trgm_ops);");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_news_articles_title_override_trgm
                ON news_articles USING GIN (lower(title_override) gin_trgm_ops);");

            // ⚠️ En büyüğü bu: gövdenin düz metni. Aramanın kapsamını daraltmak (yalnız
            // başlıkta aramak) indeksi küçültürdü ama davranışı **sessizce** değiştirirdi —
            // dün bulunan bir haber bugün bulunamaz olurdu ve kimse sebebini bilmezdi.
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_news_articles_plain_text_trgm
                ON news_articles USING GIN (lower(source_plain_text) gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_news_articles_plain_text_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_news_articles_title_override_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_news_articles_source_title_trgm;");

            // ⚠️ Uzantı DÜŞÜRÜLMEZ: `ads` ve `places` de ona bağlı (AddTrgmExtension).
        }
    }
}
