using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPushCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "campaign_id",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "push_campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_neighborhoods = table.Column<string>(type: "jsonb", nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_count = table.Column<int>(type: "integer", nullable: false),
                    sent_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    invalid_token_count = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_push_campaigns", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_campaign_id_fcm_sent",
                table: "notifications",
                columns: new[] { "campaign_id", "fcm_sent" });

            migrationBuilder.CreateIndex(
                name: "ix_push_campaigns_completed_at",
                table: "push_campaigns",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "ix_push_campaigns_created_at",
                table: "push_campaigns",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_push_campaigns_source_source_id",
                table: "push_campaigns",
                columns: new[] { "source", "source_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_notifications_push_campaigns_campaign_id",
                table: "notifications",
                column: "campaign_id",
                principalTable: "push_campaigns",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_notifications_push_campaigns_campaign_id",
                table: "notifications");

            migrationBuilder.DropTable(
                name: "push_campaigns");

            migrationBuilder.DropIndex(
                name: "ix_notifications_campaign_id_fcm_sent",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "campaign_id",
                table: "notifications");
        }
    }
}
