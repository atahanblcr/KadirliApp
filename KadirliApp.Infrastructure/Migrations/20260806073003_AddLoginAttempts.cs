using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <summary>
    /// Faz 12.2 — iki iş bir arada:
    /// <list type="number">
    ///   <item><c>login_attempts</c> tablosu (şüpheli giriş günlüğü).</item>
    ///   <item><b>"staff" izin satırlarının temizliği.</b> Personel ekranı 12.2'de izin
    ///   matrisinden çıkarıldı (<c>PanelMenu</c> satırı <c>Module = null</c>) çünkü rol
    ///   kapısı yüzünden o yetki <b>hiçbir zaman çalışmıyordu</b> — 11.15b'nin kapattığı
    ///   "karşılığı olmayan yetki" hatasının hâlâ ayakta duran örneğiydi. Kalan satırlar
    ///   artık matriste görünmeyen ama veritabanında duran <b>ölü izinler</b> olurdu:
    ///   ekranda hiçbir kutu işaretli görünmez, DB'de kayıt vardır ve ikisi arasındaki
    ///   fark hiçbir yerde belli olmaz.</item>
    /// </list>
    /// </summary>
    public partial class AddLoginAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "login_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    identifier = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_suspicious = table.Column<bool>(type: "boolean", nullable: false),
                    suspicion_rule = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    alerted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_attempts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_identifier_created_at",
                table: "login_attempts",
                columns: new[] { "identifier", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_ip_address_created_at",
                table: "login_attempts",
                columns: new[] { "ip_address", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_is_suspicious_created_at",
                table: "login_attempts",
                columns: new[] { "is_suspicious", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_succeeded_created_at",
                table: "login_attempts",
                columns: new[] { "succeeded", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_user_id_created_at",
                table: "login_attempts",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_user_id_succeeded_ip_address",
                table: "login_attempts",
                columns: new[] { "user_id", "succeeded", "ip_address" });

            // ── "staff" izinlerinin temizliği (bkz. sınıf özeti) ─────────────────
            //
            // ⚠️ Bu bir veri silme işlemidir ve bilinçlidir: silinen satırlar, verildiği
            // andan beri HİÇBİR ŞEY YAPMAYAN izinlerdir. Rol kapısı
            // ([Authorize(Roles = "admin,super_admin")]) moderatörü zaten kapıda durduruyordu;
            // yani bu satırlar hiçbir moderatöre hiçbir erişim sağlamadı. Bırakılsalardı
            // ekranda görünmeyen ama DB'de duran ölü kayıtlara dönüşürlerdi.
            //
            // 🔑 `admin_permissions` panelin matrisi; `permissions`/`role_permissions` ise
            // Admin API'nin ([RequirePermission("staff", …)]) tarafı. API uçları duruyor ve
            // yalnız admin/super_admin'e açık — onlar izin matrisinden GEÇMİYOR (rol yeterli),
            // bu yüzden satırların silinmesi hiçbir API davranışını değiştirmez.
            migrationBuilder.Sql("DELETE FROM admin_permissions WHERE module = 'staff';");
            migrationBuilder.Sql(
                "DELETE FROM role_permissions WHERE permission_id IN " +
                "(SELECT id FROM permissions WHERE module = 'staff');");
            migrationBuilder.Sql("DELETE FROM permissions WHERE module = 'staff';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "login_attempts");

            // ⚠️ Silinen "staff" izinleri BİLEREK geri getirilmiyor: hangi kullanıcıya
            // hangi bayrakların verildiği bilgisi yok (ve olsaydı bile o izinler
            // çalışmayan izinlerdi). Geri alma, tabloyu düşürmekle sınırlı.
        }
    }
}
