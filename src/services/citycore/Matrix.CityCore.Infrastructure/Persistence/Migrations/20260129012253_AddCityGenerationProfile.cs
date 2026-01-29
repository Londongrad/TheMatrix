using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityGenerationProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenerationDevelopmentLevel",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "GenerationSeed",
                table: "Cities",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "legacy-city");

            migrationBuilder.AddColumn<int>(
                name: "GenerationSizeTier",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "GenerationUrbanDensity",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.Sql("UPDATE \"Cities\" SET \"GenerationSeed\" = COALESCE(NULLIF(BTRIM(\"Name\"), ''), 'legacy-city');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerationDevelopmentLevel",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "GenerationSeed",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "GenerationSizeTier",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "GenerationUrbanDensity",
                table: "Cities");
        }
    }
}