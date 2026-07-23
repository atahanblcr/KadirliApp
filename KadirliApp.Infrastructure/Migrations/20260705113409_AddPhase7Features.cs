using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase7Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "age",
                table: "death_notices");

            migrationBuilder.AddColumn<string>(
                name: "amenities",
                table: "places",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "condolence_latitude",
                table: "death_notices",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "condolence_longitude",
                table: "death_notices",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_file_id",
                table: "announcements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "announcements",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_name",
                table: "announcements",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "announcements",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_cover_image_id",
                table: "events",
                column: "cover_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_announcements_image_file_id",
                table: "announcements",
                column: "image_file_id");

            migrationBuilder.AddForeignKey(
                name: "fk_announcements_files_image_file_id",
                table: "announcements",
                column: "image_file_id",
                principalTable: "files",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_events_files_cover_image_id",
                table: "events",
                column: "cover_image_id",
                principalTable: "files",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_announcements_files_image_file_id",
                table: "announcements");

            migrationBuilder.DropForeignKey(
                name: "fk_events_files_cover_image_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_cover_image_id",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_announcements_image_file_id",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "amenities",
                table: "places");

            migrationBuilder.DropColumn(
                name: "condolence_latitude",
                table: "death_notices");

            migrationBuilder.DropColumn(
                name: "condolence_longitude",
                table: "death_notices");

            migrationBuilder.DropColumn(
                name: "image_file_id",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "location_name",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "announcements");

            migrationBuilder.AddColumn<int>(
                name: "age",
                table: "death_notices",
                type: "integer",
                nullable: true);
        }
    }
}
