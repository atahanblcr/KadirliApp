using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerOutageNeighborhood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "area_detail",
                table: "power_outages",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "neighborhood_id",
                table: "power_outages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_power_outages_neighborhood_id",
                table: "power_outages",
                column: "neighborhood_id");

            migrationBuilder.AddForeignKey(
                name: "fk_power_outages_neighborhoods_neighborhood_id",
                table: "power_outages",
                column: "neighborhood_id",
                principalTable: "neighborhoods",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_power_outages_neighborhoods_neighborhood_id",
                table: "power_outages");

            migrationBuilder.DropIndex(
                name: "ix_power_outages_neighborhood_id",
                table: "power_outages");

            migrationBuilder.DropColumn(
                name: "area_detail",
                table: "power_outages");

            migrationBuilder.DropColumn(
                name: "neighborhood_id",
                table: "power_outages");
        }
    }
}
