using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeNameTrigramSearch : Migration
    {
        // Typo-tolerant employee-name search on the Employees read model (012, ADR-0047). pg_trgm provides the
        // word-similarity operators; unaccent folds diacritics. Stock unaccent is only STABLE, so it cannot
        // back a functional index — immutable_unaccent is the standard IMMUTABLE wrapper over the two-argument
        // unaccent(regdictionary, text) (which is dictionary-pinned and therefore search-path independent). The
        // GIN trigram index on immutable_unaccent(display_name) makes the `<%` pre-filter index-bounded, so the
        // searched query never degrades to a full-table similarity scan (SC-004). The blank-q keyset path keeps
        // its own (display_name, employee_id) order and is unaffected.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION immutable_unaccent(text)
                    RETURNS text
                    LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
                    AS $$ SELECT public.unaccent('public.unaccent', $1) $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX ix_employees_display_name_trgm
                    ON employees USING gin (immutable_unaccent(display_name) gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_employees_display_name_trgm;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS immutable_unaccent(text);");
        }
    }
}
