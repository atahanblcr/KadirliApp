using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDistricts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "district_id",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    province_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_center = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_districts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_events_district",
                table: "events",
                column: "district_id");

            migrationBuilder.CreateIndex(
                name: "ix_districts_province_name_display_order",
                table: "districts",
                columns: new[] { "province_name", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_districts_slug",
                table: "districts",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_events_districts_district_id",
                table: "events",
                column: "district_id",
                principalTable: "districts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_events_districts_district_id",
                table: "events");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropIndex(
                name: "ix_events_district",
                table: "events");

            migrationBuilder.DropColumn(
                name: "district_id",
                table: "events");
        }
    }
}
