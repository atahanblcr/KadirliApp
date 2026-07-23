using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrgmExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_ads_title_trgm ON ads USING GIN (title gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_places_name_trgm ON places USING GIN (name gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_places_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ads_title_trgm;");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}
