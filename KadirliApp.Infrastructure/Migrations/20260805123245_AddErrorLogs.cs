using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "error_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    stack_trace = table.Column<string>(type: "text", nullable: true),
                    path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    trace_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    app_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    os_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    fingerprint = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_error_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_fingerprint",
                table: "error_logs",
                column: "fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_is_resolved",
                table: "error_logs",
                column: "is_resolved");

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_last_seen_at",
                table: "error_logs",
                column: "last_seen_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_error_logs_source_level",
                table: "error_logs",
                columns: new[] { "source", "level" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "error_logs");
        }
    }
}
