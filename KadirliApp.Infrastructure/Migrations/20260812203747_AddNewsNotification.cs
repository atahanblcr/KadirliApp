using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔴 ELLE DÜZELTİLDİ — EF `announcement_id`'yi `notification_sent_by`'a RENAME
            // etmek istedi (ikisi de nullable uuid; sezgisel eşleştirme). Yeniden adlandırma
            // teknik olarak çalışırdı ama **niyeti gizler**: iki kolonun anlamı ilgisiz.
            // `announcement_id` 12.12'de *"haberin bildirimi bir DUYURU olarak açılır"*
            // varsayımıyla açılmıştı; 12.15 o yolu reddetti (haber, Duyurular listesinde de
            // görünürdü) ve kolon **hiç yazılmadı** — canlıda 56 satırın 56'sı NULL olduğu
            // ölçüldü. Bu yüzden düşürülüyor, taşınmıyor.
            migrationBuilder.DropColumn(
                name: "announcement_id",
                table: "news_articles");

            migrationBuilder.AddColumn<Guid>(
                name: "notification_sent_by",
                table: "news_articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "notification_campaign_id",
                table: "news_articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "notification_recipient_count",
                table: "news_articles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "notification_sent_at",
                table: "news_articles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_push_campaigns_news_source_id_unique",
                table: "push_campaigns",
                columns: new[] { "source", "source_id" },
                unique: true,
                filter: "source = 'news' AND source_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_notification_campaign_id",
                table: "news_articles",
                column: "notification_campaign_id");

            migrationBuilder.AddForeignKey(
                name: "fk_news_articles_push_campaigns_notification_campaign_id",
                table: "news_articles",
                column: "notification_campaign_id",
                principalTable: "push_campaigns",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_news_articles_push_campaigns_notification_campaign_id",
                table: "news_articles");

            migrationBuilder.DropIndex(
                name: "ix_push_campaigns_news_source_id_unique",
                table: "push_campaigns");

            migrationBuilder.DropIndex(
                name: "ix_news_articles_notification_campaign_id",
                table: "news_articles");

            migrationBuilder.DropColumn(
                name: "notification_campaign_id",
                table: "news_articles");

            migrationBuilder.DropColumn(
                name: "notification_recipient_count",
                table: "news_articles");

            migrationBuilder.DropColumn(
                name: "notification_sent_at",
                table: "news_articles");

            migrationBuilder.DropColumn(
                name: "notification_sent_by",
                table: "news_articles");

            // Geri alma, kolonu **boş** olarak geri koyar — hiç yazılmamıştı, dolayısıyla
            // kurtarılacak veri de yok.
            migrationBuilder.AddColumn<Guid>(
                name: "announcement_id",
                table: "news_articles",
                type: "uuid",
                nullable: true);
        }
    }
}
