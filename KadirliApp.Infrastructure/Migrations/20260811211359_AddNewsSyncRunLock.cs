using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <summary>
    /// Faz 12.13 — <b>aynı anda iki haber senkronu koşamaz</b> (kilit veritabanında).
    ///
    /// 🔴 <b>Neden Redis değil:</b> bu projede Redis bilinçli olarak <b>fail-open</b>
    /// (§7 madde 36) — erişilemediği an kilidi açar. Kilidin gerçekten gerektiği an ise tam
    /// olarak o an olabilir. §7 madde 32'nin dersi birebir: <i>"benzersiz indeks Api/Web
    /// yarışını yakalar."</i>
    ///
    /// 🔑 <b>Kısmi</b> unique indeks, sabit bir ifade (<c>(1)</c>) üzerinde ve yalnız
    /// <c>completed_at IS NULL</c> satırlarını kapsıyor: yani "en fazla bir tane çalışan koşu".
    /// Sabit bir kolon eklemek yerine ifade kullanıldı — eklenen kolon, "koşu bitince NULL'a
    /// çekilmesi gereken" ikinci bir alan olurdu ve unutulduğu an kilit sessizce kalıcılaşırdı.
    ///
    /// ⚠️ <b>Kalıcı kilit tehlikesi ve karşılığı:</b> süreç öldürülürse (deploy, OOM) satır
    /// sonsuza kadar <c>completed_at IS NULL</c> kalır ve bu indeks <b>bütün gelecek koşuları</b>
    /// engellerdi — hiçbir hata vermeden, yalnız haberler akmayı bırakarak. Karşılığı kodda:
    /// <c>NewsSyncService.ReapStuckRunsAsync</c> her koşunun ilk adımında 30 dakikayı aşan
    /// yarım koşuları <b>başarısız</b> olarak kapatır. Koruma ile kurtarma birlikte yazılmak
    /// zorundaydı; yalnız biri yazılsaydı elimizde ya yarış ya da kilitlenme kalırdı.
    /// </summary>
    public partial class AddNewsSyncRunLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // İndeks kurulmadan önce var olan yarım koşular kapatılır — biri bile kalsa
            // sorun olmaz ama ikisi kalırsa indeks OLUŞTURULAMAZ ve migration düşer.
            // 🔑 Silmiyoruz, kapatıyoruz: "bu koşuya ne oldu?" sorusunun cevabı panoda dursun.
            migrationBuilder.Sql(@"
                UPDATE news_sync_runs
                SET status = 'failed',
                    completed_at = NOW(),
                    error_message = COALESCE(error_message,
                        'Koşu yarıda kalmıştı; eşzamanlılık kilidi kurulurken kapatıldı.')
                WHERE completed_at IS NULL;");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ux_news_sync_runs_single_active
                ON news_sync_runs ((1))
                WHERE completed_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ux_news_sync_runs_single_active;");
        }
    }
}
