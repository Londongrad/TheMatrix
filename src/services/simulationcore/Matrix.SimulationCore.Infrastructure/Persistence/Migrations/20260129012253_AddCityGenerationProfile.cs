using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityGenerationProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                ADD COLUMN IF NOT EXISTS "GenerationDevelopmentLevel" integer NOT NULL DEFAULT 2;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                ADD COLUMN IF NOT EXISTS "GenerationSeed" character varying(128) NOT NULL DEFAULT 'legacy-city';
                """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                ADD COLUMN IF NOT EXISTS "GenerationSizeTier" integer NOT NULL DEFAULT 2;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                ADD COLUMN IF NOT EXISTS "GenerationUrbanDensity" integer NOT NULL DEFAULT 2;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'Cities'
                    ) THEN
                        UPDATE "Cities"
                        SET "GenerationSeed" = COALESCE(NULLIF(BTRIM("Name"), ''), 'legacy-city')
                        WHERE "GenerationSeed" IS NULL OR BTRIM("GenerationSeed") = '';
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                DROP COLUMN IF EXISTS "GenerationDevelopmentLevel";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                DROP COLUMN IF EXISTS "GenerationSeed";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                DROP COLUMN IF EXISTS "GenerationSizeTier";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS "Cities"
                DROP COLUMN IF EXISTS "GenerationUrbanDensity";
                """);
        }
    }
}
