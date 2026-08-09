using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportFieldModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "operating_days",
                table: "intercity_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 127);

            migrationBuilder.AddColumn<Guid>(
                name: "departure_point_id",
                table: "intercity_routes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_type",
                table: "intercity_routes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "bus");

            migrationBuilder.CreateTable(
                name: "transport_departure_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transport_departure_points", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_intercity_routes_departure_point",
                table: "intercity_routes",
                column: "departure_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_intercity_routes_vehicle_type",
                table: "intercity_routes",
                column: "vehicle_type");

            migrationBuilder.CreateIndex(
                name: "ix_transport_departure_points_is_active_display_order",
                table: "transport_departure_points",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_transport_departure_points_slug",
                table: "transport_departure_points",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_intercity_routes_transport_departure_points_departure_point",
                table: "intercity_routes",
                column: "departure_point_id",
                principalTable: "transport_departure_points",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_intercity_routes_transport_departure_points_departure_point",
                table: "intercity_routes");

            migrationBuilder.DropTable(
                name: "transport_departure_points");

            migrationBuilder.DropIndex(
                name: "ix_intercity_routes_departure_point",
                table: "intercity_routes");

            migrationBuilder.DropIndex(
                name: "ix_intercity_routes_vehicle_type",
                table: "intercity_routes");

            migrationBuilder.DropColumn(
                name: "operating_days",
                table: "intercity_schedules");

            migrationBuilder.DropColumn(
                name: "departure_point_id",
                table: "intercity_routes");

            migrationBuilder.DropColumn(
                name: "vehicle_type",
                table: "intercity_routes");
        }
    }
}
