using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PopulationDbContext))]
    [Migration("20260719102300_RemoveEducationServiceQuality")]
    public sealed class RemoveEducationServiceQuality : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationQualityIndex",
                table: "CityPopulationServiceQualityStates");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EducationQualityIndex",
                table: "CityPopulationServiceQualityStates",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: false,
                defaultValue: 1m);
        }
    }
}
