using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <summary>
    /// Faz 12.15b — <c>notification_preferences</c> JSON'una <c>News</c> anahtarını ekler.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Bu migration bir "temizlik" değil, ZORUNLU.</b> Tercihler
    /// <c>OwnsOne(...).ToJson()</c> ile tek bir JSON kolonda saklanıyor ve <b>ölçüldü</b>
    /// (13 satırın 13'ünde anahtar yoktu): EF'in JSON materyalizasyonu <b>varsayılan
    /// başlatıcıyı ÇALIŞTIRMIYOR</b> — <c>public bool News { get; set; } = true;</c> yazmış
    /// olmamıza rağmen anahtarsız bir JSON <c>false</c> olarak okunuyor.
    ///
    /// Yani bu migration olmadan 12.15b, <b>mevcut bütün kullanıcıları haber bildiriminden
    /// sessizce çıkarırdı</b>: uçlar 200 döner, panel kampanya satırını yine açar, hiçbir
    /// hata oluşmaz — tek belirti "kimse haber bildirimi almıyor" olurdu ve sebebi hiçbir
    /// yerde yazmazdı. Hata bir varsayımdan doğacaktı (<i>"başlatıcı çalışır herhâlde"</i>)
    /// ve <c>NotificationPreferenceAxisTests.MissingJsonKey_MaterialisesAsFalse</c> onu
    /// ölçtüğü için yakalandı.
    ///
    /// ⚠️ Yön: <c>'{"News": true}' || mevcut</c> — sağdaki operand çakışmada <b>kazanır</b>,
    /// yani zaten anahtarı olan bir satırın <b>açık tercihi ezilmez</b>. <c>WHERE</c> ile
    /// birlikte adım <b>idempotent</b>.
    ///
    /// ⚠️ Ham SQL burada doğru araç (12.3'ün "migration'da kör SQL yazma" kuralının
    /// istisnası): eşleştirilecek bir iş kuralı yok, yapılan şey bir <b>şema tamamlama</b> —
    /// eksik bir JSON anahtarına, varlığın zaten beyan ettiği varsayılanı yazmak.
    /// </remarks>
    public partial class BackfillNewsNotificationPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE users
                SET notification_preferences = '{"News": true}'::jsonb || notification_preferences
                WHERE NOT (notification_preferences ? 'News');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE users SET notification_preferences = notification_preferences - 'News';");
        }
    }
}
