using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsSyncStateSingletonGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🐛 EF varsayılanı 0 üretmişti ve bu KISITI SESSİZCE ETKİSİZ KILARDI:
            // var olan satır 0, yeni eklenen satır (C# tarafındaki `= 1`) 1 olurdu → iki
            // farklı değer, unique indeks çakışmaz, ikinci imleç satırı yine doğardı.
            // Varsayılan 1 ve var olan satır da 1'e çekiliyor.
            migrationBuilder.AddColumn<int>(
                name: "singleton",
                table: "news_sync_state",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE news_sync_state SET singleton = 1;");

            // Fazladan satır varsa (kısıt konmadan önce doğmuş olabilir) en eskisi kalır:
            // imleç geçmişini en çok o taşır. Aksi hâlde indeks oluşturulamaz.
            migrationBuilder.Sql(@"
                DELETE FROM news_sync_state
                WHERE id NOT IN (SELECT id FROM news_sync_state ORDER BY created_at LIMIT 1);");

            migrationBuilder.CreateIndex(
                name: "ix_news_sync_state_singleton",
                table: "news_sync_state",
                column: "singleton",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_news_sync_state_singleton",
                table: "news_sync_state");

            migrationBuilder.DropColumn(
                name: "singleton",
                table: "news_sync_state");
        }
    }
}
