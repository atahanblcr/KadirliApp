using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "news_articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    wp_id = table.Column<int>(type: "integer", nullable: false),
                    source_title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_excerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source_content_html = table.Column<string>(type: "text", nullable: false),
                    source_plain_text = table.Column<string>(type: "text", nullable: false),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    source_published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_state_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_image_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source_image_width = table.Column<int>(type: "integer", nullable: true),
                    source_image_height = table.Column<int>(type: "integer", nullable: true),
                    reading_minutes = table.Column<int>(type: "integer", nullable: false),
                    title_override = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    excerpt_override = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cover_image_file_id_override = table.Column<Guid>(type: "uuid", nullable: true),
                    override_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    override_updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    archived_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    featured_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_news_articles_files_cover_image_file_id_override",
                        column: x => x.cover_image_file_id_override,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_news_articles_files_source_image_file_id",
                        column: x => x.source_image_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "news_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    wp_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    article_count = table.Column<int>(type: "integer", nullable: false),
                    is_excluded = table.Column<bool>(type: "boolean", nullable: false),
                    show_in_filter_strip = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_sync_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    trigger = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    triggered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    fetched = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<int>(type: "integer", nullable: false),
                    updated = table.Column<int>(type: "integer", nullable: false),
                    skipped = table.Column<int>(type: "integer", nullable: false),
                    failed = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cursor_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cursor_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    marked_gone = table.Column<int>(type: "integer", nullable: false),
                    restored = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_sync_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_sync_state",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    forward_cursor_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archive_cursor_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archive_completed = table.Column<bool>(type: "boolean", nullable: false),
                    last_successful_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_sync_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "news_article_categories",
                columns: table => new
                {
                    news_article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    news_category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_news_article_categories", x => new { x.news_article_id, x.news_category_id });
                    table.ForeignKey(
                        name: "fk_news_article_categories_news_articles_news_article_id",
                        column: x => x.news_article_id,
                        principalTable: "news_articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_news_article_categories_news_categories_news_category_id",
                        column: x => x.news_category_id,
                        principalTable: "news_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_news_article_categories_news_category_id",
                table: "news_article_categories",
                column: "news_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_cover_image_file_id_override",
                table: "news_articles",
                column: "cover_image_file_id_override");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_is_archived_source_state",
                table: "news_articles",
                columns: new[] { "is_archived", "source_state" });

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_source_image_file_id",
                table: "news_articles",
                column: "source_image_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_source_image_url",
                table: "news_articles",
                column: "source_image_url");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_source_modified_at",
                table: "news_articles",
                column: "source_modified_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_source_published_at",
                table: "news_articles",
                column: "source_published_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_source_title",
                table: "news_articles",
                column: "source_title");

            migrationBuilder.CreateIndex(
                name: "ix_news_articles_wp_id",
                table: "news_articles",
                column: "wp_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_news_categories_wp_id",
                table: "news_categories",
                column: "wp_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_news_sync_runs_started_at",
                table: "news_sync_runs",
                column: "started_at",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "news_article_categories");

            migrationBuilder.DropTable(
                name: "news_sync_runs");

            migrationBuilder.DropTable(
                name: "news_sync_state");

            migrationBuilder.DropTable(
                name: "news_articles");

            migrationBuilder.DropTable(
                name: "news_categories");
        }
    }
}
